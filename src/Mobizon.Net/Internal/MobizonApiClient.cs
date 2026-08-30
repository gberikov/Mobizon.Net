using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts;

using Mobizon.Net.Internal.Converters;

namespace Mobizon.Net.Internal
{
    internal class MobizonApiClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new StringToIntConverter(),
                new StringToLongConverter(),
                new StringToFloatConverter(),
                new StringToDecimalConverter(),
                new StringToBoolConverter(),
                new StringMinutesToTimeSpanConverter(),
                new SmsStatusConverter(),
                new CampaignCommonStatusConverter(),
                new MessageTypeConverter(),
                new StringToNumericEnumConverter<CampaignType>(),
                new StringToNumericEnumConverter<StopListLevel>(),
                new StringToNumericEnumConverter<Mobizon.Contracts.LinkStatus>(),
                new StringToNumericEnumConverter<Mobizon.Contracts.LinkModeratorStatus>(),
                new MobizonDateTimeConverter(),
                new TolerantStringEnumConverter<ContactType>(),
                new TolerantStringEnumConverter<Gender>(),
                // Contact-card fields the PHP API may emit as `[]`/`""` when unset — tolerate that.
                new EmptyTolerantObjectConverter<Mobizon.Contracts.ContactFieldInfo>(),
                new EmptyTolerantObjectConverter<Mobizon.Contracts.MobileFieldInfo>(),
                new EmptyTolerantObjectConverter<Mobizon.Contracts.AddressFieldInfo>(),
                new AddRecipientsResultConverter(),
                new LinkStatsResultConverter()
            }
        };

        private static readonly ProductInfoHeaderValue UserAgent =
            new ProductInfoHeaderValue("Mobizon.Net", GetSdkVersion());

        private readonly HttpClient _httpClient;
        private readonly MobizonClientOptions _options;

        public MobizonApiClient(HttpClient httpClient, MobizonClientOptions options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            options.Validate();
        }

        /// <summary>
        /// Sends a form-encoded POST. The API key is always a body field (never part of the URL), so it
        /// does not leak into proxy / HttpClient request logs.
        /// </summary>
        public async Task<MobizonResponse<T>> SendAsync<T>(
            string module,
            string apiMethod,
            IDictionary<string, string>? parameters,
            CancellationToken cancellationToken = default,
            int[]? extraSuccessCodes = null)
        {
            var form = new Dictionary<string, string>();
            if (parameters != null)
                foreach (var kv in parameters)
                    form[kv.Key] = kv.Value;
            form["apiKey"] = _options.ApiKey; // SDK's key always wins; never overridable by a caller parameter.

            var request = CreateRequest(module, apiMethod);
            request.Content = new FormUrlEncodedContent(form);

            return await SendCoreAsync<T>(request, cancellationToken, extraSuccessCodes).ConfigureAwait(false);
        }

        public async Task<MobizonResponse<T>> SendMultipartAsync<T>(
            string module,
            string apiMethod,
            IDictionary<string, string> fields,
            Stream? photo = null,
            string? photoFileName = null,
            CancellationToken cancellationToken = default,
            int[]? extraSuccessCodes = null,
            string fileFieldName = "data[photo]")
        {
            var request = CreateRequest(module, apiMethod);

            var multipart = new MultipartFormDataContent();
            multipart.Add(new StringContent(_options.ApiKey), "apiKey");

            foreach (var kv in fields)
                multipart.Add(new StringContent(kv.Value ?? string.Empty), kv.Key);

            if (photo != null)
            {
                var fileContent = new StreamContent(photo);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                multipart.Add(fileContent, fileFieldName, photoFileName ?? "photo");
            }

            request.Content = multipart;

            return await SendCoreAsync<T>(request, cancellationToken, extraSuccessCodes).ConfigureAwait(false);
        }

        /// <summary>Read-only API methods are safe to retry. Every Mobizon read is <c>get*</c> or <c>list</c>.</summary>
        internal static bool IsReadMethod(string apiMethod) =>
            apiMethod.StartsWith("get", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(apiMethod, "list", StringComparison.OrdinalIgnoreCase);

        private HttpRequestMessage CreateRequest(string module, string apiMethod)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, BuildUrl(module, apiMethod));
            request.Headers.UserAgent.Add(UserAgent);
            if (IsReadMethod(apiMethod))
                RequestMarkers.MarkIdempotent(request);
            return request;
        }

        private string BuildUrl(string module, string apiMethod) =>
            $"{_options.ApiUrl.TrimEnd('/')}/service/{module}/{apiMethod}?output=json&api={_options.ApiVersion}";

        private static string GetSdkVersion()
        {
            var v = typeof(MobizonApiClient).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
            var plus = v.IndexOf('+');
            return plus > 0 ? v.Substring(0, plus) : v;
        }

        private async Task<MobizonResponse<T>> SendCoreAsync<T>(
            HttpRequestMessage request,
            CancellationToken cancellationToken,
            int[]? extraSuccessCodes = null)
        {
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw; // the caller asked for it
            }
            catch (OperationCanceledException ex)
            {
                // HttpClient.Timeout surfaces as TaskCanceledException while the caller's token is untouched.
                throw new MobizonException(
                    $"Request to Mobizon API timed out after {_httpClient.Timeout}.", statusCode: null, ex);
            }
            catch (Exception ex)
            {
                throw new MobizonException($"Failed to send request to Mobizon API: {ex.Message}", statusCode: null, ex);
            }

            using (response)
            {
                string json;
                try
                {
                    json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    throw new MobizonException("Failed to read Mobizon API response", response.StatusCode, ex);
                }

                MobizonResponse<T>? result;
                try
                {
                    result = JsonSerializer.Deserialize<MobizonResponse<T>>(json, JsonOptions);
                }
                catch (JsonException ex)
                {
                    throw new MobizonException(DescribeUnexpectedBody(response, json), response.StatusCode, ex);
                }

                if (result == null)
                    throw new MobizonException(DescribeUnexpectedBody(response, json), response.StatusCode);

                if (result.Code != MobizonResponseCode.Success &&
                    result.Code != MobizonResponseCode.BackgroundTask &&
                    (extraSuccessCodes == null || Array.IndexOf(extraSuccessCodes, result.RawCode) < 0))
                {
                    throw new MobizonApiException(result.RawCode, result.Message, response.StatusCode);
                }

                return result;
            }
        }

        private static string DescribeUnexpectedBody(HttpResponseMessage response, string body)
        {
            var status = (int)response.StatusCode;
            var snippet = body.Length <= 200 ? body : body.Substring(0, 200) + "…";
            return response.IsSuccessStatusCode
                ? $"Failed to deserialize Mobizon API response (HTTP {status}): {snippet}"
                : $"Mobizon API returned HTTP {status} ({response.StatusCode}) with a non-JSON body: {snippet}";
        }
    }
}

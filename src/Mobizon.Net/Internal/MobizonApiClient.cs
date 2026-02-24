using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts.Exceptions;
using Mobizon.Contracts.Models;
using Mobizon.Contracts.Models.Campaign;

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
                new StringToFloatConverter(),
                new StringToDecimalConverter(),
                new SmsStatusConverter(),
                new CampaignCommonStatusConverter(),
                new StringToNumericEnumConverter<CampaignType>(),
                new MobizonDateTimeConverter()
            }
        };

        private static readonly JsonSerializerOptions JsonWriteOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private readonly HttpClient _httpClient;
        private readonly MobizonClientOptions _options;

        public MobizonApiClient(HttpClient httpClient, MobizonClientOptions options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            options.Validate();
        }

        public async Task<MobizonResponse<T>> SendAsync<T>(
            HttpMethod method,
            string module,
            string apiMethod,
            IDictionary<string, string>? parameters,
            CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(method, BuildUrl(module, apiMethod));

            if (method == HttpMethod.Post && parameters != null && parameters.Count > 0)
                request.Content = new FormUrlEncodedContent(parameters);

            return await SendCoreAsync<T>(request, cancellationToken).ConfigureAwait(false);
        }

        public async Task<MobizonResponse<T>> SendJsonAsync<T>(
            string module,
            string apiMethod,
            object? body,
            CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, BuildUrl(module, apiMethod));

            if (body != null)
            {
                var json = JsonSerializer.Serialize(body, JsonWriteOptions);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            return await SendCoreAsync<T>(request, cancellationToken).ConfigureAwait(false);
        }

        public async Task<MobizonResponse<T>> SendMultipartAsync<T>(
            string module,
            string apiMethod,
            IDictionary<string, string> fields,
            Stream? photo = null,
            string? photoFileName = null,
            CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, BuildUrl(module, apiMethod));

            var multipart = new MultipartFormDataContent();

            foreach (var kv in fields)
                multipart.Add(new StringContent(kv.Value ?? string.Empty), kv.Key);

            if (photo != null)
            {
                var fileContent = new StreamContent(photo);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                multipart.Add(fileContent, "data[photo]", photoFileName ?? "photo");
            }

            request.Content = multipart;

            return await SendCoreAsync<T>(request, cancellationToken).ConfigureAwait(false);
        }

        private string BuildUrl(string module, string apiMethod) =>
            $"{_options.ApiUrl.TrimEnd('/')}/service/{module}/{apiMethod}" +
            $"?output=json&api={_options.ApiVersion}&apiKey={_options.ApiKey}";

        private async Task<MobizonResponse<T>> SendCoreAsync<T>(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new MobizonException(
                    $"Failed to send request to Mobizon API: {ex.Message}", ex);
            }

            string json;
            try
            {
                json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new MobizonException("Failed to read Mobizon API response", ex);
            }

            MobizonResponse<T> result;
            try
            {
                result = JsonSerializer.Deserialize<MobizonResponse<T>>(json, JsonOptions)!;
            }
            catch (Exception ex)
            {
                throw new MobizonException("Failed to deserialize Mobizon API response", ex);
            }

            if (result.Code != MobizonResponseCode.Success &&
                result.Code != MobizonResponseCode.BackgroundTask)
            {
                throw new MobizonApiException(result.RawCode, result.Message);
            }

            return result;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts.Exceptions;
using Mobizon.Contracts.Models;

namespace Mobizon.Net.Internal
{
    internal class MobizonApiClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
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
            var url = $"{_options.ApiUrl.TrimEnd('/')}/service/{module}/{apiMethod}" +
                      $"?output=json&api={_options.ApiVersion}&apiKey={_options.ApiKey}";

            var request = new HttpRequestMessage(method, url);

            if (method == HttpMethod.Post && parameters != null && parameters.Count > 0)
            {
                request.Content = new FormUrlEncodedContent(parameters);
            }

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
                throw new MobizonException(
                    "Failed to read Mobizon API response", ex);
            }

            MobizonResponse<T> result;
            try
            {
                result = JsonSerializer.Deserialize<MobizonResponse<T>>(json, JsonOptions)!;
            }
            catch (Exception ex)
            {
                throw new MobizonException(
                    "Failed to deserialize Mobizon API response", ex);
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

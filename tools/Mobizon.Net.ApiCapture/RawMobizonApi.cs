using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Mobizon.Net.ApiCapture
{
    public sealed class RawMobizonApi
    {
        private readonly HttpClient _http;
        private readonly string _apiUrl, _apiKey, _apiVersion;

        public RawMobizonApi(HttpClient http, string apiUrl, string apiKey, string apiVersion = "v1")
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API key is required.", nameof(apiKey));
            if (!Uri.TryCreate(apiUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                throw new ArgumentException(
                    "Capture runs against the live account: the endpoint must be an absolute https URI.", nameof(apiUrl));

            _http = http; _apiUrl = apiUrl; _apiKey = apiKey; _apiVersion = apiVersion;
        }

        /// <summary>
        /// Endpoint URL without credentials. The API key is a body field, exactly as in the SDK, so it does not
        /// reach proxy access logs or the shell history of whoever runs a capture.
        /// </summary>
        public static string BuildUrl(string apiUrl, string apiVersion, string module, string method) =>
            $"{apiUrl.TrimEnd('/')}/service/{module}/{method}?output=json&api={apiVersion}";

        public async Task<string> CallAsync(string module, string method, IDictionary<string, string>? form = null)
        {
            var url = BuildUrl(_apiUrl, _apiVersion, module, method);
            var body = new Dictionary<string, string>();
            if (form != null)
                foreach (var kv in form)
                    body[kv.Key] = kv.Value;
            body["apiKey"] = _apiKey;

            using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(body) };
            using var resp = await _http.SendAsync(req);
            return await resp.Content.ReadAsStringAsync();
        }
    }
}

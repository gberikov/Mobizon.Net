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
            _http = http; _apiUrl = apiUrl; _apiKey = apiKey; _apiVersion = apiVersion;
        }

        public static string BuildUrl(string apiUrl, string apiVersion, string apiKey, string module, string method) =>
            $"{apiUrl.TrimEnd('/')}/service/{module}/{method}?output=json&api={apiVersion}&apiKey={apiKey}";

        public async Task<string> CallAsync(string module, string method, IDictionary<string, string>? form = null)
        {
            var url = BuildUrl(_apiUrl, _apiVersion, _apiKey, module, method);
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            if (form != null && form.Count > 0) req.Content = new FormUrlEncodedContent(form);
            using var resp = await _http.SendAsync(req);
            return await resp.Content.ReadAsStringAsync();
        }
    }
}

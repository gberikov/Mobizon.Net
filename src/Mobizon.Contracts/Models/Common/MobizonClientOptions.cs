using Mobizon.Contracts;
using System;
using System.Threading;

namespace Mobizon.Contracts
{
    /// <summary>
    /// Configuration options for the Mobizon API client.
    /// </summary>
    public class MobizonClientOptions
    {
        private string _apiKey = string.Empty;
        private string _apiUrl = string.Empty;

        /// <summary>
        /// Gets or sets the Mobizon API key used for authentication.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the value is set to <see langword="null"/>.</exception>
        public string ApiKey
        {
            get => _apiKey;
            set => _apiKey = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets the base URL of the Mobizon API endpoint (e.g. <c>https://api.mobizon.kz</c>).
        /// Must be an absolute <c>https</c> URI without query, fragment or user info. A path prefix is allowed
        /// (e.g. a trusted reverse proxy at <c>https://gateway.example.com/mobizon</c>).
        /// Do not include the <c>/service/</c> segment — the client appends it automatically.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the value is set to <see langword="null"/>.</exception>
        public string ApiUrl
        {
            get => _apiUrl;
            set => _apiUrl = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Allows a plain <c>http://</c> <see cref="ApiUrl"/>. Off by default: the API key travels in every request
        /// body, so an unencrypted endpoint exposes it and every message text. Enable only for a local test server.
        /// </summary>
        public bool AllowInsecureHttp { get; set; }

        /// <summary>
        /// Gets or sets the API version segment included in every request URL. Defaults to <c>"v1"</c>.
        /// </summary>
        public string ApiVersion { get; set; } = "v1";

        /// <summary>
        /// Gets or sets the HTTP request timeout, covering the whole exchange including reading the response body.
        /// Defaults to 30 seconds; <see cref="Timeout.InfiniteTimeSpan"/> disables it. Applied only to an
        /// <see cref="System.Net.Http.HttpClient"/> the SDK owns (parameterless client constructor or the DI
        /// registration); a caller-supplied client keeps its own timeout.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Validates the options: non-empty <see cref="ApiKey"/>, an absolute HTTPS <see cref="ApiUrl"/>
        /// (HTTP only with <see cref="AllowInsecureHttp"/>), a well-formed <see cref="ApiVersion"/> and a positive
        /// or infinite <see cref="Timeout"/>.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when any option is invalid.</exception>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ApiKey))
                throw new ArgumentException("ApiKey must not be null or empty.", nameof(ApiKey));

            if (string.IsNullOrWhiteSpace(ApiUrl))
                throw new ArgumentException("ApiUrl must not be null or empty.", nameof(ApiUrl));

            if (!Uri.TryCreate(ApiUrl, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Host))
                throw new ArgumentException("ApiUrl must be an absolute URI, e.g. https://api.mobizon.kz.", nameof(ApiUrl));

            if (uri.Scheme == Uri.UriSchemeHttp)
            {
                if (!AllowInsecureHttp)
                    throw new ArgumentException(
                        "ApiUrl must use https: the API key is sent in every request body. " +
                        "Set AllowInsecureHttp = true only for a local test server.", nameof(ApiUrl));
            }
            else if (uri.Scheme != Uri.UriSchemeHttps)
            {
                throw new ArgumentException("ApiUrl must use the https scheme.", nameof(ApiUrl));
            }

            if (!string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) || !string.IsNullOrEmpty(uri.UserInfo))
                throw new ArgumentException("ApiUrl must not contain a query string, fragment or user info.", nameof(ApiUrl));

            if (string.IsNullOrWhiteSpace(ApiVersion) || !IsVersionToken(ApiVersion))
                throw new ArgumentException("ApiVersion must be a simple token such as \"v1\".", nameof(ApiVersion));

            if (Timeout <= TimeSpan.Zero && Timeout != System.Threading.Timeout.InfiniteTimeSpan)
                throw new ArgumentException("Timeout must be positive or Timeout.InfiniteTimeSpan.", nameof(Timeout));
        }

        private static bool IsVersionToken(string value)
        {
            foreach (var c in value)
                if (!char.IsLetterOrDigit(c) && c != '.' && c != '-' && c != '_')
                    return false;
            return true;
        }
    }
}

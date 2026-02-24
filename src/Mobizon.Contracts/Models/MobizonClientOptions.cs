using System;

namespace Mobizon.Contracts.Models
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
        /// Gets or sets the base URL of the Mobizon API endpoint (e.g. <c>https://api.mobizon.com/service/</c>).
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the value is set to <see langword="null"/>.</exception>
        public string ApiUrl
        {
            get => _apiUrl;
            set => _apiUrl = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets the API version segment included in every request URL. Defaults to <c>"v1"</c>.
        /// </summary>
        public string ApiVersion { get; set; } = "v1";

        /// <summary>
        /// Gets or sets the HTTP request timeout. Defaults to 30 seconds.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Validates that the required options are present and non-empty.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when <see cref="ApiKey"/> or <see cref="ApiUrl"/> is null or whitespace.
        /// </exception>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ApiKey))
                throw new ArgumentException("ApiKey must not be null or empty.", nameof(ApiKey));
            if (string.IsNullOrWhiteSpace(ApiUrl))
                throw new ArgumentException("ApiUrl must not be null or empty.", nameof(ApiUrl));
        }
    }
}

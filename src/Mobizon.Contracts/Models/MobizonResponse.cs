using System.Text.Json.Serialization;

namespace Mobizon.Contracts.Models
{
    /// <summary>
    /// Universal response wrapper returned by all Mobizon API endpoints.
    /// </summary>
    /// <typeparam name="T">The type of the payload contained in <see cref="Data"/>.</typeparam>
    public class MobizonResponse<T>
    {
        /// <summary>
        /// Gets or sets the raw integer response code returned by the API.
        /// </summary>
        [JsonPropertyName("code")]
        public int RawCode { get; set; }

        /// <summary>
        /// Gets the strongly-typed <see cref="MobizonResponseCode"/> representation of <see cref="RawCode"/>.
        /// </summary>
        [JsonIgnore]
        public MobizonResponseCode Code => (MobizonResponseCode)RawCode;

        /// <summary>
        /// Gets or sets the payload returned by the API. May be the default value of <typeparamref name="T"/> when no data is present.
        /// </summary>
        [JsonPropertyName("data")]
        public T Data { get; set; } = default!;

        /// <summary>
        /// Gets or sets the human-readable message accompanying the response, typically describing an error.
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}

using Mobizon.Contracts;
using System.Text.Json.Serialization;

namespace Mobizon.Net.Internal
{
    /// <summary>
    /// Universal response wrapper returned by all Mobizon API endpoints.
    /// Internal implementation detail — consumers receive unwrapped domain types from service methods.
    /// </summary>
    /// <typeparam name="T">The type of the payload contained in <see cref="Data"/>.</typeparam>
    internal class MobizonResponse<T>
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
        /// Gets or sets the payload returned by the API.
        /// <para>
        /// For endpoints that return a payload, <see cref="Data"/> is populated on success
        /// (<see cref="Code"/> is <see cref="MobizonResponseCode.Success"/> or
        /// <see cref="MobizonResponseCode.BackgroundTask"/>).
        /// For endpoints that return no payload, or for value-type <typeparamref name="T"/>,
        /// <see cref="Data"/> may be the default value of <typeparamref name="T"/> — this is
        /// legitimate and should not be treated as an error.
        /// </para>
        /// </summary>
        [JsonPropertyName("data")]
        public T Data { get; set; } = default!;

        /// <summary>
        /// Gets or sets the human-readable message accompanying the response, typically describing an error.
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>HTTP status of the response this envelope was read from (always 2xx for a returned response).</summary>
        [JsonIgnore]
        public System.Net.HttpStatusCode? StatusCode { get; set; }
    }
}

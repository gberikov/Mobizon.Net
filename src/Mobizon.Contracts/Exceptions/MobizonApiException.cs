using Mobizon.Contracts.Models.Common;

namespace Mobizon.Contracts.Exceptions
{
    /// <summary>
    /// Exception thrown when the Mobizon API returns a non-success response code.
    /// </summary>
    public class MobizonApiException : MobizonException
    {
        /// <summary>
        /// Gets the strongly-typed response code returned by the API.
        /// </summary>
        public MobizonResponseCode Code { get; }

        /// <summary>
        /// Gets the raw integer response code returned by the API.
        /// </summary>
        public int RawCode { get; }

        /// <summary>
        /// Gets the human-readable error message returned by the API.
        /// </summary>
        public string ApiMessage { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="MobizonApiException"/> with the API error code and message.
        /// </summary>
        /// <param name="rawCode">The raw integer response code returned by the API.</param>
        /// <param name="apiMessage">The human-readable error message returned by the API.</param>
        public MobizonApiException(int rawCode, string apiMessage)
            : base($"Mobizon API error {rawCode}: {apiMessage}")
        {
            RawCode = rawCode;
            Code = (MobizonResponseCode)rawCode;
            ApiMessage = apiMessage;
        }
    }
}

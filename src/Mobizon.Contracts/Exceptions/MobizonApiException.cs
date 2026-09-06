using System.Collections.Generic;
using System.Net;
using Mobizon.Contracts;

namespace Mobizon.Contracts
{
    /// <summary>
    /// Exception thrown when the Mobizon API returns a non-success response code.
    /// </summary>
    public class MobizonApiException : MobizonException
    {
        private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> NoFieldErrors =
            new Dictionary<string, IReadOnlyList<string>>();

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
        /// Per-field validation errors extracted from the error response's <c>data</c> object, keyed by field
        /// path (nested objects are flattened with dots, e.g. <c>mobile.value</c>). Populated for
        /// <see cref="MobizonResponseCode.ValidationError"/>-style responses whose <c>data</c> is an object of
        /// field → message(s); empty when the API sent no details or sent them in another shape.
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyList<string>> FieldErrors { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="MobizonApiException"/> with the API error code and message.
        /// </summary>
        /// <param name="rawCode">The raw integer response code returned by the API.</param>
        /// <param name="apiMessage">The human-readable error message returned by the API.</param>
        /// <param name="statusCode">HTTP status of the response (usually 200 — Mobizon reports errors inside the JSON envelope).</param>
        public MobizonApiException(int rawCode, string apiMessage, HttpStatusCode? statusCode = null)
            : this(rawCode, apiMessage, statusCode, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MobizonApiException"/> with the API error code, message and
        /// per-field validation details.
        /// </summary>
        /// <param name="rawCode">The raw integer response code returned by the API.</param>
        /// <param name="apiMessage">The human-readable error message returned by the API.</param>
        /// <param name="statusCode">HTTP status of the response.</param>
        /// <param name="fieldErrors">Per-field errors, or <see langword="null"/> when the response carried none.</param>
        public MobizonApiException(
            int rawCode,
            string apiMessage,
            HttpStatusCode? statusCode,
            IReadOnlyDictionary<string, IReadOnlyList<string>>? fieldErrors)
            : base($"Mobizon API error {rawCode}: {apiMessage}", statusCode)
        {
            RawCode = rawCode;
            Code = (MobizonResponseCode)rawCode;
            ApiMessage = apiMessage;
            FieldErrors = fieldErrors ?? NoFieldErrors;
        }
    }
}

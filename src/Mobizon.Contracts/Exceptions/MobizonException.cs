using System;
using System.Net;

namespace Mobizon.Contracts
{
    /// <summary>
    /// Base exception for all errors raised by the Mobizon.Net SDK: transport failures, timeouts,
    /// unexpected (non-JSON) responses and — via <see cref="MobizonApiException"/> — API-level errors.
    /// </summary>
    public class MobizonException : Exception
    {
        /// <summary>
        /// HTTP status code of the response that caused this exception, or <see langword="null"/>
        /// when no response was received (network error, timeout).
        /// </summary>
        public HttpStatusCode? StatusCode { get; }

        /// <summary>Initializes a new instance with a message.</summary>
        public MobizonException(string message)
            : base(message)
        {
        }

        /// <summary>Initializes a new instance with a message and the causing exception.</summary>
        public MobizonException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>Initializes a new instance with a message, the HTTP status of the offending response and an optional cause.</summary>
        public MobizonException(string message, HttpStatusCode? statusCode, Exception? innerException = null)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }
    }
}

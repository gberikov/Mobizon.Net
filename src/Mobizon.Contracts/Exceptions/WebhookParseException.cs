using System;

namespace Mobizon.Contracts.Exceptions
{
    /// <summary>
    /// Thrown when a webhook request body cannot be parsed — malformed JSON or a missing/invalid
    /// required envelope field. This is distinct from a signature mismatch, which is reported as a
    /// status rather than an exception.
    /// </summary>
    public class WebhookParseException : Exception
    {
        /// <summary>Initializes a new instance with a descriptive message.</summary>
        /// <param name="message">A description of the parse failure.</param>
        public WebhookParseException(string message)
            : base(message)
        {
        }

        /// <summary>Initializes a new instance with a descriptive message and underlying cause.</summary>
        /// <param name="message">A description of the parse failure.</param>
        /// <param name="innerException">The underlying exception (e.g. a <see cref="System.Text.Json.JsonException"/>).</param>
        public WebhookParseException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}

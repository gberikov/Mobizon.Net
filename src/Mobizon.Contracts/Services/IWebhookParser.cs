using Mobizon.Contracts.Models.Webhooks;

namespace Mobizon.Contracts.Services
{
    /// <summary>
    /// Parses a raw Mobizon webhook JSON body into a typed <see cref="MobizonWebhookEvent"/>.
    /// </summary>
    public interface IWebhookParser
    {
        /// <summary>
        /// Parses the body into a typed event.
        /// </summary>
        /// <param name="jsonBody">The raw request body (JSON).</param>
        /// <returns>The parsed event; an <see cref="UnknownWebhookEvent"/> for unrecognised types.</returns>
        /// <exception cref="Mobizon.Contracts.Exceptions.WebhookParseException">
        /// Thrown on malformed JSON or a missing/invalid required envelope field.
        /// </exception>
        MobizonWebhookEvent Parse(string jsonBody);

        /// <summary>
        /// Attempts to parse the body without throwing.
        /// </summary>
        /// <param name="jsonBody">The raw request body (JSON).</param>
        /// <param name="event">When this method returns <see langword="true"/>, the parsed event; otherwise <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if parsing succeeded; otherwise <see langword="false"/>.</returns>
        bool TryParse(string jsonBody, out MobizonWebhookEvent? @event);
    }
}

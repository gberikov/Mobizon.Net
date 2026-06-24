using System;
using Mobizon.Contracts.Models.Webhooks;

namespace Mobizon.Contracts.Services
{
    /// <summary>
    /// Combined verify-and-parse entry point for inbound Mobizon webhooks (recommended).
    /// Encodes the "parse → verify → acknowledge → process" flow in a single call.
    /// </summary>
    public interface IWebhookProcessor
    {
        /// <summary>
        /// Parses and verifies a webhook body against a single shared secret key.
        /// </summary>
        /// <param name="jsonBody">The raw request body (JSON).</param>
        /// <param name="secretKey">The webhook secret key.</param>
        /// <returns>A <see cref="WebhookProcessResult"/> describing the outcome.</returns>
        WebhookProcessResult Process(string jsonBody, string secretKey);

        /// <summary>
        /// Parses the envelope, lets the caller select the secret from the parsed event (typically by
        /// <see cref="MobizonWebhookEvent.WebhookId"/>), then verifies — supporting per-webhook secrets.
        /// Parsing the envelope confers no trust; the selected secret still gates authenticity.
        /// </summary>
        /// <param name="jsonBody">The raw request body (JSON).</param>
        /// <param name="secretSelector">Selects the secret key for the parsed event.</param>
        /// <returns>A <see cref="WebhookProcessResult"/> describing the outcome.</returns>
        WebhookProcessResult Process(string jsonBody, Func<MobizonWebhookEvent, string> secretSelector);
    }
}

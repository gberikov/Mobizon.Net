using System;

namespace Mobizon.Contracts.Webhooks
{
    /// <summary>
    /// Common envelope shared by every Mobizon webhook callback. Concrete subclasses
    /// (e.g. <see cref="SmsDeliveryReportEvent"/>) carry the event-specific data.
    /// </summary>
    public abstract class MobizonWebhookEvent
    {
        /// <summary>Unique event identifier. Stable across delivery retries — use it as an idempotency key.</summary>
        public long EventId { get; set; }

        /// <summary>The recognised event type. <see cref="WebhookEventType.Unknown"/> for unrecognised types.</summary>
        public WebhookEventType EventType { get; set; }

        /// <summary>The original <c>eventType</c> string exactly as received (preserves fidelity for unknown types).</summary>
        public string EventTypeRaw { get; set; } = string.Empty;

        /// <summary>The event creation time, parsed from <c>eventCreateTs</c> for convenience.</summary>
        public DateTimeOffset? EventCreateTs { get; set; }

        /// <summary>
        /// The original <c>eventCreateTs</c> string exactly as received. This verbatim value is what
        /// the signature is computed over — never a reformatted value.
        /// </summary>
        public string EventCreateTsRaw { get; set; } = string.Empty;

        /// <summary>Identifier of the webhook that produced this delivery. Disambiguates multiple webhooks and resolves per-webhook secrets.</summary>
        public long WebhookId { get; set; }

        /// <summary>The delivery attempt number (1 for the first attempt; increments on retry).</summary>
        public int Attempt { get; set; }

        /// <summary>The digital signature accompanying the request, to be verified against the recomputed value.</summary>
        public string Sign { get; set; } = string.Empty;
    }
}

using System.Text.Json;

namespace Mobizon.Contracts.Webhooks
{
    /// <summary>
    /// A webhook whose <c>eventType</c> is not recognised by this SDK version. The common envelope
    /// is still populated and the signature is still verifiable; the event-specific payload is
    /// exposed unparsed via <see cref="RawData"/> for forward compatibility.
    /// </summary>
    public sealed class UnknownWebhookEvent : MobizonWebhookEvent
    {
        /// <summary>The raw, unparsed <c>data</c> element of the event (an independent clone, safe to retain).</summary>
        public JsonElement RawData { get; set; }
    }
}

namespace Mobizon.Contracts.Webhooks
{
    /// <summary>
    /// The outcome of processing (verifying + parsing) a webhook request.
    /// </summary>
    public enum WebhookProcessStatus
    {
        /// <summary>The body parsed and the signature matched — the event is authentic.</summary>
        Ok = 0,

        /// <summary>The body parsed but the signature did not match — reject (e.g. HTTP 403).</summary>
        SignatureMismatch,

        /// <summary>The body could not be parsed — reject (e.g. HTTP 400).</summary>
        ParseError
    }

    /// <summary>
    /// The result of <see cref="Mobizon.Contracts.Webhooks.IWebhookProcessor"/> processing.
    /// </summary>
    public sealed class WebhookProcessResult
    {
        /// <summary>Initializes a new result.</summary>
        /// <param name="status">The processing status.</param>
        /// <param name="event">The parsed event, if the body parsed successfully; otherwise <see langword="null"/>.</param>
        public WebhookProcessResult(WebhookProcessStatus status, MobizonWebhookEvent? @event)
        {
            Status = status;
            Event = @event;
        }

        /// <summary>The processing status, which drives the HTTP response (200 / 403 / 400).</summary>
        public WebhookProcessStatus Status { get; }

        /// <summary>
        /// The parsed event. Populated whenever the body parsed successfully — i.e. for both
        /// <see cref="WebhookProcessStatus.Ok"/> and <see cref="WebhookProcessStatus.SignatureMismatch"/>;
        /// <see langword="null"/> only for <see cref="WebhookProcessStatus.ParseError"/>. Check
        /// <see cref="IsAuthentic"/> before trusting it.
        /// </summary>
        public MobizonWebhookEvent? Event { get; }

        /// <summary><see langword="true"/> only when <see cref="Status"/> is <see cref="WebhookProcessStatus.Ok"/>.</summary>
        public bool IsAuthentic => Status == WebhookProcessStatus.Ok;
    }
}

using System;
using Mobizon.Contracts.Webhooks;

namespace Mobizon.Net.Webhooks.AspNetCore
{
    /// <summary>
    /// Options for the ASP.NET Core Mobizon webhook integration.
    /// </summary>
    public sealed class MobizonWebhookOptions
    {
        /// <summary>
        /// Resolves the webhook secret key for a received event. Receives the request's
        /// <see cref="IServiceProvider"/> and the parsed envelope so the secret can be chosen per
        /// <see cref="MobizonWebhookEvent.WebhookId"/>. For a single shared secret, ignore the event
        /// argument and return the configured value. Required.
        /// </summary>
        public Func<IServiceProvider, MobizonWebhookEvent, string>? SecretKeyResolver { get; set; }

        /// <summary>
        /// Maximum accepted request body size in bytes. Requests larger than this are rejected with 413.
        /// Default 262144 (256 KiB).
        /// </summary>
        public int MaxRequestBodyBytes { get; set; } = 262144;
    }
}

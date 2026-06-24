using System;
using Mobizon.Contracts.Models.Webhooks;

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
    }
}

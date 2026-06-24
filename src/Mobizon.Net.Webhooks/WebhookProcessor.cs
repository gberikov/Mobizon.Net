using System;
using Mobizon.Contracts.Exceptions;
using Mobizon.Contracts.Models.Webhooks;
using Mobizon.Contracts.Services;

namespace Mobizon.Net.Webhooks
{
    /// <summary>
    /// Default <see cref="IWebhookProcessor"/>: combines parsing and signature verification, returning a
    /// <see cref="WebhookProcessResult"/> that maps cleanly to HTTP 200 / 403 / 400.
    /// </summary>
    public sealed class WebhookProcessor : IWebhookProcessor
    {
        private readonly IWebhookParser _parser;
        private readonly IWebhookSignatureVerifier _verifier;

        /// <summary>Creates a processor wiring the default parser and verifier.</summary>
        public WebhookProcessor()
            : this(new WebhookParser(), new WebhookSignatureVerifier())
        {
        }

        /// <summary>Creates a processor with the supplied parser and verifier.</summary>
        /// <param name="parser">The webhook parser.</param>
        /// <param name="verifier">The signature verifier.</param>
        public WebhookProcessor(IWebhookParser parser, IWebhookSignatureVerifier verifier)
        {
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _verifier = verifier ?? throw new ArgumentNullException(nameof(verifier));
        }

        /// <inheritdoc />
        public WebhookProcessResult Process(string jsonBody, string secretKey)
        {
            return Process(jsonBody, _ => secretKey);
        }

        /// <inheritdoc />
        public WebhookProcessResult Process(string jsonBody, Func<MobizonWebhookEvent, string> secretSelector)
        {
            if (secretSelector is null)
                throw new ArgumentNullException(nameof(secretSelector));

            MobizonWebhookEvent evt;
            try
            {
                evt = _parser.Parse(jsonBody);
            }
            catch (WebhookParseException)
            {
                return new WebhookProcessResult(WebhookProcessStatus.ParseError, null);
            }

            var secret = secretSelector(evt);
            var authentic = _verifier.Verify(evt, secret);

            return new WebhookProcessResult(
                authentic ? WebhookProcessStatus.Ok : WebhookProcessStatus.SignatureMismatch,
                evt);
        }
    }
}

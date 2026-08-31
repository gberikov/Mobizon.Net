using Mobizon.Contracts.Webhooks;

namespace Mobizon.Contracts.Webhooks
{
    /// <summary>
    /// Verifies the authenticity of an inbound Mobizon webhook via its SHA1 signature.
    /// Implementations MUST compare in constant time and fail closed on any missing input.
    /// </summary>
    public interface IWebhookSignatureVerifier
    {
        /// <summary>
        /// Verifies an already-parsed event against the supplied secret key.
        /// </summary>
        /// <param name="event">The parsed webhook event whose envelope fields and <c>Sign</c> are checked.</param>
        /// <param name="secretKey">The per-webhook secret key chosen when the webhook was created.</param>
        /// <returns><see langword="true"/> if authentic; otherwise <see langword="false"/>.</returns>
        bool Verify(MobizonWebhookEvent @event, string secretKey);

        /// <summary>
        /// Verifies from raw envelope values. The signature string is
        /// <c>eventId|attempt|eventCreateTs|secretKey</c>, hashed with SHA1 and rendered as lowercase hex.
        /// </summary>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="attempt">The delivery attempt number.</param>
        /// <param name="eventCreateTsRaw">The verbatim <c>eventCreateTs</c> string as received.</param>
        /// <param name="sign">The signature value from the request.</param>
        /// <param name="secretKey">The per-webhook secret key.</param>
        /// <returns><see langword="true"/> if authentic; otherwise <see langword="false"/>.</returns>
        bool Verify(long eventId, int attempt, string eventCreateTsRaw, string sign, string secretKey);
    }
}

using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Mobizon.Contracts.Webhooks;
using Mobizon.Contracts;

namespace Mobizon.Net.Webhooks
{
    /// <summary>
    /// Default <see cref="IWebhookSignatureVerifier"/>. Recomputes the Mobizon SHA1 signature over
    /// <c>eventId|attempt|eventCreateTs|secretKey</c> (lowercase hex) and compares it to the request's
    /// signature in constant time. Fails closed on any missing input.
    /// </summary>
    public sealed class WebhookSignatureVerifier : IWebhookSignatureVerifier
    {
        /// <inheritdoc />
        public bool Verify(MobizonWebhookEvent @event, string secretKey)
        {
            if (@event is null)
                return false;

            return Verify(@event.EventId, @event.Attempt, @event.EventCreateTsRaw, @event.Sign, secretKey);
        }

        /// <inheritdoc />
        public bool Verify(long eventId, int attempt, string eventCreateTsRaw, string sign, string secretKey)
        {
            if (string.IsNullOrEmpty(sign) || string.IsNullOrEmpty(eventCreateTsRaw) || string.IsNullOrEmpty(secretKey))
                return false;

            var signingString =
                eventId.ToString(CultureInfo.InvariantCulture) + "|" +
                attempt.ToString(CultureInfo.InvariantCulture) + "|" +
                eventCreateTsRaw + "|" +
                secretKey;

            var computed = ComputeSha1Hex(signingString);
            return FixedTimeEquals(computed, sign);
        }

        private static string ComputeSha1Hex(string input)
        {
            using (var sha1 = SHA1.Create())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash)
                    sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                return sb.ToString();
            }
        }

        private static bool FixedTimeEquals(string a, string b)
        {
            if (a.Length != b.Length)
                return false;

            var diff = 0;
            for (var i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];

            return diff == 0;
        }
    }
}

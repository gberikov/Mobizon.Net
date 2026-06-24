using System;
using Mobizon.Contracts.Models.Messages;

namespace Mobizon.Contracts.Models.Webhooks
{
    /// <summary>
    /// Payload of an <c>sms-delivery-report</c> webhook: the final delivery status of a single SMS
    /// message (one webhook per message, not per segment).
    /// </summary>
    public sealed class SmsDeliveryReport
    {
        /// <summary>Identifier of the SMS campaign the message belonged to.</summary>
        public long CampaignId { get; set; }

        /// <summary>Identifier of the SMS message (correlates with send results and status queries).</summary>
        public long MessageId { get; set; }

        /// <summary>Number of segments the message was split into.</summary>
        public int SegNum { get; set; }

        /// <summary>Time the status was last updated, parsed for convenience.</summary>
        public DateTimeOffset? StatusUpdateTs { get; set; }

        /// <summary>
        /// The final delivery status mapped to the SDK's <see cref="SmsStatus"/> enum, or
        /// <see langword="null"/> if the raw status string is not recognised (see <see cref="StatusRaw"/>).
        /// </summary>
        public SmsStatus? Status { get; set; }

        /// <summary>The original status string exactly as received (e.g. <c>DELIVRD</c>). Always populated.</summary>
        public string StatusRaw { get; set; } = string.Empty;

        /// <summary>Recipient phone number in international format.</summary>
        public string To { get; set; } = string.Empty;
    }
}

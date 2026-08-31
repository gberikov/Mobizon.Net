using Mobizon.Contracts;

namespace Mobizon.Net.Webhooks.Internal.Converters
{
    /// <summary>
    /// Maps Mobizon SMS status strings (e.g. <c>DELIVRD</c>) to the SDK's <see cref="SmsStatus"/> enum.
    /// Non-throwing: an unrecognised status returns <c>null</c> so the raw value can be preserved and
    /// future statuses do not break parsing.
    /// </summary>
    internal static class WebhookSmsStatusConverter
    {
        /// <summary>Maps a raw status string to <see cref="SmsStatus"/>, or <c>null</c> if unrecognised.</summary>
        public static SmsStatus? Map(string? raw)
        {
            switch (raw)
            {
                case "NEW": return SmsStatus.New;
                case "ENQUEUD": return SmsStatus.Enqueued;
                case "ACCEPTD": return SmsStatus.Accepted;
                case "DELIVRD": return SmsStatus.Delivered;
                case "UNDELIV": return SmsStatus.Undelivered;
                case "REJECTD": return SmsStatus.Rejected;
                case "EXPIRD": return SmsStatus.Expired;
                case "DELETED": return SmsStatus.Deleted;
                case "SCHEDUL": return SmsStatus.Scheduled;
                default: return null;
            }
        }
    }
}

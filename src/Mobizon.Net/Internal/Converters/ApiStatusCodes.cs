using System;
using Mobizon.Contracts;

namespace Mobizon.Net.Internal.Converters
{
    /// <summary>Maps strongly-typed status enums to the wire codes the Mobizon API expects on input.</summary>
    internal static class ApiStatusCodes
    {
        public static string ToApiCode(SmsStatus status)
        {
            switch (status)
            {
                case SmsStatus.New:         return "NEW";
                case SmsStatus.Enqueued:    return "ENQUEUD";
                case SmsStatus.Accepted:    return "ACCEPTD";
                case SmsStatus.Delivered:   return "DELIVRD";
                case SmsStatus.Undelivered: return "UNDELIV";
                case SmsStatus.Rejected:    return "REJECTD";
                case SmsStatus.Expired:     return "EXPIRD";
                case SmsStatus.Deleted:     return "DELETED";
                case SmsStatus.Scheduled:   return "SCHEDUL";
                default: throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }

        public static string ToApiCode(CampaignCommonStatus status)
        {
            switch (status)
            {
                case CampaignCommonStatus.Moderation:       return "MODERATION";
                case CampaignCommonStatus.Declined:         return "DECLINED";
                case CampaignCommonStatus.ReadyForSend:     return "READY_FOR_SEND";
                case CampaignCommonStatus.AutoReadyForSend: return "AUTO_READY_FOR_SEND";
                case CampaignCommonStatus.Running:          return "RUNNING";
                case CampaignCommonStatus.Sent:             return "SENT";
                case CampaignCommonStatus.Done:             return "DONE";
                default: throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }
    }
}

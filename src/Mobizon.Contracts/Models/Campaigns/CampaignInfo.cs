using System;

namespace Mobizon.Contracts.Models.Campaigns
{
    /// <summary>
    /// Represents the full data and delivery statistics for an SMS campaign
    /// as returned by the Mobizon API (<c>campaign/getInfo</c>).
    /// Extends <see cref="CampaignData"/> with statistical counters and creation metadata.
    /// </summary>
    public class CampaignInfo : CampaignData
    {
        /// <summary>
        /// Gets or sets the way the campaign was created:
        /// <c>1</c> — via web browser; <c>5</c> — functional (service) campaign.
        /// </summary>
        public int? CreationWay { get; set; }

        /// <summary>
        /// Gets or sets the statistical counters for this campaign.
        /// </summary>
        public CampaignCounters? Counters { get; set; }
    }

    /// <summary>
    /// Contains the message and segment counters returned by <c>campaign/getInfo</c>.
    /// </summary>
    public class CampaignCounters
    {
        /// <summary>Gets or sets the timestamp of the last counter update.</summary>
        public DateTime? UpdateTs { get; set; }

        // ── Segment counters ────────────────────────────────────────────────

        /// <summary>Total segments with status NEW.</summary>
        public int TotalNewSegNum { get; set; }
        /// <summary>Total segments with status ENQUEUD.</summary>
        public int TotalEnqueudSegNum { get; set; }
        /// <summary>Total segments with status ACCEPTD.</summary>
        public int TotalAcceptdSegNum { get; set; }
        /// <summary>Total segments with status DELIVRD.</summary>
        public int TotalDelivrdSegNum { get; set; }
        /// <summary>Total segments with status REJECTD.</summary>
        public int TotalRejectdSegNum { get; set; }
        /// <summary>Total segments with status EXPIRED.</summary>
        public int TotalExpiredSegNum { get; set; }
        /// <summary>Total segments with status UNDELIV.</summary>
        public int TotalUndelivSegNum { get; set; }
        /// <summary>Total segments with status DELETED.</summary>
        public int TotalDeletedSegNum { get; set; }
        /// <summary>Total segments with status UNKNOWN.</summary>
        public int TotalUnknownSegNum { get; set; }
        /// <summary>Total segments with status PDLIVRD.</summary>
        public int TotalPdlivrdSegNum { get; set; }
        /// <summary>Total number of segments in the campaign.</summary>
        public int TotalSegNum { get; set; }

        // ── Message counters ────────────────────────────────────────────────

        /// <summary>Total messages with status NEW.</summary>
        public int TotalNewMsgNum { get; set; }
        /// <summary>Total messages with status ENQUEUD.</summary>
        public int TotalEnqueudMsgNum { get; set; }
        /// <summary>Total messages with status ACCEPTD.</summary>
        public int TotalAcceptdMsgNum { get; set; }
        /// <summary>Total messages with status DELIVRD.</summary>
        public int TotalDelivrdMsgNum { get; set; }
        /// <summary>Total messages with status REJECTD.</summary>
        public int TotalRejectdMsgNum { get; set; }
        /// <summary>Total messages with status EXPIRED.</summary>
        public int TotalExpiredMsgNum { get; set; }
        /// <summary>Total messages with status UNDELIV.</summary>
        public int TotalUndelivMsgNum { get; set; }
        /// <summary>Total messages with status DELETED.</summary>
        public int TotalDeletedMsgNum { get; set; }
        /// <summary>Total messages with status UNKNOWN.</summary>
        public int TotalUnknownMsgNum { get; set; }
        /// <summary>Total messages with status PDLIVRD.</summary>
        public int TotalPdlivrdMsgNum { get; set; }
        /// <summary>Total number of messages (not segments) in the campaign.</summary>
        public int TotalMsgNum { get; set; }

        // ── Cost counters ───────────────────────────────────────────────────

        /// <summary>Total cost of all segments with status NEW.</summary>
        public decimal TotalNewMsgCost { get; set; }
        /// <summary>Total cost of all segments with status ENQUEUD.</summary>
        public decimal TotalEnqueudMsgCost { get; set; }
        /// <summary>Total cost of all segments with status ACCEPTD.</summary>
        public decimal TotalAcceptdMsgCost { get; set; }
        /// <summary>Total cost of all segments with status DELIVRD.</summary>
        public decimal TotalDelivrdMsgCost { get; set; }
        /// <summary>Total cost of all segments with status REJECTD.</summary>
        public decimal TotalRejectdMsgCost { get; set; }
        /// <summary>Total cost of all segments with status EXPIRED.</summary>
        public decimal TotalExpiredMsgCost { get; set; }
        /// <summary>Total cost of all segments with status UNDELIV.</summary>
        public decimal TotalUndelivMsgCost { get; set; }
        /// <summary>Total cost of all segments with status DELETED.</summary>
        public decimal TotalDeletedMsgCost { get; set; }
        /// <summary>Total cost of all segments with status UNKNOWN.</summary>
        public decimal TotalUnknownMsgCost { get; set; }
        /// <summary>Total cost of all segments with status PDLIVRD.</summary>
        public decimal TotalPdlivrdMsgCost { get; set; }
        /// <summary>Total cost of the campaign.</summary>
        public decimal TotalCost { get; set; }
        /// <summary>Total cost at partner rate.</summary>
        public decimal TotalPartnerCost { get; set; }

        /// <summary>
        /// Gets or sets the number of rejected recipients (not included in the campaign).
        /// </summary>
        public int RecipientsRejected { get; set; }
    }
}

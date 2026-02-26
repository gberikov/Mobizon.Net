using Mobizon.Contracts.Models.Common;
using System;
using System.Collections.Generic;
using Mobizon.Contracts.Models.Campaigns;

namespace Mobizon.Contracts.Models.Messages
{
    /// <summary>
    /// Specifies optional filter, pagination, and sort criteria for listing SMS messages.
    /// </summary>
    public class MessageListRequest
    {
        /// <summary>
        /// Gets or sets optional search/filter criteria.
        /// When <see langword="null"/>, no filtering is applied.
        /// </summary>
        public MessageListCriteria? Criteria { get; set; }

        /// <summary>
        /// Gets or sets whether to include recipient country/operator info (0 or 1).
        /// </summary>
        public int? WithNumberInfo { get; set; }

        /// <summary>
        /// Gets or sets optional pagination settings.
        /// </summary>
        public PaginationRequest? Pagination { get; set; }

        /// <summary>
        /// Gets or sets optional sort settings.
        /// </summary>
        public SortRequest? Sort { get; set; }
    }

    /// <summary>
    /// Defines the filter criteria for the <c>message/list</c> API method.
    /// All fields are optional; multiple fields may be combined simultaneously.
    /// </summary>
    public class MessageListCriteria
    {
        /// <summary>
        /// Gets or sets campaign IDs to filter by (max 100).
        /// </summary>
        public IReadOnlyList<int>? CampaignIds { get; set; }

        /// <summary>
        /// Gets or sets an optional sender name or number to filter messages by.
        /// </summary>
        public string? From { get; set; }

        /// <summary>
        /// Gets or sets an optional recipient phone number (or partial match) to filter by.
        /// </summary>
        public string? To { get; set; }

        /// <summary>
        /// Gets or sets an optional message text to filter by (exact match).
        /// </summary>
        public string? Text { get; set; }

        /// <summary>
        /// Gets or sets an optional delivery status to filter messages by.
        /// </summary>
        public SmsStatus? Status { get; set; }

        /// <summary>
        /// Gets or sets contact group IDs to filter by.
        /// </summary>
        public IReadOnlyList<int>? Groups { get; set; }

        /// <summary>
        /// Gets or sets a campaign status to filter by.
        /// </summary>
        public CampaignCommonStatus? CampaignStatus { get; set; }

        /// <summary>
        /// Gets or sets the start of the campaign creation date range.
        /// </summary>
        public DateTime? CampaignCreatedFrom { get; set; }

        /// <summary>
        /// Gets or sets the end of the campaign creation date range.
        /// </summary>
        public DateTime? CampaignCreatedTo { get; set; }

        /// <summary>
        /// Gets or sets the start of the campaign send date range.
        /// </summary>
        public DateTime? CampaignSentFrom { get; set; }

        /// <summary>
        /// Gets or sets the end of the campaign send date range.
        /// </summary>
        public DateTime? CampaignSentTo { get; set; }

        /// <summary>
        /// Gets or sets the start of the message send date range.
        /// </summary>
        public DateTime? SentFrom { get; set; }

        /// <summary>
        /// Gets or sets the end of the message send date range.
        /// </summary>
        public DateTime? SentTo { get; set; }

        /// <summary>
        /// Gets or sets the start of the status update date range.
        /// </summary>
        public DateTime? StatusUpdatedFrom { get; set; }

        /// <summary>
        /// Gets or sets the end of the status update date range.
        /// </summary>
        public DateTime? StatusUpdatedTo { get; set; }
    }
}

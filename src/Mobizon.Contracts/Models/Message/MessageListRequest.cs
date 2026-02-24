namespace Mobizon.Contracts.Models.Message
{
    /// <summary>
    /// Specifies optional filter, pagination, and sort criteria for listing SMS messages.
    /// </summary>
    public class MessageListRequest
    {
        /// <summary>
        /// Gets or sets campaign IDs to filter by (max 100).
        /// </summary>
        public string? CampaignIds { get; set; }

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
        /// Gets or sets an optional delivery status code to filter messages by.
        /// </summary>
        public int? Status { get; set; }

        /// <summary>
        /// Gets or sets contact group IDs to filter by.
        /// </summary>
        public string? Groups { get; set; }

        /// <summary>
        /// Gets or sets a campaign status to filter by.
        /// </summary>
        public string? CampaignStatus { get; set; }

        /// <summary>
        /// Gets or sets the start of the campaign creation date range (YYYY-MM-DD HH:MM:SS).
        /// </summary>
        public string? CampaignCreateTsFrom { get; set; }

        /// <summary>
        /// Gets or sets the end of the campaign creation date range (YYYY-MM-DD HH:MM:SS).
        /// </summary>
        public string? CampaignCreateTsTo { get; set; }

        /// <summary>
        /// Gets or sets the start of the campaign send date range (YYYY-MM-DD HH:MM:SS).
        /// </summary>
        public string? CampaignSentTsFrom { get; set; }

        /// <summary>
        /// Gets or sets the end of the campaign send date range (YYYY-MM-DD HH:MM:SS).
        /// </summary>
        public string? CampaignSentTsTo { get; set; }

        /// <summary>
        /// Gets or sets the start of the message send date range (YYYY-MM-DD HH:MM:SS).
        /// </summary>
        public string? StartSendTsFrom { get; set; }

        /// <summary>
        /// Gets or sets the end of the message send date range (YYYY-MM-DD HH:MM:SS).
        /// </summary>
        public string? StartSendTsTo { get; set; }

        /// <summary>
        /// Gets or sets the start of the status update date range (YYYY-MM-DD HH:MM:SS).
        /// </summary>
        public string? StatusUpdateTsFrom { get; set; }

        /// <summary>
        /// Gets or sets the end of the status update date range (YYYY-MM-DD HH:MM:SS).
        /// </summary>
        public string? StatusUpdateTsTo { get; set; }

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
}

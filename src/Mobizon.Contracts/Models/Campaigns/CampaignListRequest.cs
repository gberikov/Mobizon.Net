using Mobizon.Contracts.Models.Common;
using System.Collections.Generic;

namespace Mobizon.Contracts.Models.Campaigns
{
    /// <summary>
    /// Specifies optional search criteria, pagination and sort parameters for listing campaigns.
    /// </summary>
    public class CampaignListRequest
    {
        /// <summary>
        /// Gets or sets optional search/filter criteria.
        /// When <see langword="null"/>, no filtering is applied.
        /// </summary>
        public CampaignCriteria? Criteria { get; set; }

        /// <summary>
        /// Gets or sets optional pagination settings. When <see langword="null"/>, the API default page size is used.
        /// </summary>
        public PaginationRequest? Pagination { get; set; }

        /// <summary>
        /// Gets or sets optional sort settings. When <see langword="null"/>, the API default sort order is used.
        /// </summary>
        public SortRequest? Sort { get; set; }
    }

    /// <summary>
    /// Defines the filter criteria for the <c>campaign/list</c> API method.
    /// All fields are optional; multiple fields may be combined simultaneously.
    /// </summary>
    public class CampaignCriteria
    {
        /// <summary>Gets or sets the campaign ID to search for a single campaign.</summary>
        public int? Id { get; set; }

        /// <summary>
        /// Gets or sets a list of campaign IDs to search. Maximum 100 IDs.
        /// </summary>
        public IReadOnlyList<int>? Ids { get; set; }

        /// <summary>
        /// Gets or sets a recipient phone number (or partial number) to search campaigns by.
        /// </summary>
        public string? Recipient { get; set; }

        /// <summary>Gets or sets the sender signature to filter by.</summary>
        public string? From { get; set; }

        /// <summary>Gets or sets the exact message text to filter by.</summary>
        public string? Text { get; set; }

        /// <summary>Gets or sets the campaign status to filter by.</summary>
        public string? Status { get; set; }

        /// <summary>
        /// Gets or sets the lower bound of the campaign creation date range.
        /// Format: <c>YYYY-MM-DD HH:MM:SS</c>.
        /// </summary>
        public string? CreateTsFrom { get; set; }

        /// <summary>
        /// Gets or sets the upper bound of the campaign creation date range.
        /// Format: <c>YYYY-MM-DD HH:MM:SS</c>.
        /// </summary>
        public string? CreateTsTo { get; set; }

        /// <summary>
        /// Gets or sets the lower bound of the campaign send date range.
        /// Format: <c>YYYY-MM-DD HH:MM:SS</c>.
        /// </summary>
        public string? SentTsFrom { get; set; }

        /// <summary>
        /// Gets or sets the upper bound of the campaign send date range.
        /// Format: <c>YYYY-MM-DD HH:MM:SS</c>.
        /// </summary>
        public string? SentTsTo { get; set; }

        /// <summary>
        /// Gets or sets the campaign type to filter by:
        /// <c>1</c> — Single, <c>2</c> — Bulk (default), <c>3</c> — Template.
        /// </summary>
        public int? Type { get; set; }

        /// <summary>
        /// Gets or sets the contact-group IDs to filter campaigns by.
        /// </summary>
        public IReadOnlyList<string>? Groups { get; set; }
    }
}

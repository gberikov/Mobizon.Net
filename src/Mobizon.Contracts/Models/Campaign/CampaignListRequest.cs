using Mobizon.Contracts.Models;

namespace Mobizon.Contracts.Models.Campaign
{
    /// <summary>
    /// Specifies optional pagination and sort criteria for listing campaigns.
    /// </summary>
    public class CampaignListRequest
    {
        /// <summary>
        /// Gets or sets optional pagination settings. When <see langword="null"/>, the API default page size is used.
        /// </summary>
        public PaginationRequest? Pagination { get; set; }

        /// <summary>
        /// Gets or sets optional sort settings. When <see langword="null"/>, the API default sort order is used.
        /// </summary>
        public SortRequest? Sort { get; set; }
    }
}

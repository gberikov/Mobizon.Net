namespace Mobizon.Contracts.Models.Links
{
    /// <summary>
    /// Represents the parameters required to retrieve click statistics for one or more short links.
    /// </summary>
    public class GetLinkStatsRequest
    {
        /// <summary>
        /// Gets or sets the IDs of the links for which to retrieve statistics.
        /// </summary>
        public int[] Ids { get; set; } = System.Array.Empty<int>();

        /// <summary>
        /// Gets or sets the aggregation period for the statistics (daily or monthly).
        /// </summary>
        public LinkStatsType Type { get; set; }

        /// <summary>
        /// Gets or sets the start of the date range for the statistics query,
        /// as a string in the format expected by the API. When <see langword="null"/>, no lower bound is applied.
        /// </summary>
        public string? DateFrom { get; set; }

        /// <summary>
        /// Gets or sets the end of the date range for the statistics query,
        /// as a string in the format expected by the API. When <see langword="null"/>, no upper bound is applied.
        /// </summary>
        public string? DateTo { get; set; }
    }
}

using System;

namespace Mobizon.Contracts
{
    /// <summary>
    /// Represents the parameters required to retrieve click statistics for one or more short links.
    /// </summary>
    public class GetLinkStatsRequest
    {
        /// <summary>
        /// Gets or sets the IDs of the links for which to retrieve statistics.
        /// The API accepts a maximum of 5 IDs per request.
        /// </summary>
        public long[] Ids { get; set; } = System.Array.Empty<long>();

        /// <summary>
        /// Gets or sets the aggregation period for the statistics (daily, monthly, hourly, or minute).
        /// </summary>
        public LinkStatsType Type { get; set; }

        /// <summary>
        /// Gets or sets the start of the date range for the statistics query.
        /// When <see langword="null"/>, no lower bound is applied.
        /// </summary>
        public DateTime? DateFrom { get; set; }

        /// <summary>
        /// Gets or sets the end of the date range for the statistics query.
        /// When <see langword="null"/>, no upper bound is applied.
        /// </summary>
        public DateTime? DateTo { get; set; }
    }
}

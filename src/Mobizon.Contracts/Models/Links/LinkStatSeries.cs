using System;
using System.Collections.Generic;

namespace Mobizon.Contracts.Models.Links
{
    /// <summary>Click/redirect statistics for a single short link over the requested period range.</summary>
    public class LinkStatSeries
    {
        /// <summary>Zero-based position of this link within the requested <see cref="GetLinkStatsRequest.Ids"/>.</summary>
        public int Index { get; set; }

        /// <summary>
        /// The link ID this series belongs to, resolved by the SDK from the request's IDs by
        /// <see cref="Index"/>. <see langword="null"/> if it could not be resolved.
        /// </summary>
        public long? LinkId { get; set; }

        /// <summary>Total clicks across the whole range (from the API <c>totals</c> object).</summary>
        public int TotalClicks { get; set; }

        /// <summary>Total redirects across the whole range.</summary>
        public int TotalRedirects { get; set; }

        /// <summary>Per-period data points, in the order returned by the API.</summary>
        public IReadOnlyList<LinkStatPoint> Points { get; set; } = Array.Empty<LinkStatPoint>();
    }
}

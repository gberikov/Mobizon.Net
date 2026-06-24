using System;
using System.Collections.Generic;

namespace Mobizon.Contracts.Models.Links
{
    /// <summary>Click statistics for short links: per-period points plus total clicks.</summary>
    public class LinkStatsResult
    {
        public IReadOnlyList<LinkStatPoint> Items { get; set; } = Array.Empty<LinkStatPoint>();
        public int Totals { get; set; }
    }
}

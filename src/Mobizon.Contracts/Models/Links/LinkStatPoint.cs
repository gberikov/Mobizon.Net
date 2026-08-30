namespace Mobizon.Contracts
{
    /// <summary>A single click-statistics data point for a short link within one time bucket.</summary>
    public class LinkStatPoint
    {
        /// <summary>
        /// The period label exactly as returned by the API in the <c>param</c> field — its format
        /// depends on <see cref="GetLinkStatsRequest.Type"/> (e.g. <c>2026-03-27</c> for daily,
        /// <c>2026-06</c> for monthly, an hour or minute bucket otherwise).
        /// </summary>
        public string Param { get; set; } = string.Empty;

        /// <summary>Number of clicks in this period.</summary>
        public int Clicks { get; set; }

        /// <summary>Number of redirects in this period.</summary>
        public int Redirects { get; set; }
    }
}

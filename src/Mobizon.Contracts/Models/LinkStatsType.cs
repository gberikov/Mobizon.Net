namespace Mobizon.Contracts.Models
{
    /// <summary>
    /// Specifies the aggregation period for short-link click statistics.
    /// </summary>
    public enum LinkStatsType
    {
        /// <summary>Aggregate click statistics by day.</summary>
        Daily,

        /// <summary>Aggregate click statistics by month.</summary>
        Monthly
    }
}

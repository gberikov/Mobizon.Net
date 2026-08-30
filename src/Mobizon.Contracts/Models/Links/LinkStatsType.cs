namespace Mobizon.Contracts
{
    /// <summary>
    /// Specifies the aggregation period for short-link click statistics.
    /// </summary>
    public enum LinkStatsType
    {
        /// <summary>Aggregate click statistics by day.</summary>
        Daily,

        /// <summary>Aggregate click statistics by month.</summary>
        Monthly,

        /// <summary>Aggregate click statistics by hour.</summary>
        Hourly,

        /// <summary>Aggregate click statistics by minute.</summary>
        Minute
    }
}

namespace Mobizon.Contracts.Models.Link
{
    /// <summary>
    /// Represents click statistics for a single short link over a specific time period.
    /// </summary>
    public class LinkStatsResult
    {
        /// <summary>
        /// Gets or sets the ID of the link these statistics belong to.
        /// </summary>
        public int LinkId { get; set; }

        /// <summary>
        /// Gets or sets the date or month of the statistics entry as a string in the format returned by the API.
        /// </summary>
        public string Date { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of clicks recorded for the link in this time period.
        /// </summary>
        public int Clicks { get; set; }
    }
}

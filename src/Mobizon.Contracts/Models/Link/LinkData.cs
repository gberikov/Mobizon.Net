namespace Mobizon.Contracts.Models.Link
{
    /// <summary>
    /// Represents a Mobizon short link and its associated metadata.
    /// </summary>
    public class LinkData
    {
        /// <summary>
        /// Gets or sets the unique numeric ID of the link.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the unique short code used in the shortened URL.
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the full destination URL that the short link redirects to.
        /// </summary>
        public string FullLink { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the current status code of the link.
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// Gets or sets the expiration date of the link as returned by the API,
        /// or <see langword="null"/> if the link does not expire.
        /// </summary>
        public string? ExpirationDate { get; set; }

        /// <summary>
        /// Gets or sets an optional comment or label associated with the link.
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// Gets or sets the total number of times this short link has been clicked.
        /// </summary>
        public int Clicks { get; set; }
    }
}

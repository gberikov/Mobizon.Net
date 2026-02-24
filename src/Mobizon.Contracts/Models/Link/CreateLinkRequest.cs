namespace Mobizon.Contracts.Models.Link
{
    /// <summary>
    /// Represents the parameters required to create a new Mobizon short link.
    /// </summary>
    public class CreateLinkRequest
    {
        /// <summary>
        /// Gets or sets the full destination URL that the short link will redirect to.
        /// </summary>
        public string FullLink { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the initial status of the link.
        /// When <see langword="null"/>, the API default status is applied.
        /// </summary>
        public int? Status { get; set; }

        /// <summary>
        /// Gets or sets the optional expiration date of the link as a string in the format expected by the API.
        /// When <see langword="null"/>, the link does not expire.
        /// </summary>
        public string? ExpirationDate { get; set; }

        /// <summary>
        /// Gets or sets an optional comment or label for the link.
        /// </summary>
        public string? Comment { get; set; }
    }
}

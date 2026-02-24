namespace Mobizon.Contracts.Models.Link
{
    /// <summary>
    /// Represents the parameters required to update an existing Mobizon short link.
    /// </summary>
    public class UpdateLinkRequest
    {
        /// <summary>
        /// Gets or sets the short code that uniquely identifies the link to update.
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the new full destination URL for the link.
        /// When <see langword="null"/>, the existing URL is preserved.
        /// </summary>
        public string? FullLink { get; set; }

        /// <summary>
        /// Gets or sets the new status for the link.
        /// When <see langword="null"/>, the existing status is preserved.
        /// </summary>
        public int? Status { get; set; }

        /// <summary>
        /// Gets or sets the new expiration date of the link as a string in the format expected by the API.
        /// When <see langword="null"/>, the existing expiration date is preserved.
        /// </summary>
        public string? ExpirationDate { get; set; }

        /// <summary>
        /// Gets or sets the new comment or label for the link.
        /// When <see langword="null"/>, the existing comment is preserved.
        /// </summary>
        public string? Comment { get; set; }
    }
}

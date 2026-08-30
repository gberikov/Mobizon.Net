using System;

namespace Mobizon.Contracts
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
        public LinkStatus? Status { get; set; }

        /// <summary>
        /// Gets or sets the last day the link is valid, in the account's time zone.
        /// <see langword="null"/> = never expires.
        /// </summary>
        public DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// Gets or sets an optional comment or label for the link.
        /// </summary>
        public string? Comment { get; set; }
    }
}

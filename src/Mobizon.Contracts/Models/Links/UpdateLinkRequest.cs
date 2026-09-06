using System;

namespace Mobizon.Contracts
{
    /// <summary>
    /// Represents the parameters required to update an existing Mobizon short link.
    /// </summary>
    public class UpdateLinkRequest
    {
        /// <summary>
        /// Gets or sets the numeric ID that uniquely identifies the link to update.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the new destination URL the short link points to. Sent only when non-<see langword="null"/>.
        /// <para>
        /// Note: <c>link/update</c> does not list this field among its documented parameters (only status,
        /// expiration date and comment). The SDK still sends it when you set it, but the server may ignore it.
        /// </para>
        /// </summary>
        public string? FullLink { get; set; }

        /// <summary>
        /// Gets or sets the new status for the link. Sent only when non-<see langword="null"/>;
        /// whether the server keeps or resets an omitted status is not documented.
        /// </summary>
        public LinkStatus? Status { get; set; }

        /// <summary>
        /// Gets or sets the last day the link is valid, in the account's time zone.
        /// <para>
        /// <b>A null value does not preserve the current date.</b> The SDK omits the field, and the API
        /// documents an omitted <c>data[expirationDate]</c> as making the link valid indefinitely. To keep an
        /// existing date, read the link first (<c>Links.GetByIdAsync</c>) and pass its
        /// <see cref="LinkData.ExpirationDate"/> back. This follows the published contract; it has not been
        /// confirmed against a live account.
        /// </para>
        /// </summary>
        public DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// Gets or sets the new comment or label for the link. Sent only when non-<see langword="null"/>;
        /// whether the server keeps or clears an omitted comment is not documented.
        /// </summary>
        public string? Comment { get; set; }
    }
}

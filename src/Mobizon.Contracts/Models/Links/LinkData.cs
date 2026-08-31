using System;
using System.Text.Json.Serialization;

namespace Mobizon.Contracts
{
    /// <summary>Represents a Mobizon short link.</summary>
    public class LinkData
    {
        /// <summary>Gets or sets the unique ID of the link.</summary>
        public long Id { get; set; }

        /// <summary>Gets or sets the short code that identifies the link (the path segment of <see cref="ShortLink"/>).</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Gets or sets the full short URL (e.g. <c>https://mbzn.co/xyz</c>) that redirects to <see cref="FullLink"/>.</summary>
        [JsonPropertyName("shortLink")] public string? ShortLink { get; set; }

        /// <summary>Gets or sets the destination URL the short link redirects to.</summary>
        public string FullLink { get; set; } = string.Empty;

        /// <summary>Gets or sets the user-set status of the link (active or inactive).</summary>
        public LinkStatus Status { get; set; }

        /// <summary>Gets or sets the administrator moderation status of the link (approved or blocked).</summary>
        [JsonPropertyName("moderatorStatus")] public LinkModeratorStatus ModeratorStatus { get; set; }

        /// <summary>Gets or sets the total number of times the short link was clicked.</summary>
        [JsonPropertyName("clickCnt")] public int Clicks { get; set; }

        /// <summary>Gets or sets the total number of times a click actually redirected to <see cref="FullLink"/>.</summary>
        [JsonPropertyName("redirectCnt")] public int Redirects { get; set; }

        /// <summary>
        /// Gets or sets the expiration date requested when the link was created or last updated.
        /// <see langword="null"/> means the link was created without an expiration date.
        /// </summary>
        public DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// Gets or sets the expiration date actually enforced by the API for this link, which may differ
        /// from <see cref="ExpirationDate"/> (for example when the API applies an account-level default or
        /// maximum retention period). <see langword="null"/> means the link never expires.
        /// </summary>
        [JsonPropertyName("realExpirationDate")] public DateTime? RealExpirationDate { get; set; }

        /// <summary>Gets or sets the optional comment or label set on the link.</summary>
        public string? Comment { get; set; }

        /// <summary>Gets or sets the administrator's comment, typically explaining why the link was blocked.</summary>
        [JsonPropertyName("moderatorComment")] public string? ModeratorComment { get; set; }

        /// <summary>Gets or sets the date and time the link was created.</summary>
        [JsonPropertyName("createTs")] public DateTime? Created { get; set; }

        /// <summary>Gets or sets the date and time the link was last updated.</summary>
        [JsonPropertyName("updateTs")] public DateTime? Updated { get; set; }
    }
}

using System;

namespace Mobizon.Contracts
{
    /// <summary>Filter criteria for <c>link/list</c>.</summary>
    public class LinkListCriteria
    {
        /// <summary>Gets or sets the user-set status to filter by. <see langword="null"/> matches any status.</summary>
        public LinkStatus? Status { get; set; }

        /// <summary>Gets or sets the moderation status to filter by. <see langword="null"/> matches any status.</summary>
        public LinkModeratorStatus? ModeratorStatus { get; set; }

        /// <summary>Gets or sets the exact short code to filter by.</summary>
        public string? Code { get; set; }

        /// <summary>Gets or sets the destination URL to filter by.</summary>
        public string? FullLink { get; set; }

        /// <summary>Gets or sets the comment text to filter by.</summary>
        public string? Comment { get; set; }

        /// <summary>Gets or sets the earliest creation date and time to include (inclusive).</summary>
        public DateTime? CreatedFrom { get; set; }

        /// <summary>Gets or sets the latest creation date and time to include (inclusive).</summary>
        public DateTime? CreatedTo { get; set; }

        /// <summary>Gets or sets the minimum click count to include (inclusive).</summary>
        public int? ClicksFrom { get; set; }

        /// <summary>Gets or sets the maximum click count to include (inclusive).</summary>
        public int? ClicksTo { get; set; }
    }
}

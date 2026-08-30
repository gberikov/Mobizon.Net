using System;

namespace Mobizon.Contracts
{
    /// <summary>Filter criteria for <c>link/list</c>.</summary>
    public class LinkListCriteria
    {
        public LinkStatus? Status { get; set; }
        public LinkModeratorStatus? ModeratorStatus { get; set; }
        public string? Code { get; set; }
        public string? FullLink { get; set; }
        public string? Comment { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public int? ClicksFrom { get; set; }
        public int? ClicksTo { get; set; }
    }
}

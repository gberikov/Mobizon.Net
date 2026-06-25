namespace Mobizon.Contracts.Models.Links
{
    /// <summary>Filter criteria for <c>link/list</c>.</summary>
    public class LinkListCriteria
    {
        public int? Status { get; set; }
        public int? ModeratorStatus { get; set; }
        public string? Code { get; set; }
        public string? FullLink { get; set; }
        public string? Comment { get; set; }
        public string? CreateTsFrom { get; set; }
        public string? CreateTsTo { get; set; }
        public int? ClickCntFrom { get; set; }
        public int? ClickCntTo { get; set; }
    }
}

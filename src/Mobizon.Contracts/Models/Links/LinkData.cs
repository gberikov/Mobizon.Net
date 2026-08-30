using System;
using System.Text.Json.Serialization;

namespace Mobizon.Contracts
{
    /// <summary>Represents a Mobizon short link.</summary>
    public class LinkData
    {
        public long Id { get; set; }
        public string Code { get; set; } = string.Empty;
        [JsonPropertyName("shortLink")] public string? ShortLink { get; set; }
        public string FullLink { get; set; } = string.Empty;
        public LinkStatus Status { get; set; }
        [JsonPropertyName("moderatorStatus")] public LinkModeratorStatus ModeratorStatus { get; set; }
        [JsonPropertyName("clickCnt")] public int Clicks { get; set; }
        [JsonPropertyName("redirectCnt")] public int Redirects { get; set; }
        public string? ExpirationDate { get; set; }
        [JsonPropertyName("realExpirationDate")] public string? RealExpirationDate { get; set; }
        public string? Comment { get; set; }
        [JsonPropertyName("moderatorComment")] public string? ModeratorComment { get; set; }
        [JsonPropertyName("createTs")] public DateTime? Created { get; set; }
        [JsonPropertyName("updateTs")] public DateTime? Updated { get; set; }
    }
}

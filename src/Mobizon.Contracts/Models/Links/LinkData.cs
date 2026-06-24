using System.Text.Json.Serialization;

namespace Mobizon.Contracts.Models.Links
{
    /// <summary>Represents a Mobizon short link.</summary>
    public class LinkData
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        [JsonPropertyName("shortLink")] public string? ShortLink { get; set; }
        public string FullLink { get; set; } = string.Empty;
        public int Status { get; set; }
        [JsonPropertyName("moderatorStatus")] public int ModeratorStatus { get; set; }
        [JsonPropertyName("clickCnt")] public int ClickCnt { get; set; }
        [JsonPropertyName("redirectCnt")] public int RedirectCnt { get; set; }
        public string? ExpirationDate { get; set; }
        [JsonPropertyName("realExpirationDate")] public string? RealExpirationDate { get; set; }
        public string? Comment { get; set; }
        [JsonPropertyName("moderatorComment")] public string? ModeratorComment { get; set; }
        [JsonPropertyName("createTs")] public string? CreateTs { get; set; }
        [JsonPropertyName("updateTs")] public string? UpdateTs { get; set; }
    }
}

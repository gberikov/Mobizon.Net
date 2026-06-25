using System.Text.Json.Serialization;

namespace Mobizon.Contracts.Models.Alphanames
{
    /// <summary>A registered sender ID (alphanumeric signature) and its moderation status.</summary>
    public class AlphanameData
    {
        [JsonPropertyName("alphanameId")] public int AlphanameId { get; set; }
        [JsonPropertyName("globalStatus")] public int GlobalStatus { get; set; }
        [JsonPropertyName("partnerStatus")] public int PartnerStatus { get; set; }
        [JsonPropertyName("isDefault")] public int IsDefault { get; set; }
        [JsonPropertyName("createTs")] public string? CreateTs { get; set; }
        [JsonPropertyName("alphaname")] public AlphanameInfo? Alphaname { get; set; }
        [JsonPropertyName("details")] public AlphanameDetails? Details { get; set; }
    }

    /// <summary>The signature value itself.</summary>
    public class AlphanameInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Type { get; set; }
        [JsonPropertyName("createTs")] public string? CreateTs { get; set; }
    }

    /// <summary>Free-form details for a signature.</summary>
    public class AlphanameDetails
    {
        public string? Description { get; set; }
    }
}

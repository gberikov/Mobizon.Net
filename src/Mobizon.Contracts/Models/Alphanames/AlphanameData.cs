using System;
using System.Text.Json.Serialization;

namespace Mobizon.Contracts.Models.Alphanames
{
    /// <summary>A registered sender ID (alphanumeric signature) and its moderation status.</summary>
    public class AlphanameData
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("alphanameId")] public long AlphanameId { get; set; }
        [JsonPropertyName("globalStatus")] public int GlobalStatus { get; set; }
        [JsonPropertyName("partnerStatus")] public int PartnerStatus { get; set; }
        [JsonPropertyName("isDefault")] public bool IsDefault { get; set; }
        [JsonPropertyName("createTs")] public DateTime? Created { get; set; }
        [JsonPropertyName("alphaname")] public AlphanameInfo? Alphaname { get; set; }
        [JsonPropertyName("details")] public AlphanameDetails? Details { get; set; }
    }

    /// <summary>The signature value itself.</summary>
    public class AlphanameInfo
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Type { get; set; }
        [JsonPropertyName("createTs")] public DateTime? Created { get; set; }
    }

    /// <summary>Free-form details for a signature.</summary>
    public class AlphanameDetails
    {
        public string? Description { get; set; }
    }
}

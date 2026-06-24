using System.Text.Json.Serialization;

namespace Mobizon.Contracts.Models.Links
{
    /// <summary>A single click-statistics data point for a short link.</summary>
    public class LinkStatPoint
    {
        [JsonPropertyName("linkId")] public int LinkId { get; set; }
        public string Date { get; set; } = string.Empty;
        public int Clicks { get; set; }
    }
}

using System;
using System.Text.Json.Serialization;

namespace Mobizon.Contracts
{
    /// <summary>A registered sender ID (alphanumeric signature) and its moderation status.</summary>
    public class AlphanameData
    {
        /// <summary>Gets or sets the unique ID of this registration record.</summary>
        [JsonPropertyName("id")] public long Id { get; set; }

        /// <summary>Gets or sets the ID of the underlying signature (see <see cref="Alphaname"/>).</summary>
        [JsonPropertyName("alphanameId")] public long AlphanameId { get; set; }

        /// <summary>
        /// Raw global (platform-wide) moderation status code returned by the API; value domain not documented.
        /// </summary>
        [JsonPropertyName("globalStatus")] public int GlobalStatus { get; set; }

        /// <summary>
        /// Raw partner (account-level) moderation status code returned by the API; value domain not documented.
        /// </summary>
        [JsonPropertyName("partnerStatus")] public int PartnerStatus { get; set; }

        /// <summary>
        /// <see langword="true"/> when this is the account's default sender ID, used when a message is sent
        /// without an explicit <c>from</c> value.
        /// </summary>
        [JsonPropertyName("isDefault")] public bool IsDefault { get; set; }

        /// <summary>Gets or sets the date and time this registration was created.</summary>
        [JsonPropertyName("createTs")] public DateTime? Created { get; set; }

        /// <summary>Gets or sets the underlying signature value and its own metadata.</summary>
        [JsonPropertyName("alphaname")] public AlphanameInfo? Alphaname { get; set; }

        /// <summary>Gets or sets free-form moderation details for this registration.</summary>
        [JsonPropertyName("details")] public AlphanameDetails? Details { get; set; }
    }

    /// <summary>The signature value itself.</summary>
    public class AlphanameInfo
    {
        /// <summary>Gets or sets the unique ID of the signature.</summary>
        public long Id { get; set; }

        /// <summary>Gets or sets the sender ID text (e.g. the alphanumeric name shown to recipients).</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Raw signature type code returned by the API; value domain not documented.</summary>
        public int Type { get; set; }

        /// <summary>Gets or sets the date and time the signature was created.</summary>
        [JsonPropertyName("createTs")] public DateTime? Created { get; set; }
    }

    /// <summary>Free-form details for a signature.</summary>
    public class AlphanameDetails
    {
        /// <summary>Gets or sets the moderator-facing description or notes for the signature, if any.</summary>
        public string? Description { get; set; }
    }
}

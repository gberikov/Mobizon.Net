using System.Text.Json.Serialization;

namespace Mobizon.Contracts
{
    /// <summary>
    /// Represents the mobile phone field of a contact card.
    /// </summary>
    public class MobileFieldInfo
    {
        /// <summary>Gets or sets the phone number in international format.</summary>
        public string? Value { get; set; }

        /// <summary>
        /// Gets or sets the phone type exactly as the API spelled it (e.g. <c>HOME</c>, <c>JOB</c>, <c>MAIN</c>).
        /// Preserved verbatim so a value outside <see cref="ContactType"/> survives a read/write round trip.
        /// </summary>
        [JsonPropertyName("type")]
        [JsonConverter(typeof(PhpScalarStringConverter))]
        public string? TypeRaw { get; set; }

        /// <summary>
        /// Gets or sets the phone type, or <see langword="null"/> when <see cref="TypeRaw"/> is absent or is not
        /// one of the known <see cref="ContactType"/> values.
        /// </summary>
        [JsonIgnore]
        public ContactType? Type
        {
            get => ContactEnumText.ParseContactType(TypeRaw);
            set => TypeRaw = ContactEnumText.Format(value);
        }

        /// <summary>Gets or sets the ISO 3166-1 alpha-2 country code.</summary>
        public string? CountryA2 { get; set; }

        /// <summary>Gets or sets the country name.</summary>
        public string? CountryName { get; set; }

        /// <summary>Gets or sets the mobile operator ID.</summary>
        public string? OperatorId { get; set; }

        /// <summary>Gets or sets the mobile operator name.</summary>
        public string? Operator { get; set; }

        /// <summary>Gets or sets whether the number has been ported (MNP).</summary>
        public bool? IsMNP { get; set; }

        /// <summary>Gets or sets the original operator ID before MNP porting.</summary>
        public string? SourceOperatorId { get; set; }

        /// <summary>Gets or sets the original operator name before MNP porting.</summary>
        public string? SourceOperatorName { get; set; }
    }
}

using System.Text.Json.Serialization;

namespace Mobizon.Contracts
{
    /// <summary>
    /// Represents a simple contact field (email, viber, whatsapp, landline, skype, telegram)
    /// as returned by the API — an object with <c>value</c> and <c>type</c>.
    /// </summary>
    public class ContactFieldInfo
    {
        /// <summary>Gets or sets the field value (e.g. email address or phone number).</summary>
        public string? Value { get; set; }

        /// <summary>
        /// Gets or sets the field type exactly as the API spelled it (e.g. <c>MAIN</c>, <c>JOB</c>).
        /// Preserved verbatim so a value outside <see cref="ContactType"/> survives a read/write round trip.
        /// </summary>
        [JsonPropertyName("type")]
        [JsonConverter(typeof(PhpScalarStringConverter))]
        public string? TypeRaw { get; set; }

        /// <summary>
        /// Gets or sets the field type, or <see langword="null"/> when <see cref="TypeRaw"/> is absent or is not
        /// one of the known <see cref="ContactType"/> values.
        /// </summary>
        [JsonIgnore]
        public ContactType? Type
        {
            get => ContactEnumText.ParseContactType(TypeRaw);
            set => TypeRaw = ContactEnumText.Format(value);
        }
    }
}

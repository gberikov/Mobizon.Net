namespace Mobizon.Contracts.Models.ContactCard
{
    /// <summary>
    /// Represents the mobile phone field of a contact card.
    /// </summary>
    public class MobileFieldInfo
    {
        /// <summary>Gets or sets the phone number in international format.</summary>
        public string? Value { get; set; }

        /// <summary>Gets or sets the phone type (e.g. HOME, JOB, MAIN).</summary>
        public string? Type { get; set; }

        /// <summary>Gets or sets the ISO 3166-1 alpha-2 country code.</summary>
        public string? CountryA2 { get; set; }

        /// <summary>Gets or sets the country name.</summary>
        public string? CountryName { get; set; }

        /// <summary>Gets or sets the mobile operator ID.</summary>
        public string? OperatorId { get; set; }

        /// <summary>Gets or sets the mobile operator name.</summary>
        public string? Operator { get; set; }

        /// <summary>Gets or sets whether the number has been ported (MNP). <c>"1"</c> = ported.</summary>
        public string? IsMNP { get; set; }

        /// <summary>Gets or sets the original operator ID before MNP porting.</summary>
        public string? SourceOperatorId { get; set; }

        /// <summary>Gets or sets the original operator name before MNP porting.</summary>
        public string? SourceOperatorName { get; set; }
    }
}

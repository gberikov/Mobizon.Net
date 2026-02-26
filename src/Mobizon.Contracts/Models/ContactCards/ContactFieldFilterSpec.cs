namespace Mobizon.Contracts.Models.ContactCards
{
    /// <summary>
    /// Describes the filterable sub-fields of a structured contact field
    /// (Mobile, Email, Viber, WhatsApp, Landline, Skype, Telegram).
    /// Used in LINQ-style expressions: <c>x.Mobile.Type == PhoneType.Main</c>,
    /// <c>x.Email.Value == "user@example.com"</c>.
    /// </summary>
    public sealed class ContactFieldFilterSpec
    {
        internal static readonly ContactFieldFilterSpec Instance = new ContactFieldFilterSpec();

        /// <summary>Filters by field type (e.g. <see cref="ContactType.Main"/>).</summary>
        public ContactType? Type { get; }

        /// <summary>Filters by field value (e.g. a phone number or e-mail address).</summary>
        public string Value => string.Empty;
    }
}

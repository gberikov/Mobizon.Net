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

        /// <summary>Gets or sets the field type (e.g. <c>MAIN</c>, <c>JOB</c>).</summary>
        public ContactType? Type { get; set; }
    }
}

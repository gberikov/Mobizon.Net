namespace Mobizon.Contracts.Webhooks
{
    /// <summary>
    /// Known values of <see cref="WebhookFieldItem.FieldType"/> (documented as
    /// "<c>TEXT_STRING</c>, <c>EMAIL</c>, <c>MOBILE</c> and others"). An unrecognised value is not an error:
    /// <see cref="WebhookFieldItem.FieldTypeKind"/> is <see langword="null"/> and the raw string stays available.
    /// </summary>
    public enum WebhookFieldType
    {
        /// <summary>Free text (<c>TEXT_STRING</c>).</summary>
        TextString,

        /// <summary>E-mail address (<c>EMAIL</c>); may require confirmation.</summary>
        Email,

        /// <summary>Mobile phone number (<c>MOBILE</c>); may require confirmation.</summary>
        Mobile
    }
}

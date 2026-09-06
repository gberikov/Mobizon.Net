using System;

namespace Mobizon.Contracts
{
    /// <summary>
    /// Translates between the contact-card wire strings and their enums. An unrecognised string is not an error:
    /// it stays available on the owning DTO's raw property, so an API value the SDK does not know yet is never
    /// silently dropped or overwritten on the next update.
    /// </summary>
    internal static class ContactEnumText
    {
        /// <summary>Parses a contact field type (<c>MAIN</c>, <c>JOB</c>, …); <see langword="null"/> when unknown.</summary>
        public static ContactType? ParseContactType(string? raw) =>
            !string.IsNullOrWhiteSpace(raw) && Enum.TryParse<ContactType>(raw, ignoreCase: true, out var value)
                ? value
                : (ContactType?)null;

        /// <summary>Parses a gender (<c>male</c>, <c>female</c>); <see langword="null"/> when unknown or unset.</summary>
        public static Gender? ParseGender(string? raw) =>
            !string.IsNullOrWhiteSpace(raw) && Enum.TryParse<Gender>(raw, ignoreCase: true, out var value)
                ? value
                : (Gender?)null;

        /// <summary>Wire form of a contact field type: uppercase, invariant (tr-TR must not produce <c>MAİN</c>).</summary>
        public static string? Format(ContactType? value) =>
            value.HasValue ? value.Value.ToString().ToUpperInvariant() : null;

        /// <summary>
        /// Wire form of a gender: lowercase, invariant. <see cref="Gender.Undefined"/> and <see langword="null"/>
        /// both mean "no value".
        /// </summary>
        public static string? Format(Gender? value) =>
            value.HasValue && value.Value != Gender.Undefined ? value.Value.ToString().ToLowerInvariant() : null;
    }
}

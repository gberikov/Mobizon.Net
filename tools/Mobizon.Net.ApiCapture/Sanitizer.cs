using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Mobizon.Net.ApiCapture
{
    /// <summary>
    /// Removes personal and account data from a captured response before it is committed as a test fixture.
    /// <para>
    /// The scrub walks the parsed JSON instead of the raw text, for two reasons. Text substitution corrupted
    /// the document — a numeric <c>"id": 12345678</c> became the unquoted token <c>7000000XXXX</c> — and it
    /// only recognised what its patterns happened to describe, so Latin names, e-mail addresses and short
    /// one-time codes survived. Walking the tree lets every replacement keep the JSON type of the value it
    /// replaces, and lets a field be redacted because of its <em>name</em>, whatever language its content is in.
    /// </para>
    /// <para>
    /// The result is re-parsed before it is returned, so a scrub that produces invalid JSON fails here rather
    /// than in a test months later. This is best-effort redaction of known shapes, not a guarantee that an
    /// unreviewed capture is safe to publish: read the diff before committing fixtures.
    /// </para>
    /// </summary>
    public static class Sanitizer
    {
        private enum Replacement { Phone, Email, Text, Token, Money }

        /// <summary>Property names whose value is replaced wholesale, matched case-insensitively.</summary>
        private static readonly Dictionary<string, Replacement> SensitiveFields =
            new Dictionary<string, Replacement>(StringComparer.OrdinalIgnoreCase)
            {
                // Credentials and signatures
                ["apiKey"] = Replacement.Token,
                ["sign"] = Replacement.Token,
                ["secretKey"] = Replacement.Token,
                ["password"] = Replacement.Token,

                // Phone numbers
                ["to"] = Replacement.Phone,
                ["recipient"] = Replacement.Phone,
                ["number"] = Replacement.Phone,
                ["numberFrom"] = Replacement.Phone,
                ["numberTo"] = Replacement.Phone,
                ["msisdn"] = Replacement.Phone,

                // Identity and contact details
                ["email"] = Replacement.Email,
                ["username"] = Replacement.Email,
                ["name"] = Replacement.Text,
                ["surname"] = Replacement.Text,
                ["salutation"] = Replacement.Text,
                ["title"] = Replacement.Text,
                ["company_name"] = Replacement.Text,
                ["company_url"] = Replacement.Text,
                ["birth_date"] = Replacement.Text,
                ["city"] = Replacement.Text,
                ["street"] = Replacement.Text,
                ["building"] = Replacement.Text,
                ["postalcode"] = Replacement.Text,
                ["other"] = Replacement.Text,

                // Message bodies and free text: may carry one-time codes, order numbers, customer names
                ["text"] = Replacement.Text,
                ["highlightedText"] = Replacement.Text,
                ["comment"] = Replacement.Text,
                ["globalComment"] = Replacement.Text,
                ["partnerComment"] = Replacement.Text,
                ["moderatorComment"] = Replacement.Text,
                ["description"] = Replacement.Text,
                ["info"] = Replacement.Text,

                // Money
                ["balance"] = Replacement.Money,
            };

        /// <summary>
        /// Parents whose nested <c>value</c> field holds a phone number. A bare <c>value</c> is redacted as
        /// free text; this keeps the phone-shaped fixtures phone-shaped.
        /// </summary>
        private static readonly HashSet<string> PhoneValueParents =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "mobile", "landline", "viber", "whatsapp" };

        private const string PhonePlaceholder = "70000000000";
        private const string EmailPlaceholder = "user@example.com";
        private const string TextPlaceholder = "REDACTED";
        private const string TokenPlaceholder = "REDACTED-TOKEN";
        private const string MoneyPlaceholder = "0.0000";

        private static readonly Regex EmailLike =
            new Regex(@"[^\s""<>@]+@[^\s""<>@]+\.[A-Za-z]{2,}", RegexOptions.Compiled);

        /// <summary>Seven or more consecutive digits: phone numbers and account identifiers inside free text.</summary>
        private static readonly Regex LongDigitRun = new Regex(@"\d{7,}", RegexOptions.Compiled);

        /// <summary>
        /// Any run outside basic/extended Latin (Cyrillic, Arabic, CJK …). Captured free text is kept
        /// Latin-only; inner spaces and hyphens belong to the run, a trailing one does not, so
        /// "&lt;cyrillic&gt; Smith" becomes "REDACTED Smith" rather than "REDACTEDSmith".
        /// </summary>
        private static readonly Regex NonLatinRun =
            new Regex("[^\u0020-\u024F]+(?:[ \\-][^\u0020-\u024F]+)*", RegexOptions.Compiled);

        /// <summary>
        /// Returns <paramref name="json"/> with sensitive values replaced, preserving the JSON type of every
        /// value. Throws <see cref="JsonException"/> when the input is not JSON, or when the scrub itself
        /// produced an invalid document.
        /// </summary>
        public static string Scrub(string json)
        {
            using var document = JsonDocument.Parse(json);
            using var buffer = new MemoryStream();
            using (var writer = new Utf8JsonWriter(buffer))
            {
                WriteValue(writer, document.RootElement, propertyName: null, parentName: null);
            }

            var scrubbed = Encoding.UTF8.GetString(buffer.ToArray());

            // A fixture that does not parse is worse than an unscrubbed one: fail now, not in a later test run.
            using (JsonDocument.Parse(scrubbed))
            {
            }

            return scrubbed;
        }

        private static void WriteValue(Utf8JsonWriter writer, JsonElement element, string? propertyName, string? parentName)
        {
            var replacement = ReplacementFor(propertyName, parentName);

            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    // An object under a sensitive name (address, mobile) is redacted field by field, so the
                    // shape the fixture exists to demonstrate survives.
                    writer.WriteStartObject();
                    foreach (var property in element.EnumerateObject())
                    {
                        writer.WritePropertyName(property.Name);
                        WriteValue(writer, property.Value, property.Name, propertyName);
                    }
                    writer.WriteEndObject();
                    return;

                case JsonValueKind.Array:
                    writer.WriteStartArray();
                    foreach (var item in element.EnumerateArray())
                        WriteValue(writer, item, propertyName, parentName);
                    writer.WriteEndArray();
                    return;

                case JsonValueKind.String:
                    writer.WriteStringValue(replacement.HasValue
                        ? Placeholder(replacement.Value)
                        : ScrubText(element.GetString()));
                    return;

                case JsonValueKind.Number:
                    // Keep the JSON type: a synthetic number, never a quoted placeholder.
                    if (replacement.HasValue)
                        writer.WriteNumberValue(SyntheticNumber(replacement.Value));
                    else
                        writer.WriteRawValue(element.GetRawText());
                    return;

                default:
                    element.WriteTo(writer);
                    return;
            }
        }

        private static Replacement? ReplacementFor(string? propertyName, string? parentName)
        {
            if (propertyName == null)
                return null;

            if (string.Equals(propertyName, "value", StringComparison.OrdinalIgnoreCase))
            {
                if (parentName != null && PhoneValueParents.Contains(parentName))
                    return Replacement.Phone;
                if (string.Equals(parentName, "email", StringComparison.OrdinalIgnoreCase))
                    return Replacement.Email;
                return Replacement.Text;
            }

            return SensitiveFields.TryGetValue(propertyName, out var replacement) ? replacement : (Replacement?)null;
        }

        private static string Placeholder(Replacement replacement)
        {
            switch (replacement)
            {
                case Replacement.Phone: return PhonePlaceholder;
                case Replacement.Email: return EmailPlaceholder;
                case Replacement.Token: return TokenPlaceholder;
                case Replacement.Money: return MoneyPlaceholder;
                default: return TextPlaceholder;
            }
        }

        private static long SyntheticNumber(Replacement replacement) =>
            replacement == Replacement.Phone
                ? long.Parse(PhonePlaceholder, CultureInfo.InvariantCulture)
                : 0;

        /// <summary>
        /// Scrubs the content of a string whose property name is not itself sensitive: e-mail addresses, long
        /// digit runs and non-Latin text can turn up in any free-form field.
        /// </summary>
        private static string ScrubText(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return value ?? string.Empty;

            var result = EmailLike.Replace(value!, EmailPlaceholder);
            result = LongDigitRun.Replace(result, PhonePlaceholder);
            result = NonLatinRun.Replace(result, TextPlaceholder);
            return result;
        }
    }
}

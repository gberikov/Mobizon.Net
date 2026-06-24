using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mobizon.Net.Webhooks.Internal.Converters
{
    /// <summary>
    /// Parses Mobizon webhook timestamps in the format <c>yyyy-MM-dd HH:mm:ss</c> (no offset; treated as UTC)
    /// to <see cref="DateTimeOffset"/>. Null, empty, or unparseable values deserialize to <c>null</c>
    /// (tolerant, never throws — the raw value is preserved elsewhere for signature use).
    /// </summary>
    internal sealed class WebhookDateTimeOffsetConverter : JsonConverter<DateTimeOffset?>
    {
        private const string Format = "yyyy-MM-dd HH:mm:ss";

        public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            return ParseOrNull(reader.GetString());
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
        {
            if (value is null)
                writer.WriteNullValue();
            else
                writer.WriteStringValue(value.Value.UtcDateTime.ToString(Format, CultureInfo.InvariantCulture));
        }

        /// <summary>Parses a Mobizon timestamp string, returning <c>null</c> for empty or unparseable input.</summary>
        public static DateTimeOffset? ParseOrNull(string? s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return null;

            if (DateTimeOffset.TryParseExact(s, Format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt))
                return dt;

            return null;
        }
    }
}

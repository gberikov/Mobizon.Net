using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mobizon.Net.Internal
{
    /// <summary>
    /// Parses Mobizon API datetime strings in the format <c>YYYY-MM-DD HH:MM:SS</c> to <see cref="DateTime"/>.
    /// Null or empty strings are deserialized as <c>null</c>.
    /// </summary>
    internal class MobizonDateTimeConverter : JsonConverter<DateTime?>
    {
        private const string Format = "yyyy-MM-dd HH:mm:ss";

        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            var s = reader.GetString();

            if (string.IsNullOrWhiteSpace(s))
                return null;

            if (DateTime.TryParseExact(s, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt;

            throw new JsonException($"Cannot parse \"{s}\" as DateTime with format \"{Format}\".");
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value is null)
                writer.WriteNullValue();
            else
                writer.WriteStringValue(value.Value.ToString(Format, CultureInfo.InvariantCulture));
        }
    }
}


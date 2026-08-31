using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mobizon.Net.Internal.Converters
{
    /// <summary>
    /// Reads a string-valued enum case-insensitively. Anything that is not a recognised string
    /// (empty string, `[]`/`{}` from the PHP API for an unset field, an unknown future value)
    /// becomes <see langword="null"/> instead of failing the whole response.
    /// </summary>
    internal class TolerantStringEnumConverter<TEnum> : JsonConverter<TEnum?>
        where TEnum : struct, Enum
    {
        public override TEnum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.String)
            {
                reader.Skip();
                return null;
            }

            var s = reader.GetString();
            if (string.IsNullOrWhiteSpace(s))
                return null;

            return Enum.TryParse<TEnum>(s, ignoreCase: true, out var result) ? result : (TEnum?)null;
        }

        public override void Write(Utf8JsonWriter writer, TEnum? value, JsonSerializerOptions options)
        {
            if (value is null)
                writer.WriteNullValue();
            else
                writer.WriteStringValue(value.Value.ToString().ToUpperInvariant());
        }
    }
}

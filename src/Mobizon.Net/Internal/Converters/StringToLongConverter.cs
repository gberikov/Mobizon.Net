using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mobizon.Net.Internal.Converters
{
    /// <summary>Handles Mobizon API responses where 64-bit numeric fields are returned as JSON strings (e.g. "70000000001").</summary>
    internal class StringToLongConverter : JsonConverter<long>
    {
        public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    var s = reader.GetString();
                    return long.TryParse(s, out var result)
                        ? result
                        : throw new JsonException($"Cannot convert string \"{s}\" to long.");
                case JsonTokenType.Number:
                    return reader.GetInt64();
                default:
                    throw new JsonException($"Unexpected token type {reader.TokenType} when parsing long.");
            }
        }

        public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
            => writer.WriteNumberValue(value);
    }
}

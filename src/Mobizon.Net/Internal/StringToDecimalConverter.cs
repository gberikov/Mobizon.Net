using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mobizon.Net.Internal
{
    /// <summary>
    /// Handles Mobizon API responses where decimal fields are returned as JSON strings (e.g. "16.2000").
    /// </summary>
    internal class StringToDecimalConverter : JsonConverter<decimal>
    {
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                {
                    var s = reader.GetString();
                    return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var result)
                        ? result
                        : throw new JsonException($"Cannot convert string \"{s}\" to decimal.");
                }
                case JsonTokenType.Number:
                    return reader.GetDecimal();
                default:
                    throw new JsonException($"Unexpected token type {reader.TokenType} when parsing decimal.");
            }
        }

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }
}

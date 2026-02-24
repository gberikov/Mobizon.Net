using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mobizon.Net.Internal
{
    /// <summary>
    /// Handles Mobizon API responses where float fields are returned as JSON strings (e.g. "0.00").
    /// </summary>
    internal class StringToFloatConverter : JsonConverter<float>
    {
        public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                {
                    var s = reader.GetString();
                    return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
                        ? result
                        : throw new JsonException($"Cannot convert string \"{s}\" to float.");
                }
                case JsonTokenType.Number:
                    return reader.GetSingle();
                default:
                    throw new JsonException($"Unexpected token type {reader.TokenType} when parsing float.");
            }
        }

        public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }
}


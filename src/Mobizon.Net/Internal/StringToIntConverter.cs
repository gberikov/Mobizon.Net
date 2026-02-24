using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mobizon.Net.Internal
{
    /// <summary>
    /// Handles Mobizon API responses where numeric fields are returned as JSON strings (e.g. "123").
    /// </summary>
    internal class StringToIntConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                {
                    var s = reader.GetString();
                    return int.TryParse(s, out var result)
                        ? result
                        : throw new JsonException($"Cannot convert string \"{s}\" to int.");
                }
                case JsonTokenType.Number:
                    return reader.GetInt32();
                case JsonTokenType.None:
                case JsonTokenType.StartObject:
                case JsonTokenType.EndObject:
                case JsonTokenType.StartArray:
                case JsonTokenType.EndArray:
                case JsonTokenType.PropertyName:
                case JsonTokenType.Comment:
                case JsonTokenType.True:
                case JsonTokenType.False:
                case JsonTokenType.Null:
                default:
                    throw new JsonException($"Unexpected token type {reader.TokenType} when parsing int.");
            }
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }
}

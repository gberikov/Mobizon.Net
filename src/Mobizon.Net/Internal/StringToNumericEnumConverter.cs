using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mobizon.Net.Internal
{
    /// <summary>
    /// Converts Mobizon API responses where a numeric enum field is returned as a JSON string (e.g. "1").
    /// Works with any enum whose underlying type is <see cref="int"/>.
    /// </summary>
    internal class StringToNumericEnumConverter<T> : JsonConverter<T> where T : struct, Enum
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            int value;

            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                {
                    var s = reader.GetString();
                    if (!int.TryParse(s, out value))
                        throw new JsonException($"Cannot convert \"{s}\" to {typeof(T).Name}.");
                    break;
                }
                case JsonTokenType.Number:
                    value = reader.GetInt32();
                    break;
                default:
                    throw new JsonException($"Unexpected token {reader.TokenType} for {typeof(T).Name}.");
            }

            return (T)(object)value;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(Convert.ToInt32(value));
        }
    }
}

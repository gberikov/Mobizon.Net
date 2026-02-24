using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mobizon.Net.Internal
{
    /// <summary>
    /// Converts Mobizon API boolean fields that may arrive as strings ("0"/"1"),
    /// numbers (0/1), or native JSON booleans.
    /// </summary>
    internal class StringToBoolConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.True:  return true;
                case JsonTokenType.False: return false;
                case JsonTokenType.Number:
                    return reader.GetInt32() != 0;
                case JsonTokenType.String:
                {
                    var s = reader.GetString();
                    switch (s)
                    {
                        case "0": case "false": case "False": case "FALSE": return false;
                        case "1": case "true":  case "True":  case "TRUE":  return true;
                        default:
                            throw new JsonException($"Cannot convert \"{s}\" to bool.");
                    }
                }
                default:
                    throw new JsonException($"Unexpected token {reader.TokenType} when parsing bool.");
            }
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            writer.WriteBooleanValue(value);
        }
    }
}

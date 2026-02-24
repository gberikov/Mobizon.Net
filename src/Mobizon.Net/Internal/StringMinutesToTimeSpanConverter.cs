using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mobizon.Net.Internal
{
    /// <summary>
    /// Converts Mobizon API validity fields returned as minutes (string or number)
    /// to <see cref="TimeSpan"/> and back.
    /// E.g. <c>"1440"</c> → <c>TimeSpan.FromMinutes(1440)</c>.
    /// </summary>
    internal class StringMinutesToTimeSpanConverter : JsonConverter<TimeSpan?>
    {
        public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            int minutes;

            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                {
                    var s = reader.GetString();
                    if (string.IsNullOrWhiteSpace(s))
                        return null;
                    if (!int.TryParse(s, out minutes))
                        throw new JsonException($"Cannot convert \"{s}\" to TimeSpan (expected minutes).");
                    break;
                }
                case JsonTokenType.Number:
                    minutes = reader.GetInt32();
                    break;
                default:
                    throw new JsonException($"Unexpected token {reader.TokenType} when parsing TimeSpan.");
            }

            return TimeSpan.FromMinutes(minutes);
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options)
        {
            if (value is null)
                writer.WriteNullValue();
            else
                writer.WriteNumberValue((int)value.Value.TotalMinutes);
        }
    }
}

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mobizon.Net.Webhooks.Internal.Converters
{
    /// <summary>
    /// Reads a nullable boolean that Mobizon may send as a JSON boolean, a number (<c>1</c>/<c>0</c>),
    /// or a string (<c>"1"</c>/<c>"0"</c>/<c>"true"</c>/<c>"false"</c>). Unrecognised values become <c>null</c>.
    /// </summary>
    internal sealed class FlexibleBoolConverter : JsonConverter<bool?>
    {
        public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    return null;
                case JsonTokenType.True:
                    return true;
                case JsonTokenType.False:
                    return false;
                case JsonTokenType.Number:
                    return reader.TryGetInt64(out var n) ? n != 0 : (bool?)null;
                case JsonTokenType.String:
                    var s = reader.GetString();
                    if (string.Equals(s, "1", StringComparison.Ordinal) || string.Equals(s, "true", StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (string.Equals(s, "0", StringComparison.Ordinal) || string.Equals(s, "false", StringComparison.OrdinalIgnoreCase))
                        return false;
                    return null;
                default:
                    return null;
            }
        }

        public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
        {
            if (value is null)
                writer.WriteNullValue();
            else
                writer.WriteBooleanValue(value.Value);
        }
    }
}

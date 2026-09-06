using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mobizon.Contracts
{
    /// <summary>
    /// Reads a contact-card scalar that the PHP API may leave unset. An empty string or an empty array (PHP's
    /// rendering of an unset associative value) both mean "no value"; a number is kept as its invariant text so
    /// the value is never lost. Any other shape is a protocol error rather than a silent <see langword="null"/>.
    /// </summary>
    internal sealed class PhpScalarStringConverter : JsonConverter<string?>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    return null;

                case JsonTokenType.String:
                    var text = reader.GetString();
                    return string.IsNullOrWhiteSpace(text) ? null : text;

                case JsonTokenType.Number:
                    return reader.TryGetInt64(out var integer)
                        ? integer.ToString(CultureInfo.InvariantCulture)
                        : reader.GetDouble().ToString(CultureInfo.InvariantCulture);

                case JsonTokenType.StartArray:
                    reader.Read();
                    if (reader.TokenType == JsonTokenType.EndArray)
                        return null;
                    throw new JsonException("Expected a contact-card scalar, found a non-empty array.");

                default:
                    throw new JsonException($"Expected a contact-card scalar, found {reader.TokenType}.");
            }
        }

        public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
        {
            if (value is null)
                writer.WriteNullValue();
            else
                writer.WriteStringValue(value);
        }
    }
}

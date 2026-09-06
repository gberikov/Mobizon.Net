using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mobizon.Net.Internal.Converters
{
    /// <summary>
    /// Reads a list of IDs in either shape the API uses: a JSON array (<c>link/delete</c> answers
    /// <c>["1","2"]</c>) or a comma-separated string (<c>campaign/get</c> documents <c>groups</c> as
    /// <c>"12,34"</c>). Empty string and empty array both mean "no IDs"; anything that is not a number is a
    /// protocol error rather than a silently dropped element.
    /// </summary>
    internal sealed class CsvOrArrayLongListConverter : JsonConverter<IReadOnlyList<long>>
    {
        public override IReadOnlyList<long>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    return null;

                case JsonTokenType.Number:
                    return new[] { reader.GetInt64() };

                case JsonTokenType.String:
                    return ParseCsv(reader.GetString());

                case JsonTokenType.StartArray:
                    var items = new List<long>();
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                        items.Add(ReadElement(ref reader));
                    return items;

                default:
                    throw new JsonException($"Expected a list of identifiers, found {reader.TokenType}.");
            }
        }

        public override void Write(Utf8JsonWriter writer, IReadOnlyList<long> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var item in value)
                writer.WriteNumberValue(item);
            writer.WriteEndArray();
        }

        private static long ReadElement(ref Utf8JsonReader reader)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Number:
                    return reader.GetInt64();
                case JsonTokenType.String:
                    var text = reader.GetString();
                    if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                        return parsed;
                    break;
            }

            throw new JsonException("Identifier list contains a value that is not a 64-bit integer.");
        }

        private static IReadOnlyList<long> ParseCsv(string? csv)
        {
            if (string.IsNullOrWhiteSpace(csv))
                return Array.Empty<long>();

            var parts = csv!.Split(',');
            var result = new List<long>(parts.Length);
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.Length == 0)
                    continue;
                if (!long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                    throw new JsonException("Comma-separated identifier list contains a value that is not a 64-bit integer.");
                result.Add(value);
            }

            return result;
        }
    }
}

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mobizon.Contracts.Models.ContactCards;

namespace Mobizon.Net.Internal.Converters
{
    /// <summary>
    /// Deserializes <see cref="ContactType"/> from the Mobizon API uppercase strings
    /// (e.g. <c>"MAIN"</c>, <c>"ADDITIONAL"</c>, <c>"JOB"</c>, <c>"HOME"</c>).
    /// </summary>
    internal class ContactTypeConverter : JsonConverter<ContactType?>
    {
        public override ContactType? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            var s = reader.GetString();

            if (string.IsNullOrWhiteSpace(s))
                return null;

            if (Enum.TryParse<ContactType>(s, ignoreCase: true, out var result))
                return result;

            throw new JsonException($"Cannot convert \"{s}\" to {nameof(ContactType)}.");
        }

        public override void Write(Utf8JsonWriter writer, ContactType? value, JsonSerializerOptions options)
        {
            if (value is null)
                writer.WriteNullValue();
            else
                writer.WriteStringValue(value.Value.ToString().ToUpperInvariant());
        }
    }
}

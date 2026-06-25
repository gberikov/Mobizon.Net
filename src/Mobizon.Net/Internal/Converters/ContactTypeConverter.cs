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

            // An empty array/object/string is how the PHP API may serialise an unset field — treat
            // anything that is not a recognised type string as "no type" rather than failing the
            // whole contactcard read. Unknown/future type values (e.g. a new "WORK") also degrade to
            // null so one field can't break deserialisation of the entire response.
            if (reader.TokenType != JsonTokenType.String)
            {
                reader.Skip();
                return null;
            }

            var s = reader.GetString();

            if (string.IsNullOrWhiteSpace(s))
                return null;

            if (Enum.TryParse<ContactType>(s, ignoreCase: true, out var result))
                return result;

            return null;
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

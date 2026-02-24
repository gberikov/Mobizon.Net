using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mobizon.Contracts.Models.Campaign;

namespace Mobizon.Net.Internal
{
    /// <summary>
    /// Converts Mobizon API message type strings (e.g. "SMS") to <see cref="MessageType"/> enum values.
    /// </summary>
    internal class MessageTypeConverter : JsonConverter<MessageType>
    {
        public override MessageType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            switch (value)
            {
                case "SMS": return MessageType.Sms;
                default:
                    throw new JsonException($"Unknown MessageType value: \"{value}\".");
            }
        }

        public override void Write(Utf8JsonWriter writer, MessageType value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case MessageType.Sms: writer.WriteStringValue("SMS"); break;
                default:
                    throw new JsonException($"Unknown MessageType value: {value}.");
            }
        }
    }
}

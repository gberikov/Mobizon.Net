using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mobizon.Contracts.Models.Message;

namespace Mobizon.Net.Internal
{
    /// <summary>
    /// Converts the Mobizon API status strings (e.g. "NEW", "DELIVRD") to <see cref="SmsStatus"/> enum values.
    /// </summary>
    internal class SmsStatusConverter : JsonConverter<SmsStatus>
    {
        public override SmsStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            switch (value)
            {
                case "NEW":     return SmsStatus.New;
                case "ENQUEUD": return SmsStatus.Enqueued;
                case "ACCEPTD": return SmsStatus.Accepted;
                case "DELIVRD": return SmsStatus.Delivered;
                case "UNDELIV": return SmsStatus.Undelivered;
                case "REJECTD": return SmsStatus.Rejected;
                case "EXPIRD":  return SmsStatus.Expired;
                case "DELETED": return SmsStatus.Deleted;
                default:
                    throw new JsonException($"Unknown SmsStatus value: \"{value}\".");
            }
        }

        public override void Write(Utf8JsonWriter writer, SmsStatus value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case SmsStatus.New:         writer.WriteStringValue("NEW");     break;
                case SmsStatus.Enqueued:    writer.WriteStringValue("ENQUEUD"); break;
                case SmsStatus.Accepted:    writer.WriteStringValue("ACCEPTD"); break;
                case SmsStatus.Delivered:   writer.WriteStringValue("DELIVRD"); break;
                case SmsStatus.Undelivered: writer.WriteStringValue("UNDELIV"); break;
                case SmsStatus.Rejected:    writer.WriteStringValue("REJECTD"); break;
                case SmsStatus.Expired:     writer.WriteStringValue("EXPIRD");  break;
                case SmsStatus.Deleted:     writer.WriteStringValue("DELETED"); break;
                default:
                    throw new JsonException($"Unknown SmsStatus value: {value}.");
            }
        }
    }
}


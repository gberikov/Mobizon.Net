using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mobizon.Contracts.Models.Campaign;

namespace Mobizon.Net.Internal
{
    /// <summary>
    /// Converts Mobizon API campaign status strings (e.g. "DONE", "READY_FOR_SEND")
    /// to <see cref="CampaignCommonStatus"/> enum values.
    /// </summary>
    internal class CampaignCommonStatusConverter : JsonConverter<CampaignCommonStatus>
    {
        public override CampaignCommonStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            switch (value)
            {
                case "MODERATION":          return CampaignCommonStatus.Moderation;
                case "DECLINED":            return CampaignCommonStatus.Declined;
                case "READY_FOR_SEND":      return CampaignCommonStatus.ReadyForSend;
                case "AUTO_READY_FOR_SEND": return CampaignCommonStatus.AutoReadyForSend;
                case "RUNNING":             return CampaignCommonStatus.Running;
                case "SENT":                return CampaignCommonStatus.Sent;
                case "DONE":                return CampaignCommonStatus.Done;
                default:
                    throw new JsonException($"Unknown CampaignCommonStatus value: \"{value}\".");
            }
        }

        public override void Write(Utf8JsonWriter writer, CampaignCommonStatus value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case CampaignCommonStatus.Moderation:        writer.WriteStringValue("MODERATION");          break;
                case CampaignCommonStatus.Declined:          writer.WriteStringValue("DECLINED");            break;
                case CampaignCommonStatus.ReadyForSend:      writer.WriteStringValue("READY_FOR_SEND");      break;
                case CampaignCommonStatus.AutoReadyForSend:  writer.WriteStringValue("AUTO_READY_FOR_SEND"); break;
                case CampaignCommonStatus.Running:           writer.WriteStringValue("RUNNING");             break;
                case CampaignCommonStatus.Sent:              writer.WriteStringValue("SENT");                break;
                case CampaignCommonStatus.Done:              writer.WriteStringValue("DONE");                break;
                default:
                    throw new JsonException($"Unknown CampaignCommonStatus value: {value}.");
            }
        }
    }
}

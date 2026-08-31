using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mobizon.Contracts.Webhooks;

namespace Mobizon.Net.Webhooks.Internal.Converters
{
    /// <summary>
    /// Reads the <c>data</c> object of an <c>sms-delivery-report</c>, mapping the single <c>status</c>
    /// field to both <see cref="SmsDeliveryReport.Status"/> (typed) and <see cref="SmsDeliveryReport.StatusRaw"/>
    /// (verbatim). Unknown fields are skipped.
    /// </summary>
    internal sealed class SmsDeliveryReportConverter : JsonConverter<SmsDeliveryReport>
    {
        public override SmsDeliveryReport Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected start of object for sms-delivery-report data.");

            var report = new SmsDeliveryReport();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return report;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;

                var prop = reader.GetString();
                reader.Read();

                switch (prop)
                {
                    case "campaignId":
                        report.CampaignId = ReadInt64(ref reader);
                        break;
                    case "messageId":
                        report.MessageId = ReadInt64(ref reader);
                        break;
                    case "segNum":
                        report.Segments = (int)ReadInt64(ref reader);
                        break;
                    case "statusUpdateTs":
                        report.StatusUpdateTs = WebhookDateTimeOffsetConverter.ParseOrNull(
                            reader.TokenType == JsonTokenType.Null ? null : reader.GetString());
                        break;
                    case "status":
                        var s = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
                        report.StatusRaw = s ?? string.Empty;
                        report.Status = WebhookSmsStatusConverter.Map(s);
                        break;
                    case "to":
                        report.To = (reader.TokenType == JsonTokenType.Null ? null : reader.GetString()) ?? string.Empty;
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            throw new JsonException("Unexpected end of JSON while reading sms-delivery-report data.");
        }

        public override void Write(Utf8JsonWriter writer, SmsDeliveryReport value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("campaignId", value.CampaignId);
            writer.WriteNumber("messageId", value.MessageId);
            writer.WriteNumber("segNum", value.Segments);
            writer.WriteString("statusUpdateTs", value.StatusUpdateTs?.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            writer.WriteString("status", value.StatusRaw);
            writer.WriteString("to", value.To);
            writer.WriteEndObject();
        }

        private static long ReadInt64(ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out var v))
                return v;
            if (reader.TokenType == JsonTokenType.String && long.TryParse(reader.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var sv))
                return sv;
            return 0;
        }
    }
}

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mobizon.Contracts.Exceptions;
using Mobizon.Contracts.Models.Webhooks;
using Mobizon.Contracts.Services;
using Mobizon.Net.Webhooks.Internal.Converters;

namespace Mobizon.Net.Webhooks
{
    /// <summary>
    /// Default <see cref="IWebhookParser"/>. Parses the common envelope, dispatches by <c>eventType</c>
    /// to a typed event, and falls back to <see cref="UnknownWebhookEvent"/> for unrecognised types.
    /// Tolerates unknown/extra fields.
    /// </summary>
    public sealed class WebhookParser : IWebhookParser
    {
        private readonly JsonSerializerOptions _options;

        /// <summary>Creates a parser with the default Mobizon webhook serialization settings.</summary>
        public WebhookParser()
        {
            _options = new JsonSerializerOptions
            {
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };
            _options.Converters.Add(new WebhookDateTimeOffsetConverter());
            _options.Converters.Add(new FlexibleBoolConverter());
            _options.Converters.Add(new SmsDeliveryReportConverter());
        }

        /// <inheritdoc />
        public MobizonWebhookEvent Parse(string jsonBody)
        {
            if (string.IsNullOrWhiteSpace(jsonBody))
                throw new WebhookParseException("Webhook body is empty.");

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(jsonBody);
            }
            catch (JsonException ex)
            {
                throw new WebhookParseException("Webhook body is not valid JSON.", ex);
            }

            using (doc)
            {
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                    throw new WebhookParseException("Webhook body must be a JSON object.");

                var eventTypeRaw = GetRequiredString(root, "eventType");
                var knownType = MapEventType(eventTypeRaw);

                var hasData = root.TryGetProperty("data", out var dataElement);

                MobizonWebhookEvent evt;
                switch (knownType)
                {
                    case WebhookEventType.SmsDeliveryReport:
                        evt = new SmsDeliveryReportEvent { Data = DeserializeData<SmsDeliveryReport>(hasData, dataElement) };
                        break;
                    case WebhookEventType.FormSubmission:
                        evt = new FormSubmissionEvent { Data = DeserializeData<FormSubmission>(hasData, dataElement) };
                        break;
                    case WebhookEventType.FormContactConfirmation:
                        evt = new FormContactConfirmationEvent { Data = DeserializeData<FormContactConfirmation>(hasData, dataElement) };
                        break;
                    case WebhookEventType.FormContactUnsubscribe:
                        evt = new FormContactUnsubscribeEvent { Data = DeserializeData<FormContactUnsubscribe>(hasData, dataElement) };
                        break;
                    default:
                        evt = new UnknownWebhookEvent { RawData = hasData ? dataElement.Clone() : default };
                        break;
                }

                evt.EventType = knownType;
                evt.EventTypeRaw = eventTypeRaw;
                evt.EventId = GetRequiredInt64(root, "eventId");
                evt.Attempt = (int)GetRequiredInt64(root, "attempt");
                evt.WebhookId = GetOptionalInt64(root, "webhookId");
                evt.EventCreateTsRaw = GetRequiredString(root, "eventCreateTs");
                evt.EventCreateTs = WebhookDateTimeOffsetConverter.ParseOrNull(evt.EventCreateTsRaw);
                evt.Sign = GetOptionalString(root, "sign");

                return evt;
            }
        }

        /// <inheritdoc />
        public bool TryParse(string jsonBody, out MobizonWebhookEvent? @event)
        {
            try
            {
                @event = Parse(jsonBody);
                return true;
            }
            catch (WebhookParseException)
            {
                @event = null;
                return false;
            }
        }

        private T DeserializeData<T>(bool hasData, JsonElement element)
            where T : new()
        {
            if (!hasData || element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
                return new T();

            try
            {
                return element.Deserialize<T>(_options) ?? new T();
            }
            catch (JsonException ex)
            {
                throw new WebhookParseException($"Failed to parse webhook data as {typeof(T).Name}.", ex);
            }
        }

        private static WebhookEventType MapEventType(string raw)
        {
            switch (raw)
            {
                case "sms-delivery-report": return WebhookEventType.SmsDeliveryReport;
                case "form-submission": return WebhookEventType.FormSubmission;
                case "form-contact-confirmation": return WebhookEventType.FormContactConfirmation;
                case "form-contact-unsubscribe": return WebhookEventType.FormContactUnsubscribe;
                default: return WebhookEventType.Unknown;
            }
        }

        private static string GetRequiredString(JsonElement root, string name)
        {
            if (!root.TryGetProperty(name, out var p) || p.ValueKind == JsonValueKind.Null)
                throw new WebhookParseException($"Missing required field '{name}'.");
            if (p.ValueKind != JsonValueKind.String)
                throw new WebhookParseException($"Field '{name}' must be a string.");
            return p.GetString() ?? string.Empty;
        }

        private static long GetRequiredInt64(JsonElement root, string name)
        {
            if (!root.TryGetProperty(name, out var p) || p.ValueKind == JsonValueKind.Null)
                throw new WebhookParseException($"Missing required field '{name}'.");
            if (p.ValueKind == JsonValueKind.Number && p.TryGetInt64(out var v))
                return v;
            if (p.ValueKind == JsonValueKind.String && long.TryParse(p.GetString(), out var sv))
                return sv;
            throw new WebhookParseException($"Field '{name}' must be an integer.");
        }

        private static long GetOptionalInt64(JsonElement root, string name)
        {
            if (!root.TryGetProperty(name, out var p))
                return 0;
            if (p.ValueKind == JsonValueKind.Number && p.TryGetInt64(out var v))
                return v;
            if (p.ValueKind == JsonValueKind.String && long.TryParse(p.GetString(), out var sv))
                return sv;
            return 0;
        }

        private static string GetOptionalString(JsonElement root, string name)
        {
            if (!root.TryGetProperty(name, out var p) || p.ValueKind != JsonValueKind.String)
                return string.Empty;
            return p.GetString() ?? string.Empty;
        }
    }
}

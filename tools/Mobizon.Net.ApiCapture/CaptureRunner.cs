using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mobizon.Net.ApiCapture
{
    public sealed class CaptureRunner
    {
        private readonly RawMobizonApi _api;
        private readonly string _outDir;
        private readonly string _testRecipient;
        private readonly string _testGroupId;
        private readonly string _sender;

        public CaptureRunner(
            RawMobizonApi api,
            string outDir,
            string testRecipient,
            string testGroupId,
            string sender = "")
        {
            _api = api;
            _outDir = outDir;
            _testRecipient = testRecipient;
            _testGroupId = testGroupId;
            _sender = sender;
        }

        private async Task Capture(string module, string method, IDictionary<string, string>? form = null)
        {
            var json = await _api.CallAsync(module, method, form);
            Directory.CreateDirectory(_outDir);
            var path = Path.Combine(_outDir, $"{module}.{method}.json");
            File.WriteAllText(path, json);
            Console.WriteLine($"[capture] {module}/{method} -> {path}");
        }

        public async Task RunAsync(bool send = false)
        {
            // ── Tier 1 (always) ──────────────────────────────────────────────────────
            await Capture("user", "getOwnBalance");
            await Capture("message", "list");
            await Capture("campaign", "list");
            await Capture("link", "list");
            await Capture("alphaname", "list");

            // After link/list: if any link id present, also capture link/getStats
            var linkListJson = File.ReadAllText(Path.Combine(_outDir, "link.list.json"));
            string? firstLinkId = TryReadFirstDataId(linkListJson, "id");
            if (firstLinkId != null)
            {
                await Capture("link", "getStats", new Dictionary<string, string>
                {
                    ["ids[0]"] = firstLinkId,
                    ["type"] = "daily"
                });
            }
            else
            {
                Console.WriteLine("[capture] link/getStats skipped — no link id from list");
            }

            // ── Tier 2 (default on, free) ─────────────────────────────────────────
            string? newLinkId = null;
            string? newLinkCode = null;

            var createJson = await _api.CallAsync("link", "create", new Dictionary<string, string>
            {
                ["data[fullLink]"] = "https://example.com/capture"
            });
            WriteCapture("link.create.json", createJson);

            newLinkId = TryReadDataField(createJson, "id");
            newLinkCode = TryReadDataField(createJson, "code");

            if (newLinkCode != null)
            {
                var getJson = await _api.CallAsync("link", "get", new Dictionary<string, string>
                {
                    ["code"] = newLinkCode
                });
                WriteCapture("link.get.json", getJson);
            }
            else
            {
                Console.WriteLine("[capture] link/get skipped — no code from link/create");
            }

            if (newLinkId != null)
            {
                var statsJson = await _api.CallAsync("link", "getStats", new Dictionary<string, string>
                {
                    ["ids[0]"] = newLinkId,
                    ["type"] = "daily"
                });
                WriteCapture("link.getStats.new.json", statsJson);

                var updateJson = await _api.CallAsync("link", "update", new Dictionary<string, string>
                {
                    ["id"] = newLinkId,
                    ["data[comment]"] = "capture"
                });
                WriteCapture("link.update.json", updateJson);

                var deleteJson = await _api.CallAsync("link", "delete", new Dictionary<string, string>
                {
                    ["ids[0]"] = newLinkId
                });
                WriteCapture("link.delete.json", deleteJson);
            }
            else
            {
                Console.WriteLine("[capture] link/getStats+update+delete skipped — no id from link/create");
            }

            // ── Tier 3 (only if send==true) ───────────────────────────────────────
            if (!send)
            {
                Console.WriteLine("[capture] Tier 3 skipped (--send not specified)");
                return;
            }

            if (string.IsNullOrWhiteSpace(_testRecipient))
            {
                Console.WriteLine("[capture] Tier 3 skipped — no TestRecipient configured");
                return;
            }

            // Log balance before sends
            Console.WriteLine("[capture] Tier 3: logging balance before sends...");
            await Capture("user", "getOwnBalance");

            // Send single SMS
            var sendJson = await _api.CallAsync("message", "sendSmsMessage", new Dictionary<string, string>
            {
                ["recipient"] = _testRecipient,
                ["text"] = "capture test"
            });
            WriteCapture("message.sendSmsMessage.json", sendJson);

            string? messageId = TryReadDataField(sendJson, "messageId");
            if (messageId == null)
                messageId = TryReadDataField(sendJson, "id");

            if (messageId != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    await Task.Delay(2000);
                    var statusJson = await _api.CallAsync("message", "getSMSStatus", new Dictionary<string, string>
                    {
                        ["ids[0]"] = messageId
                    });
                    WriteCapture($"message.getSMSStatus.poll{i + 1}.json", statusJson);
                }
            }
            else
            {
                Console.WriteLine("[capture] message/getSMSStatus skipped — no messageId from sendSmsMessage");
            }

            // Campaign lifecycle
            var campaignCreateJson = await _api.CallAsync("campaign", "create", new Dictionary<string, string>
            {
                ["data[type]"] = "2",
                ["data[text]"] = "capture test"
            });
            WriteCapture("campaign.create.json", campaignCreateJson);

            string? campaignId = TryReadDataField(campaignCreateJson, "id");
            if (campaignId != null)
            {
                // Add recipients — hard cap: max 5
                var addRecipientsJson = await _api.CallAsync("campaign", "addRecipients", new Dictionary<string, string>
                {
                    ["id"] = campaignId,
                    ["recipients[0][recipient]"] = _testRecipient
                });
                WriteCapture("campaign.addRecipients.json", addRecipientsJson);

                var campaignSendJson = await _api.CallAsync("campaign", "send", new Dictionary<string, string>
                {
                    ["id"] = campaignId
                });
                WriteCapture("campaign.send.json", campaignSendJson);

                // Poll taskqueue only for a genuinely queued send. A synchronous send answers code 0 with a
                // result scalar that is not a task id, and getStatus takes a single `id`, not an `ids[]` array.
                string? taskId = TryReadQueuedTaskId(campaignSendJson);

                if (taskId != null)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        await Task.Delay(2000);
                        var taskStatusJson = await _api.CallAsync("taskqueue", "getStatus", new Dictionary<string, string>
                        {
                            ["id"] = taskId
                        });
                        WriteCapture($"taskqueue.getStatus.poll{i + 1}.json", taskStatusJson);
                    }
                }
                else
                {
                    Console.WriteLine("[capture] taskqueue/getStatus skipped — campaign/send did not queue a background task (code 100)");
                }

                var campaignInfoJson = await _api.CallAsync("campaign", "getInfo", new Dictionary<string, string>
                {
                    ["id"] = campaignId
                });
                WriteCapture("campaign.getInfo.json", campaignInfoJson);

                var campaignDeleteJson = await _api.CallAsync("campaign", "delete", new Dictionary<string, string>
                {
                    ["id"] = campaignId
                });
                WriteCapture("campaign.delete.json", campaignDeleteJson);
            }
            else
            {
                Console.WriteLine("[capture] campaign lifecycle skipped — no id from campaign/create");
            }

            // Log balance after sends
            Console.WriteLine("[capture] Tier 3: logging balance after sends...");
            await Capture("user", "getOwnBalance");
        }

        private void WriteCapture(string filename, string json)
        {
            Directory.CreateDirectory(_outDir);
            var path = Path.Combine(_outDir, filename);
            File.WriteAllText(path, json);
            Console.WriteLine($"[capture] -> {path}");
        }

        /// <summary>
        /// Reads the first item in data array, returns the named field as string (or null).
        /// Expects shape: { "data": [ { "id": ..., ... }, ... ] } or { "data": { "id": ... } }
        /// </summary>
        private static string? TryReadFirstDataId(string json, string field)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("data", out var data))
                    return null;

                if (data.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in data.EnumerateArray())
                    {
                        if (item.TryGetProperty(field, out var val))
                            return val.ToString();
                    }
                }
                else if (data.ValueKind == JsonValueKind.Object)
                {
                    if (data.TryGetProperty(field, out var val))
                        return val.ToString();

                    // List endpoints wrap rows in data.items[]
                    if (data.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in items.EnumerateArray())
                        {
                            if (item.TryGetProperty(field, out var itemVal))
                                return itemVal.ToString();
                        }
                    }
                }

                return null;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[capture] JSON parse error reading first data.{field}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Reads a named field from the data object (not array).
        /// </summary>
        /// <summary>
        /// Background task id of a queued operation. Only response code 100 carries one, and <c>data</c> is then
        /// the bare identifier; any other code means the operation ran synchronously and there is nothing to poll.
        /// </summary>
        private static string? TryReadQueuedTaskId(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("code", out var code))
                    return null;

                int codeValue;
                if (code.ValueKind == JsonValueKind.Number)
                    codeValue = code.GetInt32();
                else if (code.ValueKind != JsonValueKind.String || !int.TryParse(code.GetString(), out codeValue))
                    return null;

                if (codeValue != 100 || !root.TryGetProperty("data", out var data))
                    return null;

                if (data.ValueKind == JsonValueKind.Number)
                    return data.GetRawText();

                return data.ValueKind == JsonValueKind.String ? data.GetString() : null;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[capture] JSON parse error reading the queued task id: {ex.Message}");
                return null;
            }
        }

        private static string? TryReadDataField(string json, string field)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("data", out var data))
                    return null;

                if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty(field, out var val))
                    return val.ToString();

                // Some endpoints return data as a scalar (e.g., an id directly)
                if (data.ValueKind == JsonValueKind.Number || data.ValueKind == JsonValueKind.String)
                {
                    if (field == "id")
                        return data.ToString();
                }

                return null;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[capture] JSON parse error reading data.{field}: {ex.Message}");
                return null;
            }
        }
    }
}

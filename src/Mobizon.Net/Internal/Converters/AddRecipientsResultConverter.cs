using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mobizon.Contracts;

namespace Mobizon.Net.Internal.Converters
{
    /// <summary>
    /// Handles the dual shape of the <c>campaign/addRecipients</c> <c>data</c> field:
    /// <list type="bullet">
    ///   <item>
    ///     <term>Array (synchronous)</term>
    ///     <description>An array of per-recipient results; mapped to <see cref="AddRecipientsResult.Entries"/>.</description>
    ///   </item>
    ///   <item>
    ///     <term>Scalar number (asynchronous, code 100)</term>
    ///     <description>A background task ID; mapped to <see cref="AddRecipientsResult.TaskId"/>.</description>
    ///   </item>
    /// </list>
    /// Any other shape is a protocol error (<see cref="JsonException"/>), never an empty success.
    /// </summary>
    internal sealed class AddRecipientsResultConverter : JsonConverter<AddRecipientsResult>
    {
        public override AddRecipientsResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var result = new AddRecipientsResult();

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                result.Entries = JsonSerializer.Deserialize<List<AddRecipientEntry>>(ref reader, options);
            }
            else if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out var taskId))
            {
                result.TaskId = taskId;
            }
            else if (reader.TokenType == JsonTokenType.String && long.TryParse(reader.GetString(), out var taskIdFromString))
            {
                result.TaskId = taskIdFromString;
            }
            else
            {
                // Neither the documented per-recipient array nor a task id: surface it instead of returning
                // an empty (false-success) result.
                throw new JsonException("campaign/addRecipients data is neither a per-recipient result array nor a background task id.");
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, AddRecipientsResult value, JsonSerializerOptions options)
            => throw new NotSupportedException($"{nameof(AddRecipientsResultConverter)} does not support writing.");
    }
}

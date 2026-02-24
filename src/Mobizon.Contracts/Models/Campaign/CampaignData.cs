using System.Collections.Generic;
using System.Text.Json.Serialization;
using Mobizon.Contracts.Models.Message;

namespace Mobizon.Contracts.Models.Campaign
{
    /// <summary>
    /// Represents the full data of an SMS campaign as returned by the Mobizon API
    /// (<c>campaign/get</c> and <c>campaign/list</c>).
    /// </summary>
    public class CampaignData
    {
        /// <summary>Gets or sets the unique ID of the campaign.</summary>
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the moderation status of the campaign:
        /// <c>MODERATION</c>, <c>DECLINED</c>, <c>READY_FOR_SEND</c>, <c>AUTO_READY_FOR_SEND</c>.
        /// </summary>
        public string? ModerationStatus { get; set; }

        /// <summary>
        /// Gets or sets the common (overall) status of the campaign:
        /// <c>MODERATION</c>, <c>DECLINED</c>, <c>READY_FOR_SEND</c>, <c>RUNNING</c>,
        /// <c>SENT</c>, <c>DONE</c>.
        /// </summary>
        public string? CommonStatus { get; set; }

        /// <summary>
        /// Gets or sets the list of contact groups included in the campaign.
        /// Empty when no groups were used.
        /// </summary>
        public IReadOnlyList<CampaignGroupInfo>? GroupsList { get; set; }

        /// <summary>
        /// Gets or sets the campaign type:
        /// <c>1</c> — Single, <c>2</c> — Bulk, <c>3</c> — Template, <c>7</c> — Functional.
        /// </summary>
        public int Type { get; set; }

        /// <summary>Gets or sets the message type. Currently only <c>SMS</c> is supported.</summary>
        public string? MsgType { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of messages sent per <see cref="RatePeriod"/> seconds.
        /// </summary>
        public int? RateLimit { get; set; }

        /// <summary>Gets or sets the rate-limit period in seconds.</summary>
        public int? RatePeriod { get; set; }

        /// <summary>Gets or sets the send status: <c>SENT</c> or <c>DONE</c>.</summary>
        public string? SendStatus { get; set; }

        /// <summary>
        /// Gets or sets the deletion flag:
        /// <c>0</c> — available; <c>1</c> — deleted.
        /// </summary>
        public int IsDeleted { get; set; }

        /// <summary>
        /// Gets or sets the deferred send date and time.
        /// Format: <c>YYYY-MM-DD HH:MM:SS</c>.
        /// </summary>
        [JsonPropertyName("deferredToTs")]
        public string? DeferredTo { get; set; }

        /// <summary>
        /// Gets or sets the campaign creation date and time.
        /// Format: <c>YYYY-MM-DD HH:MM:SS</c>.
        /// </summary>
        public string? CreateTs { get; set; }

        /// <summary>
        /// Gets or sets the date and time when sending actually started.
        /// Format: <c>YYYY-MM-DD HH:MM:SS</c>.
        /// </summary>
        public string? StartSendTs { get; set; }

        /// <summary>
        /// Gets or sets the date and time when all messages were sent.
        /// Format: <c>YYYY-MM-DD HH:MM:SS</c>.
        /// </summary>
        public string? EndSendTs { get; set; }

        /// <summary>Gets or sets the campaign name.</summary>
        public string? Name { get; set; }

        /// <summary>Gets or sets the sender name or number used in the campaign.</summary>
        public string? From { get; set; }

        /// <summary>Gets or sets the full message text or template text with placeholders.</summary>
        public string? Text { get; set; }

        /// <summary>
        /// Gets or sets the maximum message delivery wait time in minutes.
        /// </summary>
        public int? Validity { get; set; }

        /// <summary>
        /// Gets or sets the SMS message class (Flash or Normal).
        /// </summary>
        [JsonPropertyName("mclass")]
        public MessageClass? MessageClass { get; set; }

        /// <summary>
        /// Gets or sets whether recipient click-tracking on short links is enabled:
        /// <c>0</c> — disabled; <c>1</c> — enabled.
        /// </summary>
        public bool? TrackShortLinkRecipients { get; set; }

        /// <summary>
        /// Gets or sets the comma-separated contact group IDs used in the campaign.
        /// </summary>
        public string? Groups { get; set; }

        /// <summary>
        /// Gets or sets the moderator's comment if the campaign was declined.
        /// </summary>
        public string? GlobalComment { get; set; }
    }

    /// <summary>
    /// Represents a contact group entry within a campaign's <c>groupsList</c> field.
    /// </summary>
    public class CampaignGroupInfo
    {
        /// <summary>Gets or sets the group ID.</summary>
        public int Id { get; set; }

        /// <summary>Gets or sets the group name.</summary>
        public string? Name { get; set; }

        /// <summary>Gets or sets the number of contacts in the group available for sending.</summary>
        public int CardsCnt { get; set; }
    }
}

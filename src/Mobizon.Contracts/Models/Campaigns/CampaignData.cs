using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Mobizon.Contracts.Models.Messages;

namespace Mobizon.Contracts.Models.Campaigns
{
    /// <summary>
    /// Represents the full data of an SMS campaign as returned by the Mobizon API
    /// (<c>campaign/get</c> and <c>campaign/list</c>).
    /// </summary>
    public class CampaignData
    {
        /// <summary>Gets or sets the unique ID of the campaign.</summary>
        public int Id { get; set; }

        /// <summary>Gets or sets the moderation status of the campaign.</summary>
        public CampaignCommonStatus? ModerationStatus { get; set; }

        /// <summary>Gets or sets the common (overall) status of the campaign.</summary>
        public CampaignCommonStatus? CommonStatus { get; set; }

        /// <summary>
        /// Gets or sets the list of contact groups included in the campaign.
        /// Empty when no groups were used.
        /// </summary>
        public IReadOnlyList<CampaignGroupInfo>? GroupsList { get; set; }

        /// <summary>Gets or sets the campaign type (Single, Bulk, Template).</summary>
        public CampaignType? Type { get; set; }

        /// <summary>Gets or sets the message type (e.g. SMS).</summary>
        [JsonPropertyName("msgType")]
        public MessageType? MessageType { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of messages sent per <see cref="RatePeriod"/> seconds.
        /// </summary>
        public int? RateLimit { get; set; }

        /// <summary>Gets or sets the rate-limit period in seconds.</summary>
        public int? RatePeriod { get; set; }

        /// <summary>Gets or sets the send status of the campaign.</summary>
        public CampaignCommonStatus? SendStatus { get; set; }

        /// <summary>
        /// Gets or sets whether the campaign has been deleted.
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>Gets or sets the deferred send date and time.</summary>
        [JsonPropertyName("deferredToTs")]
        public DateTime? DeferredTo { get; set; }

        /// <summary>Gets or sets the campaign creation date and time.</summary>
        [JsonPropertyName("createTs")]
        public DateTime? Created { get; set; }

        /// <summary>Gets or sets the date and time when sending actually started.</summary>
        [JsonPropertyName("startSendTs")]
        public DateTime? SendStarted { get; set; }

        /// <summary>Gets or sets the date and time when all messages were sent.</summary>
        [JsonPropertyName("endSendTs")]
        public DateTime? SendEnded { get; set; }

        /// <summary>Gets or sets the campaign name.</summary>
        public string? Name { get; set; }

        /// <summary>Gets or sets the sender name or number used in the campaign.</summary>
        public string? From { get; set; }

        /// <summary>Gets or sets the full message text or template text with placeholders.</summary>
        public string? Text { get; set; }

        /// <summary>
        /// Gets or sets the maximum message delivery wait time.
        /// The API represents this value in minutes (e.g. <c>"1440"</c> = 24 hours).
        /// </summary>
        public TimeSpan? Validity { get; set; }

        /// <summary>Gets or sets the SMS message class (Flash or Normal).</summary>
        [JsonPropertyName("mclass")]
        public MessageClass? MessageClass { get; set; }

        /// <summary>
        /// Gets or sets whether recipient click-tracking on short links is enabled.
        /// </summary>
        public bool? TrackShortLinkRecipients { get; set; }

        /// <summary>Gets or sets the contact group IDs used in the campaign.</summary>
        public IReadOnlyList<int>? Groups { get; set; }

        /// <summary>Gets or sets the moderator's comment if the campaign was declined.</summary>
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

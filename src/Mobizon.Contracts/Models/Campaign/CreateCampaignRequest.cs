using System;
using Mobizon.Contracts.Models.Message;

namespace Mobizon.Contracts.Models.Campaign
{
    /// <summary>
    /// Represents the parameters required to create a new SMS campaign.
    /// </summary>
    public class CreateCampaignRequest
    {
        /// <summary>
        /// Gets or sets the campaign name (max 255 characters).
        /// Helps identify campaigns in the dashboard.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the campaign type.
        /// Defaults to <see cref="CampaignType.Bulk"/>.
        /// </summary>
        public CampaignType Type { get; set; } = CampaignType.Bulk;

        /// <summary>
        /// Gets or sets the sender name or number displayed to recipients.
        /// If not set, the account default or service signature will be used.
        /// </summary>
        public string? From { get; set; }

        /// <summary>
        /// Gets or sets the body text of the campaign message.
        /// For template campaigns (<see cref="CampaignType.Template"/>), the text may contain
        /// placeholders in curly braces, e.g. <c>{name}</c>.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the maximum number of messages to send within the period defined by <see cref="RatePeriod"/>.
        /// Used to throttle sending speed. Maximum effective rate is 100 messages per second.
        /// </summary>
        public int? RateLimit { get; set; }

        /// <summary>
        /// Gets or sets the time period in seconds for the <see cref="RateLimit"/> throttle.
        /// Allowed values: <c>60</c> (1 minute), <c>3600</c> (1 hour), <c>86400</c> (1 day).
        /// </summary>
        public int? RatePeriod { get; set; }

        /// <summary>
        /// Gets or sets the scheduled (deferred) send date and time.
        /// Must be at least 1 hour from now and no more than 14 days ahead.
        /// </summary>
        public DateTime? DeferredTo { get; set; }

        /// <summary>
        /// Gets or sets the SMS message class (Flash or Normal).
        /// When <see langword="null"/>, <see cref="MessageClass.Normal"/> is used by the platform.
        /// </summary>
        public MessageClass? MessageClass { get; set; }

        /// <summary>
        /// Gets or sets the maximum message delivery wait time.
        /// Accepted range: 1 hour to 24 hours. The value is rounded down to whole minutes.
        /// Applies when the recipient's phone is off or out of network range.
        /// </summary>
        public TimeSpan? Validity { get; set; }

        /// <summary>
        /// Gets or sets whether to track which recipients clicked a short link in the message.
        /// <c>0</c> — disabled (default); <c>1</c> — enabled.
        /// Requires short links created via the Mobizon service to be present in the text.
        /// </summary>
        public bool? TrackShortLinkRecipients { get; set; }
    }
}

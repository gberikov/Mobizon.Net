using System;
using System.Text.Json.Serialization;

namespace Mobizon.Contracts
{
    /// <summary>
    /// The <c>extra</c> object of a campaign response. The official documentation lists these settings as
    /// top-level fields, but captured responses nest them here, so <see cref="CampaignData"/> reads either
    /// placement: <see cref="CampaignData.Validity"/>, <see cref="CampaignData.MessageClass"/> and
    /// <see cref="CampaignData.TrackShortLinkRecipients"/> fall back to this object.
    /// </summary>
    public class CampaignExtra
    {
        /// <summary>
        /// Maximum message delivery wait time. The API represents it in minutes (e.g. <c>"1440"</c> = 24 hours).
        /// </summary>
        [JsonPropertyName("validity")]
        public TimeSpan? Validity { get; set; }

        /// <summary>SMS message class (Flash or Normal).</summary>
        [JsonPropertyName("mclass")]
        public MessageClass? MessageClass { get; set; }

        /// <summary>Whether recipient click-tracking on short links is enabled.</summary>
        [JsonPropertyName("trackShortLinkRecipients")]
        public bool? TrackShortLinkRecipients { get; set; }

        /// <summary>Whether the campaign was sent with the shared test sender ID.</summary>
        [JsonPropertyName("isTestAlphanameUsed")]
        public bool? IsTestAlphanameUsed { get; set; }

        /// <summary>Raw message coding code returned by the API; value domain not documented.</summary>
        [JsonPropertyName("coding")]
        public int? Coding { get; set; }

        /// <summary>Character set used for the message text (e.g. <c>UTF-8</c>).</summary>
        [JsonPropertyName("charset")]
        public string? Charset { get; set; }
    }
}

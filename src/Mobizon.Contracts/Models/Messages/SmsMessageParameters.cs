using System;

namespace Mobizon.Contracts.Models.Messages
{
    /// <summary>
    /// Optional additional parameters for sending an SMS message (<c>params[…]</c> fields).
    /// </summary>
    public class SmsMessageParameters
    {
        /// <summary>
        /// Gets or sets the campaign name (<c>params[name]</c>).
        /// When <see langword="null"/>, the default name is used.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the scheduled send date and time (<c>params[deferredToTs]</c>).
        /// Must be at least 1 hour and at most 14 days from now.
        /// When <see langword="null"/>, the message is sent immediately.
        /// </summary>
        public DateTime? DeferredTo { get; set; }

        /// <summary>
        /// Gets or sets the message class (<c>params[mclass]</c>).
        /// When <see langword="null"/>, <see cref="MessageClass.Normal"/> is used by the platform.
        /// </summary>
        public MessageClass? MessageClass { get; set; }

        /// <summary>
        /// Gets or sets the maximum delivery wait time (<c>params[validity]</c>).
        /// Accepted range: 1 hour to 24 hours. The value is rounded down to whole minutes.
        /// When <see langword="null"/>, the platform default is used.
        /// </summary>
        public TimeSpan? Validity { get; set; }
    }
}


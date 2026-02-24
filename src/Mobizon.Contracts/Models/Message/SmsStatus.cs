using System.Text.Json.Serialization;

namespace Mobizon.Contracts.Models.Message
{
    /// <summary>
    /// Represents the delivery status of an SMS message as returned by the Mobizon API.
    /// </summary>
    public enum SmsStatus
    {
        /// <summary>Message has been created and is waiting to be queued for sending.</summary>
        New,

        /// <summary>Message has been placed in the sending queue.</summary>
        Enqueued,

        /// <summary>Message has been accepted by the mobile operator.</summary>
        Accepted,

        /// <summary>Message has been successfully delivered to the recipient.</summary>
        Delivered,

        /// <summary>Message could not be delivered (undeliverable).</summary>
        Undelivered,

        /// <summary>Message was rejected by the operator or the system.</summary>
        Rejected,

        /// <summary>Message delivery period has expired.</summary>
        Expired,

        /// <summary>Message was deleted before delivery.</summary>
        Deleted
    }
}

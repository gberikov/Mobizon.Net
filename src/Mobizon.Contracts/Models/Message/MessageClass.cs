namespace Mobizon.Contracts.Models.Message
{
    /// <summary>
    /// Defines the class of an outgoing SMS message (<c>params[mclass]</c>).
    /// </summary>
    public enum MessageClass
    {
        /// <summary>
        /// Flash SMS — the message is displayed in a pop-up window on the handset and is never stored.
        /// </summary>
        Flash = 0,

        /// <summary>
        /// Normal SMS — the message is stored in the phone's inbox. This is the platform default.
        /// </summary>
        Normal = 1
    }
}


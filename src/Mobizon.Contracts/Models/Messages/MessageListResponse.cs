using System.Collections.Generic;

namespace Mobizon.Contracts.Models.Messages
{
    /// <summary>
    /// Represents the response data returned by the message list endpoint.
    /// </summary>
    public class MessageListResponse
    {
        /// <summary>
        /// Gets or sets the list of messages on the current page.
        /// </summary>
        public IReadOnlyList<MessageInfo> Items { get; set; } = new List<MessageInfo>();

        /// <summary>
        /// Gets or sets the total number of messages matching the filter criteria.
        /// </summary>
        public int TotalItemCount { get; set; }
    }
}

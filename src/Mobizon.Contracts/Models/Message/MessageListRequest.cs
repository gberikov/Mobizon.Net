namespace Mobizon.Contracts.Models.Message
{
    /// <summary>
    /// Specifies optional filter, pagination, and sort criteria for listing SMS messages.
    /// </summary>
    public class MessageListRequest
    {
        /// <summary>
        /// Gets or sets an optional sender name or number to filter messages by.
        /// When <see langword="null"/>, messages from all senders are returned.
        /// </summary>
        public string? From { get; set; }

        /// <summary>
        /// Gets or sets an optional delivery status code to filter messages by.
        /// When <see langword="null"/>, messages with any status are returned.
        /// </summary>
        public int? Status { get; set; }

        /// <summary>
        /// Gets or sets optional pagination settings. When <see langword="null"/>, the API default page size is used.
        /// </summary>
        public PaginationRequest? Pagination { get; set; }

        /// <summary>
        /// Gets or sets optional sort settings. When <see langword="null"/>, the API default sort order is used.
        /// </summary>
        public SortRequest? Sort { get; set; }
    }
}

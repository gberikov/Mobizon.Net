using Mobizon.Contracts;

namespace Mobizon.Contracts
{
    /// <summary>
    /// Specifies sorting parameters for list API requests.
    /// </summary>
    public class SortRequest
    {
        /// <summary>
        /// Gets or sets the name of the field to sort by.
        /// </summary>
        public string Field { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the sort direction. Defaults to <see cref="SortDirection.Ascending"/>.
        /// </summary>
        public SortDirection Direction { get; set; } = SortDirection.Ascending;
    }
}

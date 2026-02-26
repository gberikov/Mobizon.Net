using Mobizon.Contracts.Models.Common;

namespace Mobizon.Contracts.Models.Common
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
        /// Gets or sets the sort direction. Defaults to <see cref="SortDirection.ASC"/>.
        /// </summary>
        public SortDirection Direction { get; set; } = SortDirection.ASC;
    }
}

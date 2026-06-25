using System.Collections.Generic;

namespace Mobizon.Contracts.Models.ContactGroups
{
    /// <summary>
    /// Result returned by <c>contactgroup/delete</c>.
    /// </summary>
    public class DeleteContactGroupResult
    {
        /// <summary>Gets or sets the IDs of groups that were successfully deleted.</summary>
        public IReadOnlyList<long> Processed { get; set; } = new List<long>();

        /// <summary>Gets or sets the IDs of groups that could not be deleted.</summary>
        public IReadOnlyList<long> NotProcessed { get; set; } = new List<long>();
    }
}

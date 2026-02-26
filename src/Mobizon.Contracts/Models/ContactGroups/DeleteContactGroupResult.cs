using System.Collections.Generic;

namespace Mobizon.Contracts.Models.ContactGroups
{
    /// <summary>
    /// Result returned by <c>contactgroup/delete</c>.
    /// </summary>
    public class DeleteContactGroupResult
    {
        /// <summary>Gets or sets the IDs of groups that were successfully deleted.</summary>
        public IReadOnlyList<int> Processed { get; set; } = new List<int>();

        /// <summary>Gets or sets the IDs of groups that could not be deleted.</summary>
        public IReadOnlyList<int> NotProcessed { get; set; } = new List<int>();
    }
}

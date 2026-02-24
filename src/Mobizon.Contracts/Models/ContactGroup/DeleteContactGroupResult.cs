using System.Collections.Generic;

namespace Mobizon.Contracts.Models.ContactGroup
{
    /// <summary>
    /// Result returned by <c>contactgroup/delete</c>.
    /// </summary>
    public class DeleteContactGroupResult
    {
        /// <summary>Gets or sets the IDs of groups that were successfully deleted.</summary>
        public IReadOnlyList<string> Processed { get; set; } = new List<string>();

        /// <summary>Gets or sets the IDs of groups that could not be deleted.</summary>
        public IReadOnlyList<string> NotProcessed { get; set; } = new List<string>();
    }
}

using System;
using System.Collections.Generic;

namespace Mobizon.Contracts
{
    /// <summary>
    /// Outcome of a delete call: the IDs the API removed and the IDs it refused
    /// (not found, not owned, or in a state that forbids deletion).
    /// </summary>
    public class DeleteResult
    {
        /// <summary>IDs that were deleted.</summary>
        public IReadOnlyList<long> Processed { get; set; } = Array.Empty<long>();

        /// <summary>IDs that were not deleted.</summary>
        public IReadOnlyList<long> NotProcessed { get; set; } = Array.Empty<long>();

        /// <summary><see langword="true"/> when every requested ID was deleted.</summary>
        public bool AllProcessed => NotProcessed.Count == 0;
    }
}

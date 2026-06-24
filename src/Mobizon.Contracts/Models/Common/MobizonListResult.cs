using System;
using System.Collections.Generic;

namespace Mobizon.Contracts.Models.Common
{
    /// <summary>
    /// Generic envelope for Mobizon list endpoints: <c>{ items, totalItemCount }</c>.
    /// </summary>
    /// <typeparam name="T">The type of each item in the page.</typeparam>
    public class MobizonListResult<T>
    {
        /// <summary>The items on the current page.</summary>
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

        /// <summary>Total number of items matching the query (API may return this as a string).</summary>
        public int TotalItemCount { get; set; }
    }
}

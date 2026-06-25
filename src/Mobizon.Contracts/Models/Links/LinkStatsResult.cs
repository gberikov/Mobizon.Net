using System;
using System.Collections.Generic;

namespace Mobizon.Contracts.Models.Links
{
    /// <summary>
    /// Click/redirect statistics for one or more short links.
    /// <para>
    /// The API returns a period-major grid (one row per time bucket, with a <c>clicks{i}</c>/<c>redirects{i}</c>
    /// column pair per requested link, where <c>i</c> is the position of the link in the requested
    /// <see cref="GetLinkStatsRequest.Ids"/>). The SDK transposes that grid into one
    /// <see cref="LinkStatSeries"/> per requested link.
    /// </para>
    /// </summary>
    public class LinkStatsResult
    {
        /// <summary>One series per requested link, in the order the IDs were supplied.</summary>
        public IReadOnlyList<LinkStatSeries> Links { get; set; } = Array.Empty<LinkStatSeries>();
    }
}

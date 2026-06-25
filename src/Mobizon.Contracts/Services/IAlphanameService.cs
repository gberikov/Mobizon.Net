using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts.Models.Alphanames;
using Mobizon.Contracts.Models.Common;

namespace Mobizon.Contracts.Services
{
    /// <summary>Operations for listing the account's registered sender IDs (alphanames).</summary>
    public interface IAlphanameService
    {
        /// <summary>Returns the registered sender IDs (alphanumeric signatures) available to the account.</summary>
        Task<MobizonResponse<MobizonListResult<AlphanameData>>> ListAsync(
            PaginationRequest? pagination = null, CancellationToken cancellationToken = default);
    }
}

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
        /// <param name="pagination">Optional pagination parameters. Pass <see langword="null"/> to use API defaults.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>
        /// A paged envelope of <see cref="AlphanameData"/> items.
        /// </returns>
        /// <exception cref="Exceptions.MobizonApiException">
        /// Thrown when the API returns a non-success response code.
        /// </exception>
        Task<MobizonListResult<AlphanameData>> ListAsync(
            PaginationRequest? pagination = null, CancellationToken cancellationToken = default);
    }
}

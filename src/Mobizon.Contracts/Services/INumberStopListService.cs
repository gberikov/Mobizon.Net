using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts.Models;
using Mobizon.Contracts.Models.StopList;

namespace Mobizon.Contracts.Services
{
    /// <summary>
    /// Provides operations for managing the user's number stop-list via the Mobizon API.
    /// </summary>
    public interface INumberStopListService
    {
        /// <summary>
        /// Returns a paginated list of stop-list entries.
        /// </summary>
        Task<MobizonResponse<StopListListResponse>> ListAsync(
            StopListListRequest? request = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a single phone number to the stop-list and returns the new record ID.
        /// </summary>
        /// <param name="number">Phone number in international format (e.g. <c>77007782006</c>).</param>
        /// <param name="comment">Optional comment describing why the number is blocked.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<MobizonResponse<string>> AddNumberAsync(
            string number,
            string? comment = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a range of consecutive phone numbers to the stop-list.
        /// </summary>
        /// <param name="numberFrom">First number of the range in international format.</param>
        /// <param name="numberTo">Last number of the range in international format.</param>
        /// <param name="comment">Optional comment describing why the range is blocked.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<MobizonResponse<bool>> AddRangeAsync(
            string numberFrom,
            string numberTo,
            string? comment = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a stop-list entry by its record ID.
        /// </summary>
        Task<MobizonResponse<bool>> DeleteAsync(
            string id,
            CancellationToken cancellationToken = default);
    }
}

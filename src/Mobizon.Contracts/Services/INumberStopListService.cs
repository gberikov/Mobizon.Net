using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts.Models.StopLists;

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
        /// <returns>The unwrapped <see cref="StopListListResponse"/> containing items and total count.</returns>
        Task<StopListListResponse> ListAsync(
            StopListListRequest? request = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a single phone number to the stop-list and returns the new record ID.
        /// </summary>
        /// <param name="number">Phone number in international format (e.g. <c>77007782006</c>).</param>
        /// <param name="comment">Optional comment describing why the number is blocked.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The ID of the newly created stop-list record.</returns>
        Task<long> AddNumberAsync(
            string number,
            string? comment = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a range of consecutive phone numbers to the stop-list.
        /// Throws <see cref="Mobizon.Contracts.Exceptions.MobizonApiException"/> on API error.
        /// </summary>
        /// <param name="numberFrom">First number of the range in international format.</param>
        /// <param name="numberTo">Last number of the range in international format.</param>
        /// <param name="comment">Optional comment describing why the range is blocked.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task AddNumberRangeAsync(
            string numberFrom,
            string numberTo,
            string? comment = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a stop-list entry by its record ID.
        /// Throws <see cref="Mobizon.Contracts.Exceptions.MobizonApiException"/> on API error.
        /// </summary>
        Task DeleteAsync(
            long id,
            CancellationToken cancellationToken = default);
    }
}

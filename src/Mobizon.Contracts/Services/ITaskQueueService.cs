using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts.Models.Common;
using Mobizon.Contracts.Models.TaskQueues;

namespace Mobizon.Contracts.Services
{
    /// <summary>
    /// Provides operations for querying the status of background tasks on the Mobizon platform.
    /// </summary>
    public interface ITaskQueueService
    {
        /// <summary>
        /// Retrieves the current status and progress of a background task by its ID.
        /// </summary>
        /// <param name="id">The ID of the background task to query.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>
        /// A <see cref="MobizonResponse{T}"/> containing a <see cref="TaskQueueStatus"/> with
        /// the task status code and completion progress percentage.
        /// </returns>
        /// <exception cref="Exceptions.MobizonApiException">
        /// Thrown when the API returns a non-success response code.
        /// </exception>
        Task<MobizonResponse<TaskQueueStatus>> GetStatusAsync(
            int id, CancellationToken cancellationToken = default);
    }
}

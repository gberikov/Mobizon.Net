// Contract definition — target: src/Mobizon.Contracts/Services/ITaskQueueService.cs
using Mobizon.Contracts.Models;
using Mobizon.Contracts.Models.TaskQueue;

namespace Mobizon.Contracts.Services;

public interface ITaskQueueService
{
    Task<MobizonResponse<TaskQueueStatus>> GetStatusAsync(
        int id,
        CancellationToken cancellationToken = default);
}

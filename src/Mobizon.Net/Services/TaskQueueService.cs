using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts.Models;
using Mobizon.Contracts.Models.TaskQueue;
using Mobizon.Contracts.Services;
using Mobizon.Net.Internal;

namespace Mobizon.Net.Services
{
    internal class TaskQueueService : ITaskQueueService
    {
        private const string ModuleName = "taskqueue";
        private readonly MobizonApiClient _apiClient;

        public TaskQueueService(MobizonApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public Task<MobizonResponse<TaskQueueStatus>> GetStatusAsync(
            int id, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["id"] = id.ToString()
            };

            return _apiClient.SendAsync<TaskQueueStatus>(
                HttpMethod.Post, ModuleName, "getstatus", parameters, cancellationToken);
        }
    }
}

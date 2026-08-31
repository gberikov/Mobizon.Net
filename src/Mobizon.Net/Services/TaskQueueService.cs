using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts;
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

        public async Task<TaskQueueStatus> GetStatusAsync(
            long id, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["id"] = ApiFormat.Int(id)
            };

            return (await _apiClient.SendAsync<TaskQueueStatus>(ModuleName, "getstatus", parameters, cancellationToken).ConfigureAwait(false)).Data!;
        }
    }
}

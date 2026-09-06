using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts;
using Mobizon.Net.Internal;

namespace Mobizon.Net.Services
{
    internal class TaskQueueService(MobizonApiClient apiClient) : ITaskQueueService
    {
        private const string ModuleName = "taskqueue";

        public async Task<TaskQueueStatus> GetStatusAsync(
            long id, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["id"] = ApiFormat.Int(id)
            };

            return (await apiClient.SendAsync<TaskQueueStatus>(ModuleName, "getstatus", parameters, cancellationToken).ConfigureAwait(false)).Data!;
        }
    }
}

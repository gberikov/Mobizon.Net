using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts;
using Mobizon.Net.Internal;

namespace Mobizon.Net.Services
{
    internal class AlphanameService(MobizonApiClient apiClient) : IAlphanameService
    {
        private const string ModuleName = "alphaname";

        public async Task<MobizonListResult<AlphanameData>> ListAsync(
            PaginationRequest? pagination = null, CancellationToken cancellationToken = default)
        {
            Dictionary<string, string>? parameters = null;
            if (pagination != null)
                parameters = new Dictionary<string, string>
                {
                    ["pagination[currentPage]"] = ApiFormat.Int(pagination.CurrentPage),
                    ["pagination[pageSize]"] = ApiFormat.Int(pagination.PageSize)
                };
            return (await apiClient.SendAsync<MobizonListResult<AlphanameData>>(
                ModuleName, "list", parameters, cancellationToken).ConfigureAwait(false)).Data!;
        }
    }
}

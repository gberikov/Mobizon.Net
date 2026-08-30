using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts.Models.Alphanames;
using Mobizon.Contracts.Models.Common;
using Mobizon.Contracts.Services;
using Mobizon.Net.Internal;

namespace Mobizon.Net.Services
{
    internal class AlphanameService : IAlphanameService
    {
        private const string ModuleName = "alphaname";
        private readonly MobizonApiClient _apiClient;
        public AlphanameService(MobizonApiClient apiClient) => _apiClient = apiClient;

        public async Task<MobizonListResult<AlphanameData>> ListAsync(
            PaginationRequest? pagination = null, CancellationToken cancellationToken = default)
        {
            Dictionary<string, string>? parameters = null;
            if (pagination != null)
                parameters = new Dictionary<string, string>
                {
                    ["pagination[currentPage]"] = pagination.CurrentPage.ToString(),
                    ["pagination[pageSize]"] = pagination.PageSize.ToString()
                };
            return (await _apiClient.SendAsync<MobizonListResult<AlphanameData>>(
                ModuleName, "list", parameters, cancellationToken).ConfigureAwait(false)).Data!;
        }
    }
}

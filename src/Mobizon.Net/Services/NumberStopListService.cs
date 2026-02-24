using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts.Models;
using Mobizon.Contracts.Models.StopList;
using Mobizon.Contracts.Services;
using Mobizon.Net.Internal;

namespace Mobizon.Net.Services
{
    internal class NumberStopListService : INumberStopListService
    {
        private const string ModuleName = "numberstoplist";
        private readonly MobizonApiClient _apiClient;

        public NumberStopListService(MobizonApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public Task<MobizonResponse<StopListListResponse>> ListAsync(
            StopListListRequest? request = null,
            CancellationToken cancellationToken = default)
        {
            Dictionary<string, string>? parameters = null;

            if (request != null)
            {
                parameters = new Dictionary<string, string>();

                if (request.Pagination != null)
                {
                    parameters["pagination[currentPage]"] = request.Pagination.CurrentPage.ToString();
                    parameters["pagination[pageSize]"] = request.Pagination.PageSize.ToString();
                }

                if (request.Sort != null)
                    parameters[$"sort[{request.Sort.Field}]"] = request.Sort.Direction.ToString();
            }

            return _apiClient.SendAsync<StopListListResponse>(
                HttpMethod.Post, ModuleName, "list", parameters, cancellationToken);
        }

        public Task<MobizonResponse<string>> AddNumberAsync(
            string number,
            string? comment = null,
            CancellationToken cancellationToken = default)
        {
            return _apiClient.SendJsonAsync<string>(
                ModuleName, "create",
                new { id = "", number, comment = comment ?? string.Empty },
                cancellationToken);
        }

        public Task<MobizonResponse<bool>> AddRangeAsync(
            string numberFrom,
            string numberTo,
            string? comment = null,
            CancellationToken cancellationToken = default)
        {
            return _apiClient.SendJsonAsync<bool>(
                ModuleName, "create",
                new { id = "", numberFrom, numberTo, comment = comment ?? string.Empty },
                cancellationToken);
        }

        public Task<MobizonResponse<bool>> DeleteAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            return _apiClient.SendJsonAsync<bool>(
                ModuleName, "delete",
                new { id },
                cancellationToken);
        }
    }
}

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts.Models.StopLists;
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

        public async Task<StopListListResponse> ListAsync(
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

            return (await _apiClient.SendAsync<StopListListResponse>(
                HttpMethod.Post, ModuleName, "list", parameters, cancellationToken).ConfigureAwait(false)).Data!;
        }

        public async Task<long> AddNumberAsync(
            string number,
            string? comment = null,
            CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["id"]      = string.Empty,
                ["number"]  = number,
                ["comment"] = comment ?? string.Empty
            };

            return (await _apiClient.SendAsync<long>(
                HttpMethod.Post, ModuleName, "create", parameters, cancellationToken).ConfigureAwait(false)).Data;
        }

        public async Task AddNumberRangeAsync(
            string numberFrom,
            string numberTo,
            string? comment = null,
            CancellationToken cancellationToken = default)
        {
            // The API requires numberFrom <= numberTo. Swap the values if the caller
            // provided them in reverse order so that the request passes validation.
            if (ulong.TryParse(numberFrom, out var from) &&
                ulong.TryParse(numberTo,   out var to) &&
                from > to)
                (numberFrom, numberTo) = (numberTo, numberFrom);

            var parameters = new Dictionary<string, string>
            {
                ["id"]         = string.Empty,
                ["numberFrom"] = numberFrom,
                ["numberTo"]   = numberTo,
                ["comment"]    = comment ?? string.Empty
            };

            await _apiClient.SendAsync<bool>(
                HttpMethod.Post, ModuleName, "create", parameters, cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteAsync(
            long id,
            CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["id"] = id.ToString()
            };

            await _apiClient.SendAsync<bool>(
                HttpMethod.Post, ModuleName, "delete", parameters, cancellationToken).ConfigureAwait(false);
        }
    }
}

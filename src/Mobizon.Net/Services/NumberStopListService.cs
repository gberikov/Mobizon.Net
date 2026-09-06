using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts;
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
                    parameters["pagination[currentPage]"] = ApiFormat.Int(request.Pagination.CurrentPage);
                    parameters["pagination[pageSize]"] = ApiFormat.Int(request.Pagination.PageSize);
                }

                if (request.Sort != null)
                    parameters[$"sort[{request.Sort.Field}]"] = ApiFormat.Sort(request.Sort.Direction);
            }

            return (await _apiClient.SendAsync<StopListListResponse>(
                ModuleName, "list", parameters, cancellationToken).ConfigureAwait(false)).Data!;
        }

        public async Task<long> AddNumberAsync(
            string number,
            string? comment = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(number))
                throw new ArgumentException("Number is required.", nameof(number));

            var parameters = new Dictionary<string, string>
            {
                ["id"]      = string.Empty,
                ["number"]  = number,
                ["comment"] = comment ?? string.Empty
            };

            return await _apiClient.SendForIdAsync(
                ModuleName, "create", parameters, cancellationToken).ConfigureAwait(false);
        }

        public async Task AddNumberRangeAsync(
            string numberFrom,
            string numberTo,
            string? comment = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(numberFrom))
                throw new ArgumentException("Range start is required.", nameof(numberFrom));
            if (string.IsNullOrWhiteSpace(numberTo))
                throw new ArgumentException("Range end is required.", nameof(numberTo));

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

            await _apiClient.SendCommandAsync(
                ModuleName, "create", parameters, cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteAsync(
            long id,
            CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["id"] = ApiFormat.Int(id)
            };

            await _apiClient.SendCommandAsync(
                ModuleName, "delete", parameters, cancellationToken).ConfigureAwait(false);
        }
    }
}

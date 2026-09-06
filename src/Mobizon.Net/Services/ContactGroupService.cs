using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts;
using Mobizon.Net.Internal;

namespace Mobizon.Net.Services
{
    internal class ContactGroupService : IContactGroupService
    {
        private const string ModuleName = "contactgroup";
        private readonly MobizonApiClient _apiClient;

        public ContactGroupService(MobizonApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<ContactGroupListResponse> ListAsync(
            ContactGroupListRequest? request = null,
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

            return (await _apiClient.SendAsync<ContactGroupListResponse>(
                ModuleName, "list", parameters, cancellationToken).ConfigureAwait(false)).Data!;
        }

        public async Task<long> CreateAsync(
            string name,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Group name is required.", nameof(name));

            var parameters = new Dictionary<string, string>
            {
                ["data[name]"] = name
            };

            return await _apiClient.SendForIdAsync(
                ModuleName, "create", parameters, cancellationToken).ConfigureAwait(false);
        }

        public async Task UpdateAsync(
            long id,
            string name,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Group name is required.", nameof(name));

            var parameters = new Dictionary<string, string>
            {
                ["id"]          = ApiFormat.Int(id),
                ["data[name]"]  = name
            };

            await _apiClient.SendCommandAsync(
                ModuleName, "update", parameters, cancellationToken).ConfigureAwait(false);
        }

        public async Task<DeleteResult> DeleteAsync(
            long id,
            CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["id"] = ApiFormat.Int(id)
            };

            return (await _apiClient.SendAsync<DeleteResult>(
                ModuleName, "delete", parameters, cancellationToken).ConfigureAwait(false)).Data!;
        }

        public async Task<long> GetCardsCountAsync(
            long? id = null,
            CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["id"] = id.HasValue ? ApiFormat.Int(id.Value) : "-1"
            };

            return (await _apiClient.SendAsync<long>(
                ModuleName, "getcardscount", parameters, cancellationToken).ConfigureAwait(false)).Data;
        }
    }
}

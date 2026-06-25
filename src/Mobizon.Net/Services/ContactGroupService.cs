using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts.Models.ContactGroups;
using Mobizon.Contracts.Services;
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
                    parameters["pagination[currentPage]"] = request.Pagination.CurrentPage.ToString();
                    parameters["pagination[pageSize]"] = request.Pagination.PageSize.ToString();
                }

                if (request.Sort != null)
                    parameters[$"sort[{request.Sort.Field}]"] = request.Sort.Direction.ToString();
            }

            return (await _apiClient.SendAsync<ContactGroupListResponse>(
                HttpMethod.Post, ModuleName, "list", parameters, cancellationToken).ConfigureAwait(false)).Data!;
        }

        public async Task<long> CreateAsync(
            string name,
            CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["data[name]"] = name
            };

            return (await _apiClient.SendAsync<long>(
                HttpMethod.Post, ModuleName, "create", parameters, cancellationToken).ConfigureAwait(false)).Data;
        }

        public async Task UpdateAsync(
            long id,
            string name,
            CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["id"]          = id.ToString(),
                ["data[name]"]  = name
            };

            await _apiClient.SendAsync<bool>(
                HttpMethod.Post, ModuleName, "update", parameters, cancellationToken).ConfigureAwait(false);
        }

        public async Task<DeleteContactGroupResult> DeleteAsync(
            long id,
            CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["id"] = id.ToString()
            };

            return (await _apiClient.SendAsync<DeleteContactGroupResult>(
                HttpMethod.Post, ModuleName, "delete", parameters, cancellationToken).ConfigureAwait(false)).Data!;
        }

        public async Task<long> GetCardsCountAsync(
            long? id = null,
            CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["id"] = id.HasValue ? id.Value.ToString() : "-1"
            };

            return (await _apiClient.SendAsync<long>(
                HttpMethod.Post, ModuleName, "getcardscount", parameters, cancellationToken).ConfigureAwait(false)).Data;
        }
    }
}

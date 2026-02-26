using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts.Models.Common;
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

        public Task<MobizonResponse<ContactGroupListResponse>> ListAsync(
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

            return _apiClient.SendAsync<ContactGroupListResponse>(
                HttpMethod.Post, ModuleName, "list", parameters, cancellationToken);
        }

        public Task<MobizonResponse<int>> CreateAsync(
            string name,
            CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["data[name]"] = name
            };

            return _apiClient.SendAsync<int>(
                HttpMethod.Post, ModuleName, "create", parameters, cancellationToken);
        }

        public Task<MobizonResponse<bool>> UpdateAsync(
            int id,
            string name,
            CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["id"]          = id.ToString(),
                ["data[name]"]  = name
            };

            return _apiClient.SendAsync<bool>(
                HttpMethod.Post, ModuleName, "update", parameters, cancellationToken);
        }

        public Task<MobizonResponse<DeleteContactGroupResult>> DeleteAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["id"] = id.ToString()
            };

            return _apiClient.SendAsync<DeleteContactGroupResult>(
                HttpMethod.Post, ModuleName, "delete", parameters, cancellationToken);
        }

        public Task<MobizonResponse<int>> GetCardsCountAsync(
            int? id = null,
            CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["id"] = id.HasValue ? id.Value.ToString() : "-1"
            };

            return _apiClient.SendAsync<int>(
                HttpMethod.Post, ModuleName, "getcardscount", parameters, cancellationToken);
        }
    }
}

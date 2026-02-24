using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts.Models;
using Mobizon.Contracts.Models.ContactGroup;
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

        public Task<MobizonResponse<string>> CreateAsync(
            string name,
            CancellationToken cancellationToken = default)
        {
            return _apiClient.SendJsonAsync<string>(
                ModuleName, "create",
                new { data = new { name } },
                cancellationToken);
        }

        public Task<MobizonResponse<bool>> UpdateAsync(
            string id,
            string name,
            CancellationToken cancellationToken = default)
        {
            return _apiClient.SendJsonAsync<bool>(
                ModuleName, "update",
                new { id, data = new { name } },
                cancellationToken);
        }

        public Task<MobizonResponse<DeleteContactGroupResult>> DeleteAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            return _apiClient.SendJsonAsync<DeleteContactGroupResult>(
                ModuleName, "delete",
                new { id },
                cancellationToken);
        }

        public Task<MobizonResponse<int>> GetCardsCountAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            return _apiClient.SendJsonAsync<int>(
                ModuleName, "getcardscount",
                new { id },
                cancellationToken);
        }
    }
}

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts.Models;
using Mobizon.Contracts.Models.Link;
using Mobizon.Contracts.Services;
using Mobizon.Net.Internal;

namespace Mobizon.Net.Services
{
    internal class LinkService : ILinkService
    {
        private readonly MobizonApiClient _apiClient;

        public LinkService(MobizonApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public Task<MobizonResponse<LinkData>> CreateAsync(
            CreateLinkRequest request, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["data[fullLink]"] = request.FullLink
            };

            if (request.Status.HasValue)
                parameters["data[status]"] = request.Status.Value.ToString();

            if (request.ExpirationDate != null)
                parameters["data[expirationDate]"] = request.ExpirationDate;

            if (request.Comment != null)
                parameters["data[comment]"] = request.Comment;

            return _apiClient.SendAsync<LinkData>(
                HttpMethod.Post, "link", "create", parameters, cancellationToken);
        }

        public Task<MobizonResponse<object>> DeleteAsync(
            int[] ids, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>();
            for (var i = 0; i < ids.Length; i++)
            {
                parameters[$"ids[{i}]"] = ids[i].ToString();
            }

            return _apiClient.SendAsync<object>(
                HttpMethod.Post, "link", "delete", parameters, cancellationToken);
        }

        public Task<MobizonResponse<LinkData>> GetAsync(
            string code, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["code"] = code
            };

            return _apiClient.SendAsync<LinkData>(
                HttpMethod.Post, "link", "get", parameters, cancellationToken);
        }

        public Task<MobizonResponse<IReadOnlyList<LinkData>>> GetLinksAsync(
            int campaignId, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["campaignId"] = campaignId.ToString()
            };

            return _apiClient.SendAsync<IReadOnlyList<LinkData>>(
                HttpMethod.Post, "link", "getlinks", parameters, cancellationToken);
        }

        public Task<MobizonResponse<IReadOnlyList<LinkStatsResult>>> GetStatsAsync(
            GetLinkStatsRequest request, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>();

            for (var i = 0; i < request.Ids.Length; i++)
            {
                parameters[$"ids[{i}]"] = request.Ids[i].ToString();
            }

            parameters["type"] = request.Type.ToString().ToLowerInvariant();

            if (request.DateFrom != null)
                parameters["criteria[dateFrom]"] = request.DateFrom;

            if (request.DateTo != null)
                parameters["criteria[dateTo]"] = request.DateTo;

            return _apiClient.SendAsync<IReadOnlyList<LinkStatsResult>>(
                HttpMethod.Post, "link", "getstats", parameters, cancellationToken);
        }

        public Task<MobizonResponse<IReadOnlyList<LinkData>>> ListAsync(
            LinkListRequest? request = null, CancellationToken cancellationToken = default)
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
                {
                    parameters[$"sort[{request.Sort.Field}]"] = request.Sort.Direction.ToString();
                }
            }

            return _apiClient.SendAsync<IReadOnlyList<LinkData>>(
                HttpMethod.Post, "link", "list", parameters, cancellationToken);
        }

        public Task<MobizonResponse<object>> UpdateAsync(
            UpdateLinkRequest request, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["code"] = request.Code
            };

            if (request.FullLink != null)
                parameters["data[fullLink]"] = request.FullLink;

            if (request.Status.HasValue)
                parameters["data[status]"] = request.Status.Value.ToString();

            if (request.ExpirationDate != null)
                parameters["data[expirationDate]"] = request.ExpirationDate;

            if (request.Comment != null)
                parameters["data[comment]"] = request.Comment;

            return _apiClient.SendAsync<object>(
                HttpMethod.Post, "link", "update", parameters, cancellationToken);
        }
    }
}

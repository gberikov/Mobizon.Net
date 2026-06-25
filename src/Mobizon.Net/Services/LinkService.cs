using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts.Models.Common;
using Mobizon.Contracts.Models.Links;
using Mobizon.Contracts.Services;
using Mobizon.Net.Internal;

namespace Mobizon.Net.Services
{
    internal class LinkService : ILinkService
    {
        private const string ModuleName = "link";
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
                HttpMethod.Post, ModuleName, "create", parameters, cancellationToken);
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
                HttpMethod.Post, ModuleName, "delete", parameters, cancellationToken);
        }

        public Task<MobizonResponse<LinkData>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => _apiClient.SendAsync<LinkData>(HttpMethod.Post, ModuleName, "get",
                new Dictionary<string, string> { ["id"] = id.ToString() }, cancellationToken);

        public Task<MobizonResponse<LinkData>> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
            => _apiClient.SendAsync<LinkData>(HttpMethod.Post, ModuleName, "get",
                new Dictionary<string, string> { ["code"] = code }, cancellationToken);

        public Task<MobizonResponse<LinkData>> GetByShortLinkAsync(string shortLink, CancellationToken cancellationToken = default)
            => _apiClient.SendAsync<LinkData>(HttpMethod.Post, ModuleName, "get",
                new Dictionary<string, string> { ["shortLink"] = shortLink }, cancellationToken);

        public Task<MobizonResponse<IReadOnlyList<LinkData>>> GetLinksAsync(
            int campaignId, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["campaignId"] = campaignId.ToString()
            };

            return _apiClient.SendAsync<IReadOnlyList<LinkData>>(
                HttpMethod.Post, ModuleName, "getlinks", parameters, cancellationToken);
        }

        public Task<MobizonResponse<LinkStatsResult>> GetStatsAsync(
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

            return _apiClient.SendAsync<LinkStatsResult>(
                HttpMethod.Post, ModuleName, "getstats", parameters, cancellationToken);
        }

        public Task<MobizonResponse<MobizonListResult<LinkData>>> ListAsync(
            LinkListRequest? request = null, CancellationToken cancellationToken = default)
        {
            Dictionary<string, string>? parameters = null;

            if (request != null)
            {
                parameters = new Dictionary<string, string>();

                if (request.Criteria != null)
                {
                    var c = request.Criteria;
                    if (c.Status.HasValue) parameters["criteria[status]"] = c.Status.Value.ToString();
                    if (c.ModeratorStatus.HasValue) parameters["criteria[moderatorStatus]"] = c.ModeratorStatus.Value.ToString();
                    if (c.Code != null) parameters["criteria[code]"] = c.Code;
                    if (c.FullLink != null) parameters["criteria[fullLink]"] = c.FullLink;
                    if (c.Comment != null) parameters["criteria[comment]"] = c.Comment;
                    if (c.CreateTsFrom != null) parameters["criteria[createTsFrom]"] = c.CreateTsFrom;
                    if (c.CreateTsTo != null) parameters["criteria[createTsTo]"] = c.CreateTsTo;
                    if (c.ClickCntFrom.HasValue) parameters["criteria[clickCntFrom]"] = c.ClickCntFrom.Value.ToString();
                    if (c.ClickCntTo.HasValue) parameters["criteria[clickCntTo]"] = c.ClickCntTo.Value.ToString();
                }

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

            return _apiClient.SendAsync<MobizonListResult<LinkData>>(
                HttpMethod.Post, ModuleName, "list", parameters, cancellationToken);
        }

        public Task<MobizonResponse<object>> UpdateAsync(UpdateLinkRequest request, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string> { ["id"] = request.Id.ToString() };
            if (request.Status.HasValue) parameters["data[status]"] = request.Status.Value.ToString();
            if (request.ExpirationDate != null) parameters["data[expirationDate]"] = request.ExpirationDate;
            if (request.Comment != null) parameters["data[comment]"] = request.Comment;
            return _apiClient.SendAsync<object>(HttpMethod.Post, ModuleName, "update", parameters, cancellationToken);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts;
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

        public async Task<LinkData> CreateAsync(
            CreateLinkRequest request, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["data[fullLink]"] = request.FullLink
            };

            if (request.Status.HasValue)
                parameters["data[status]"] = ((int)request.Status.Value).ToString(CultureInfo.InvariantCulture);

            if (request.ExpirationDate.HasValue)
                parameters["data[expirationDate]"] = ApiFormat.Date(request.ExpirationDate.Value);

            if (request.Comment != null)
                parameters["data[comment]"] = request.Comment;

            return (await _apiClient.SendAsync<LinkData>(
                ModuleName, "create", parameters, cancellationToken).ConfigureAwait(false)).Data;
        }

        public async Task<DeleteResult> DeleteAsync(
            long[] ids, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>();
            for (var i = 0; i < ids.Length; i++)
            {
                parameters[$"ids[{i}]"] = ApiFormat.Int(ids[i]);
            }

            return (await _apiClient.SendAsync<DeleteResult>(
                ModuleName, "delete", parameters, cancellationToken).ConfigureAwait(false)).Data;
        }

        public async Task<LinkData> GetByIdAsync(long id, CancellationToken cancellationToken = default)
            => (await _apiClient.SendAsync<LinkData>(ModuleName, "get",
                new Dictionary<string, string> { ["id"] = ApiFormat.Int(id) }, cancellationToken).ConfigureAwait(false)).Data;

        public async Task<LinkData> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
            => (await _apiClient.SendAsync<LinkData>(ModuleName, "get",
                new Dictionary<string, string> { ["code"] = code }, cancellationToken).ConfigureAwait(false)).Data;

        public async Task<LinkData> GetByShortLinkAsync(string shortLink, CancellationToken cancellationToken = default)
            => (await _apiClient.SendAsync<LinkData>(ModuleName, "get",
                new Dictionary<string, string> { ["shortLink"] = shortLink }, cancellationToken).ConfigureAwait(false)).Data;

        public async Task<IReadOnlyList<LinkData>> GetLinksAsync(
            long campaignId, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["campaignId"] = ApiFormat.Int(campaignId)
            };

            return (await _apiClient.SendAsync<IReadOnlyList<LinkData>>(
                ModuleName, "getlinks", parameters, cancellationToken).ConfigureAwait(false)).Data;
        }

        public async Task<LinkStatsResult> GetStatsAsync(
            GetLinkStatsRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Ids == null || request.Ids.Length == 0 || request.Ids.Length > 5)
                throw new ArgumentException("link/getStats accepts 1 to 5 link ids per request.", nameof(request));

            var parameters = new Dictionary<string, string>();

            for (var i = 0; i < request.Ids.Length; i++)
            {
                parameters[$"ids[{i}]"] = ApiFormat.Int(request.Ids[i]);
            }

            parameters["type"] = request.Type.ToString().ToLowerInvariant();

            if (request.DateFrom.HasValue)
                parameters["criteria[dateFrom]"] = ApiFormat.DateTime(request.DateFrom.Value);

            if (request.DateTo.HasValue)
                parameters["criteria[dateTo]"] = ApiFormat.DateTime(request.DateTo.Value);

            var response = await _apiClient.SendAsync<LinkStatsResult>(
                ModuleName, "getstats", parameters, cancellationToken).ConfigureAwait(false);

            // The payload identifies links only by their position in the requested `ids` array;
            // resolve each series back to its actual link ID here.
            if (response.Data?.Links != null)
            {
                foreach (var series in response.Data.Links)
                {
                    if (series.Index >= 0 && series.Index < request.Ids.Length)
                        series.LinkId = request.Ids[series.Index];
                }
            }

            return response.Data!;
        }

        public async Task<MobizonListResult<LinkData>> ListAsync(
            LinkListRequest? request = null, CancellationToken cancellationToken = default)
        {
            Dictionary<string, string>? parameters = null;

            if (request != null)
            {
                parameters = new Dictionary<string, string>();

                if (request.Criteria != null)
                {
                    var c = request.Criteria;
                    if (c.Status.HasValue) parameters["criteria[status]"] = ((int)c.Status.Value).ToString(CultureInfo.InvariantCulture);
                    if (c.ModeratorStatus.HasValue) parameters["criteria[moderatorStatus]"] = ((int)c.ModeratorStatus.Value).ToString(CultureInfo.InvariantCulture);
                    if (c.Code != null) parameters["criteria[code]"] = c.Code;
                    if (c.FullLink != null) parameters["criteria[fullLink]"] = c.FullLink;
                    if (c.Comment != null) parameters["criteria[comment]"] = c.Comment;
                    if (c.CreatedFrom.HasValue) parameters["criteria[createTsFrom]"] = ApiFormat.DateTime(c.CreatedFrom.Value);
                    if (c.CreatedTo.HasValue) parameters["criteria[createTsTo]"] = ApiFormat.DateTime(c.CreatedTo.Value);
                    if (c.ClicksFrom.HasValue) parameters["criteria[clickCntFrom]"] = c.ClicksFrom.Value.ToString(CultureInfo.InvariantCulture);
                    if (c.ClicksTo.HasValue) parameters["criteria[clickCntTo]"] = c.ClicksTo.Value.ToString(CultureInfo.InvariantCulture);
                }

                if (request.Pagination != null)
                {
                    parameters["pagination[currentPage]"] = ApiFormat.Int(request.Pagination.CurrentPage);
                    parameters["pagination[pageSize]"] = ApiFormat.Int(request.Pagination.PageSize);
                }

                if (request.Sort != null)
                {
                    parameters[$"sort[{request.Sort.Field}]"] = ApiFormat.Sort(request.Sort.Direction);
                }
            }

            return (await _apiClient.SendAsync<MobizonListResult<LinkData>>(
                ModuleName, "list", parameters, cancellationToken).ConfigureAwait(false)).Data;
        }

        public async Task UpdateAsync(UpdateLinkRequest request, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string> { ["id"] = ApiFormat.Int(request.Id) };
            if (request.FullLink != null) parameters["data[fullLink]"] = request.FullLink;
            if (request.Status.HasValue) parameters["data[status]"] = ((int)request.Status.Value).ToString(CultureInfo.InvariantCulture);
            if (request.ExpirationDate.HasValue) parameters["data[expirationDate]"] = ApiFormat.Date(request.ExpirationDate.Value);
            if (request.Comment != null) parameters["data[comment]"] = request.Comment;
            await _apiClient.SendAsync<object>(ModuleName, "update", parameters, cancellationToken).ConfigureAwait(false);
        }
    }
}

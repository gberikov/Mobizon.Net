using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts.Models;
using Mobizon.Contracts.Models.Campaign;
using Mobizon.Contracts.Services;
using Mobizon.Net.Internal;

namespace Mobizon.Net.Services
{
    internal class CampaignService : ICampaignService
    {
        private readonly MobizonApiClient _apiClient;

        public CampaignService(MobizonApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public Task<MobizonResponse<CreateCampaignResult>> CreateAsync(
            CreateCampaignRequest request, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["data[type]"] = request.Type.ToString(),
                ["data[from]"] = request.From,
                ["data[text]"] = request.Text
            };

            return _apiClient.SendAsync<CreateCampaignResult>(
                HttpMethod.Post, "campaign", "create", parameters, cancellationToken);
        }

        public Task<MobizonResponse<object>> DeleteAsync(
            int id, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["id"] = id.ToString()
            };

            return _apiClient.SendAsync<object>(
                HttpMethod.Post, "campaign", "delete", parameters, cancellationToken);
        }

        public Task<MobizonResponse<CampaignData>> GetAsync(
            int id, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["id"] = id.ToString()
            };

            return _apiClient.SendAsync<CampaignData>(
                HttpMethod.Post, "campaign", "get", parameters, cancellationToken);
        }

        public Task<MobizonResponse<CampaignInfo>> GetInfoAsync(
            int id, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["id"] = id.ToString()
            };

            return _apiClient.SendAsync<CampaignInfo>(
                HttpMethod.Post, "campaign", "getinfo", parameters, cancellationToken);
        }

        public Task<MobizonResponse<IReadOnlyList<CampaignData>>> ListAsync(
            CampaignListRequest? request = null, CancellationToken cancellationToken = default)
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

            return _apiClient.SendAsync<IReadOnlyList<CampaignData>>(
                HttpMethod.Post, "campaign", "list", parameters, cancellationToken);
        }

        public Task<MobizonResponse<CampaignSendResult>> SendAsync(
            int id, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["id"] = id.ToString()
            };

            return _apiClient.SendAsync<CampaignSendResult>(
                HttpMethod.Post, "campaign", "send", parameters, cancellationToken);
        }

        public Task<MobizonResponse<object>> AddRecipientsAsync(
            AddRecipientsRequest request, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["campaignId"] = request.CampaignId.ToString(),
                ["type"] = request.Type.ToString()
            };

            for (var i = 0; i < request.Data.Count; i++)
            {
                parameters[$"data[{i}]"] = request.Data[i];
            }

            return _apiClient.SendAsync<object>(
                HttpMethod.Post, "campaign", "addrecipients", parameters, cancellationToken);
        }
    }
}

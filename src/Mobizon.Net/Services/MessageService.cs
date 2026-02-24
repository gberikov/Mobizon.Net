using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts.Models;
using Mobizon.Contracts.Models.Message;
using Mobizon.Contracts.Services;
using Mobizon.Net.Internal;

namespace Mobizon.Net.Services
{
    internal class MessageService : IMessageService
    {
        private const string ModuleName = "Message";
        private readonly MobizonApiClient _apiClient;

        public MessageService(MobizonApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public Task<MobizonResponse<SendSmsResult>> QuickSendAsync(
            string recipient,
            string text,
            CancellationToken cancellationToken = default)
        {
            return SendSmsMessageAsync(
                new SendSmsMessageRequest { Recipient = recipient, Text = text },
                cancellationToken);
        }

        public Task<MobizonResponse<SendSmsResult>> SendSmsMessageAsync(
            SendSmsMessageRequest request,
            CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["recipient"] = request.Recipient,
                ["text"] = request.Text
            };

            if (request.From != null)
                parameters["from"] = request.From;

            if (request.Parameters != null)
            {
                var p = request.Parameters;

                if (p.Name != null)
                    parameters["params[name]"] = p.Name;

                if (p.DeferredTo.HasValue)
                    parameters["params[deferredToTs]"] = p.DeferredTo.Value.ToString("yyyy-MM-dd HH:mm:ss");

                if (p.MessageClass.HasValue)
                    parameters["params[mclass]"] = ((int)p.MessageClass.Value).ToString();

                if (p.Validity.HasValue)
                    parameters["params[validity]"] = ((int)p.Validity.Value.TotalMinutes).ToString();
            }

            return _apiClient.SendAsync<SendSmsResult>(
                HttpMethod.Post, ModuleName, "SendSmsMessage", parameters, cancellationToken);
        }

        public Task<MobizonResponse<IReadOnlyList<SmsStatusResult>>> GetSmsStatusAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return GetSmsStatusAsync(new[] { id }, cancellationToken);
        }

        public Task<MobizonResponse<IReadOnlyList<SmsStatusResult>>> GetSmsStatusAsync(
            int[] ids,
            CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>();
            for (var i = 0; i < ids.Length; i++)
            {
                parameters[$"ids[{i}]"] = ids[i].ToString();
            }

            return _apiClient.SendAsync<IReadOnlyList<SmsStatusResult>>(
                HttpMethod.Post, ModuleName, "GetSMSStatus", parameters, cancellationToken);
        }

        public Task<MobizonResponse<MessageListResponse>> ListAsync(
            MessageListRequest? request = null,
            CancellationToken cancellationToken = default)
        {
            Dictionary<string, string>? parameters = null;

            if (request != null)
            {
                parameters = new Dictionary<string, string>();

                if (request.CampaignIds != null)
                    parameters["criteria[campaignIds]"] = request.CampaignIds;

                if (request.From != null)
                    parameters["criteria[from]"] = request.From;

                if (request.To != null)
                    parameters["criteria[to]"] = request.To;

                if (request.Text != null)
                    parameters["criteria[text]"] = request.Text;

                if (request.Status.HasValue)
                    parameters["criteria[status]"] = request.Status.Value.ToString();

                if (request.Groups != null)
                    parameters["criteria[groups]"] = request.Groups;

                if (request.CampaignStatus != null)
                    parameters["criteria[campaignStatus]"] = request.CampaignStatus;

                if (request.CampaignCreateTsFrom != null)
                    parameters["criteria[campaignCreateTsFrom]"] = request.CampaignCreateTsFrom;

                if (request.CampaignCreateTsTo != null)
                    parameters["criteria[campaignCreateTsTo]"] = request.CampaignCreateTsTo;

                if (request.CampaignSentTsFrom != null)
                    parameters["criteria[campaignSentTsFrom]"] = request.CampaignSentTsFrom;

                if (request.CampaignSentTsTo != null)
                    parameters["criteria[campaignSentTsTo]"] = request.CampaignSentTsTo;

                if (request.StartSendTsFrom != null)
                    parameters["criteria[startSendTsFrom]"] = request.StartSendTsFrom;

                if (request.StartSendTsTo != null)
                    parameters["criteria[startSendTsTo]"] = request.StartSendTsTo;

                if (request.StatusUpdateTsFrom != null)
                    parameters["criteria[statusUpdateTsFrom]"] = request.StatusUpdateTsFrom;

                if (request.StatusUpdateTsTo != null)
                    parameters["criteria[statusUpdateTsTo]"] = request.StatusUpdateTsTo;

                if (request.WithNumberInfo.HasValue)
                    parameters["withNumberInfo"] = request.WithNumberInfo.Value.ToString();

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

            return _apiClient.SendAsync<MessageListResponse>(
                HttpMethod.Post, ModuleName, "List", parameters, cancellationToken);
        }
    }
}

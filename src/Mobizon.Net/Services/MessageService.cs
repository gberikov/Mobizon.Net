using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts.Models.Common;
using Mobizon.Contracts.Models.Campaigns;
using Mobizon.Contracts.Models.Messages;
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

                if (request.Criteria != null)
                {
                    var c = request.Criteria;

                    if (c.CampaignIds != null)
                        for (var i = 0; i < c.CampaignIds.Count; i++)
                            parameters[$"criteria[campaignIds][{i}]"] = c.CampaignIds[i].ToString();

                    if (c.From != null)
                        parameters["criteria[from]"] = c.From;

                    if (c.To != null)
                        parameters["criteria[to]"] = c.To;

                    if (c.Text != null)
                        parameters["criteria[text]"] = c.Text;

                    if (c.Status.HasValue)
                        parameters["criteria[status]"] = SmsStatusToApiCode(c.Status.Value);

                    if (c.Groups != null)
                        for (var i = 0; i < c.Groups.Count; i++)
                            parameters[$"criteria[groups][{i}]"] = c.Groups[i].ToString();

                    if (c.CampaignStatus.HasValue)
                        parameters["criteria[campaignStatus]"] = CampaignCommonStatusToApiCode(c.CampaignStatus.Value);

                    if (c.CampaignCreatedFrom.HasValue)
                        parameters["criteria[campaignCreateTsFrom]"] = c.CampaignCreatedFrom.Value.ToString("yyyy-MM-dd HH:mm:ss");

                    if (c.CampaignCreatedTo.HasValue)
                        parameters["criteria[campaignCreateTsTo]"] = c.CampaignCreatedTo.Value.ToString("yyyy-MM-dd HH:mm:ss");

                    if (c.CampaignSentFrom.HasValue)
                        parameters["criteria[campaignSentTsFrom]"] = c.CampaignSentFrom.Value.ToString("yyyy-MM-dd HH:mm:ss");

                    if (c.CampaignSentTo.HasValue)
                        parameters["criteria[campaignSentTsTo]"] = c.CampaignSentTo.Value.ToString("yyyy-MM-dd HH:mm:ss");

                    if (c.SentFrom.HasValue)
                        parameters["criteria[startSendTsFrom]"] = c.SentFrom.Value.ToString("yyyy-MM-dd HH:mm:ss");

                    if (c.SentTo.HasValue)
                        parameters["criteria[startSendTsTo]"] = c.SentTo.Value.ToString("yyyy-MM-dd HH:mm:ss");

                    if (c.StatusUpdatedFrom.HasValue)
                        parameters["criteria[statusUpdateTsFrom]"] = c.StatusUpdatedFrom.Value.ToString("yyyy-MM-dd HH:mm:ss");

                    if (c.StatusUpdatedTo.HasValue)
                        parameters["criteria[statusUpdateTsTo]"] = c.StatusUpdatedTo.Value.ToString("yyyy-MM-dd HH:mm:ss");
                }

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

        private static string SmsStatusToApiCode(SmsStatus status)
        {
            switch (status)
            {
                case SmsStatus.New:         return "NEW";
                case SmsStatus.Enqueued:    return "ENQUEUD";
                case SmsStatus.Accepted:    return "ACCEPTD";
                case SmsStatus.Delivered:   return "DELIVRD";
                case SmsStatus.Undelivered: return "UNDELIV";
                case SmsStatus.Rejected:    return "REJECTD";
                case SmsStatus.Expired:     return "EXPIRD";
                case SmsStatus.Deleted:     return "DELETED";
                default: throw new System.ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }

        private static string CampaignCommonStatusToApiCode(CampaignCommonStatus status)
        {
            switch (status)
            {
                case CampaignCommonStatus.Moderation:       return "MODERATION";
                case CampaignCommonStatus.Declined:         return "DECLINED";
                case CampaignCommonStatus.ReadyForSend:     return "READY_FOR_SEND";
                case CampaignCommonStatus.AutoReadyForSend: return "AUTO_READY_FOR_SEND";
                case CampaignCommonStatus.Running:          return "RUNNING";
                case CampaignCommonStatus.Sent:             return "SENT";
                case CampaignCommonStatus.Done:             return "DONE";
                default: throw new System.ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }
    }
}

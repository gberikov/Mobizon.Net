using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts;
using Mobizon.Net.Internal;
using Mobizon.Net.Internal.Converters;

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

        public async Task<SendSmsResult> QuickSendAsync(
            string recipient,
            string text,
            CancellationToken cancellationToken = default)
        {
            return await SendSmsMessageAsync(
                new SendSmsMessageRequest { Recipient = recipient, Text = text },
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<SendSmsResult> SendSmsMessageAsync(
            SendSmsMessageRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new System.ArgumentNullException(nameof(request));

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
                    parameters["params[deferredToTs]"] = ApiFormat.DateTime(p.DeferredTo.Value);

                if (p.MessageClass.HasValue)
                    parameters["params[mclass]"] = ((int)p.MessageClass.Value).ToString();

                if (p.Validity.HasValue)
                    parameters["params[validity]"] = ((int)p.Validity.Value.TotalMinutes).ToString();

                if (p.ShortenLinks.HasValue)
                    parameters["params[shortenLinks]"] = ApiFormat.Bool(p.ShortenLinks.Value);
            }

            return (await _apiClient.SendAsync<SendSmsResult>(
                ModuleName, "SendSmsMessage", parameters, cancellationToken).ConfigureAwait(false)).Data;
        }

        public async Task<IReadOnlyList<SmsStatusResult>> GetSmsStatusAsync(
            long id,
            CancellationToken cancellationToken = default)
        {
            return await GetSmsStatusAsync(new[] { id }, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<SmsStatusResult>> GetSmsStatusAsync(
            long[] ids,
            CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>();
            for (var i = 0; i < ids.Length; i++)
            {
                parameters[$"ids[{i}]"] = ids[i].ToString();
            }

            return (await _apiClient.SendAsync<IReadOnlyList<SmsStatusResult>>(
                ModuleName, "GetSMSStatus", parameters, cancellationToken).ConfigureAwait(false)).Data!;
        }

        public async Task<MobizonListResult<MessageInfo>> ListAsync(
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
                        parameters["criteria[status]"] = ApiStatusCodes.ToApiCode(c.Status.Value);

                    if (c.Groups != null)
                        for (var i = 0; i < c.Groups.Count; i++)
                            parameters[$"criteria[groups][{i}]"] = c.Groups[i].ToString();

                    if (c.CampaignStatus.HasValue)
                        parameters["criteria[campaignStatus]"] = ApiStatusCodes.ToApiCode(c.CampaignStatus.Value);

                    if (c.CampaignCreatedFrom.HasValue)
                        parameters["criteria[campaignCreateTsFrom]"] = ApiFormat.DateTime(c.CampaignCreatedFrom.Value);

                    if (c.CampaignCreatedTo.HasValue)
                        parameters["criteria[campaignCreateTsTo]"] = ApiFormat.DateTime(c.CampaignCreatedTo.Value);

                    if (c.CampaignSentFrom.HasValue)
                        parameters["criteria[campaignSentTsFrom]"] = ApiFormat.DateTime(c.CampaignSentFrom.Value);

                    if (c.CampaignSentTo.HasValue)
                        parameters["criteria[campaignSentTsTo]"] = ApiFormat.DateTime(c.CampaignSentTo.Value);

                    if (c.SentFrom.HasValue)
                        parameters["criteria[startSendTsFrom]"] = ApiFormat.DateTime(c.SentFrom.Value);

                    if (c.SentTo.HasValue)
                        parameters["criteria[startSendTsTo]"] = ApiFormat.DateTime(c.SentTo.Value);

                    if (c.StatusUpdatedFrom.HasValue)
                        parameters["criteria[statusUpdateTsFrom]"] = ApiFormat.DateTime(c.StatusUpdatedFrom.Value);

                    if (c.StatusUpdatedTo.HasValue)
                        parameters["criteria[statusUpdateTsTo]"] = ApiFormat.DateTime(c.StatusUpdatedTo.Value);
                }

                if (request.WithNumberInfo.HasValue)
                    parameters["withNumberInfo"] = ApiFormat.Bool(request.WithNumberInfo.Value);

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

            return (await _apiClient.SendAsync<MobizonListResult<MessageInfo>>(
                ModuleName, "List", parameters, cancellationToken).ConfigureAwait(false)).Data;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts;
using Mobizon.Net.Internal;
using Mobizon.Net.Internal.Converters;

namespace Mobizon.Net.Services
{
    internal class MessageService(MobizonApiClient apiClient) : IMessageService
    {
        private const string ModuleName = "Message";

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
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.Recipient))
                throw new ArgumentException("Recipient is required.", nameof(request));
            if (string.IsNullOrEmpty(request.Text))
                throw new ArgumentException("Text is required.", nameof(request));

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
                    parameters["params[mclass]"] = ApiFormat.Int((int)p.MessageClass.Value);

                if (p.Validity.HasValue)
                    parameters["params[validity]"] = ApiFormat.Int((int)p.Validity.Value.TotalMinutes);

                if (p.ShortenLinks.HasValue)
                    parameters["params[shortenLinks]"] = ApiFormat.Bool(p.ShortenLinks.Value);
            }

            return (await apiClient.SendAsync<SendSmsResult>(
                ModuleName, "SendSmsMessage", parameters, cancellationToken).ConfigureAwait(false)).Data;
        }

        public async Task<IReadOnlyList<SmsStatusResult>> GetSmsStatusAsync(
            long id,
            CancellationToken cancellationToken = default)
        {
            return await GetSmsStatusAsync(new[] { id }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>The API accepts at most this many message ids per <c>message/getSMSStatus</c> call.</summary>
        public const int GetSmsStatusMaxIds = 100;

        public async Task<IReadOnlyList<SmsStatusResult>> GetSmsStatusAsync(
            long[] ids,
            CancellationToken cancellationToken = default)
        {
            if (ids == null) throw new ArgumentNullException(nameof(ids));
            if (ids.Length == 0 || ids.Length > GetSmsStatusMaxIds)
                throw new ArgumentException(
                    $"message/getSMSStatus accepts 1 to {GetSmsStatusMaxIds} message ids per request; split larger sets into several calls.",
                    nameof(ids));

            var parameters = new Dictionary<string, string>();
            for (var i = 0; i < ids.Length; i++)
            {
                parameters[$"ids[{i}]"] = ApiFormat.Int(ids[i]);
            }

            return (await apiClient.SendAsync<IReadOnlyList<SmsStatusResult>>(
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
                            parameters[$"criteria[campaignIds][{i}]"] = ApiFormat.Int(c.CampaignIds[i]);

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
                            parameters[$"criteria[groups][{i}]"] = ApiFormat.Int(c.Groups[i]);

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
                    parameters["pagination[currentPage]"] = ApiFormat.Int(request.Pagination.CurrentPage);
                    parameters["pagination[pageSize]"] = ApiFormat.Int(request.Pagination.PageSize);
                }

                if (request.Sort != null)
                {
                    parameters[$"sort[{request.Sort.Field}]"] = ApiFormat.Sort(request.Sort.Direction);
                }
            }

            return (await apiClient.SendAsync<MobizonListResult<MessageInfo>>(
                ModuleName, "List", parameters, cancellationToken).ConfigureAwait(false)).Data;
        }
    }
}

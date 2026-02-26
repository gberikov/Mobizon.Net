using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts.Models.Common;
using Mobizon.Contracts.Models.Campaigns;
using Mobizon.Contracts.Services;
using Mobizon.Net.Internal;

namespace Mobizon.Net.Services
{
    internal class CampaignService : ICampaignService
    {
        private const string ModuleName = "Campaign";
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
                ["data[type]"] = ((int)request.Type).ToString(),
                ["data[text]"] = request.Text
            };

            if (request.Name != null)
                parameters["data[name]"] = request.Name;

            if (request.From != null)
                parameters["data[from]"] = request.From;

            if (request.RateLimit.HasValue)
                parameters["data[rateLimit]"] = request.RateLimit.Value.ToString();

            if (request.RatePeriod.HasValue)
                parameters["data[ratePeriod]"] = request.RatePeriod.Value.ToString();

            if (request.DeferredTo.HasValue)
                parameters["data[deferredToTs]"] = request.DeferredTo.Value.ToString("yyyy-MM-dd HH:mm:ss");

            if (request.MessageClass.HasValue)
                parameters["data[mclass]"] = ((int)request.MessageClass.Value).ToString();

            if (request.Validity.HasValue)
                parameters["data[validity]"] = ((int)request.Validity.Value.TotalMinutes).ToString();

            if (request.TrackShortLinkRecipients.HasValue)
                parameters["data[trackShortLinkRecipients]"] = request.TrackShortLinkRecipients.Value ? "1" : "0";

            return _apiClient.SendAsync<CreateCampaignResult>(
                HttpMethod.Post, ModuleName, "Create", parameters, cancellationToken);
        }

        public Task<MobizonResponse<object>> DeleteAsync(
            int id, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["id"] = id.ToString()
            };

            return _apiClient.SendAsync<object>(
                HttpMethod.Post, ModuleName, "Delete", parameters, cancellationToken);
        }

        public Task<MobizonResponse<CampaignData>> GetAsync(
            int id, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["id"] = id.ToString()
            };

            return _apiClient.SendAsync<CampaignData>(
                HttpMethod.Post, ModuleName, "Get", parameters, cancellationToken);
        }

        public Task<MobizonResponse<CampaignInfo>> GetInfoAsync(
            int id, int? getFilledTplCampaignText = null, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["id"] = id.ToString()
            };

            if (getFilledTplCampaignText.HasValue)
                parameters["getFilledTplCampaignText"] = getFilledTplCampaignText.Value.ToString();

            return _apiClient.SendAsync<CampaignInfo>(
                HttpMethod.Post, ModuleName, "GetInfo", parameters, cancellationToken);
        }

        public Task<MobizonResponse<IReadOnlyList<CampaignData>>> ListAsync(
            CampaignListRequest? request = null, CancellationToken cancellationToken = default)
        {
            Dictionary<string, string>? parameters = null;

            if (request != null)
            {
                parameters = new Dictionary<string, string>();

                if (request.Criteria != null)
                {
                    var c = request.Criteria;

                    if (c.Id.HasValue)
                        parameters["criteria[id]"] = c.Id.Value.ToString();

                    if (c.Ids != null)
                        for (var i = 0; i < c.Ids.Count; i++)
                            parameters[$"criteria[ids][{i}]"] = c.Ids[i].ToString();

                    if (c.Recipient != null)
                        parameters["criteria[recipient]"] = c.Recipient;

                    if (c.From != null)
                        parameters["criteria[from]"] = c.From;

                    if (c.Text != null)
                        parameters["criteria[text]"] = c.Text;

                    if (c.Status != null)
                        parameters["criteria[status]"] = c.Status;

                    if (c.CreateTsFrom != null)
                        parameters["criteria[createTsFrom]"] = c.CreateTsFrom;

                    if (c.CreateTsTo != null)
                        parameters["criteria[createTsTo]"] = c.CreateTsTo;

                    if (c.SentTsFrom != null)
                        parameters["criteria[sentTsFrom]"] = c.SentTsFrom;

                    if (c.SentTsTo != null)
                        parameters["criteria[sentTsTo]"] = c.SentTsTo;

                    if (c.Type.HasValue)
                        parameters["criteria[type]"] = c.Type.Value.ToString();

                    if (c.Groups != null)
                        for (var i = 0; i < c.Groups.Count; i++)
                            parameters[$"criteria[groups][{i}]"] = c.Groups[i];
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

            return _apiClient.SendAsync<IReadOnlyList<CampaignData>>(
                HttpMethod.Post, ModuleName, "List", parameters, cancellationToken);
        }

        public Task<MobizonResponse<CampaignSendResult>> SendAsync(
            int id, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["id"] = id.ToString()
            };

            return _apiClient.SendAsync<CampaignSendResult>(
                HttpMethod.Post, ModuleName, "Send", parameters, cancellationToken);
        }

        private const int AddRecipientsMaxBatchSize = 500;

        public async Task<MobizonResponse<AddRecipientsResult>> AddRecipientsAsync(
            AddRecipientsRequest request, CancellationToken cancellationToken = default)
        {
            // Groups and file recipients are asynchronous — no limit applies, send as-is.
            if (request.RecipientGroups != null || (request.Recipients == null && request.RecipientContacts == null))
                return await SendAddRecipientsAsync(request, cancellationToken).ConfigureAwait(false);

            // Split synchronous recipients (phone numbers / contact cards) into batches of 500.
            var recipients = request.Recipients;
            var contacts = request.RecipientContacts;
            var totalCount = (recipients?.Count ?? 0) + (contacts?.Count ?? 0);

            // Fast path: fits within a single batch.
            if (totalCount <= AddRecipientsMaxBatchSize)
                return await SendAddRecipientsAsync(request, cancellationToken).ConfigureAwait(false);

            // Multi-batch path: aggregate all per-recipient entries into the first response.
            MobizonResponse<AddRecipientsResult>? aggregated = null;

            var recipientOffset = 0;
            var contactOffset = 0;

            while (recipientOffset < (recipients?.Count ?? 0) || contactOffset < (contacts?.Count ?? 0))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var remaining = AddRecipientsMaxBatchSize;

                IReadOnlyList<RecipientEntry>? batchRecipients = null;
                IReadOnlyList<string>? batchContacts = null;

                if (recipients != null && recipientOffset < recipients.Count)
                {
                    var take = Math.Min(remaining, recipients.Count - recipientOffset);
                    batchRecipients = Slice(recipients, recipientOffset, take);
                    recipientOffset += take;
                    remaining -= take;
                }

                if (contacts != null && contactOffset < contacts.Count && remaining > 0)
                {
                    var take = Math.Min(remaining, contacts.Count - contactOffset);
                    batchContacts = Slice(contacts, contactOffset, take);
                    contactOffset += take;
                }

                // Only the very first batch may honour Replace=1 to avoid wiping already-added recipients.
                var batchParams = request.Parameters;
                if (aggregated != null && batchParams?.Replace == 1)
                {
                    batchParams = new AddRecipientsParameters
                    {
                        Replace = 0,
                        PlaceholdersFlag = request.Parameters!.PlaceholdersFlag,
                        RecipientsFileEncoding = request.Parameters.RecipientsFileEncoding,
                        RecipientsFileSkipHeader = request.Parameters.RecipientsFileSkipHeader,
                        RecipientsFileDelimiter = request.Parameters.RecipientsFileDelimiter,
                        RecipientsFileEnclosure = request.Parameters.RecipientsFileEnclosure
                    };
                }

                var batchRequest = new AddRecipientsRequest
                {
                    CampaignId = request.CampaignId,
                    Recipients = batchRecipients,
                    RecipientContacts = batchContacts,
                    Parameters = batchParams
                };

                var response = await SendAddRecipientsAsync(batchRequest, cancellationToken).ConfigureAwait(false);

                if (aggregated == null)
                {
                    aggregated = response;
                }
                else
                {
                    aggregated.Data?.MergeEntries(response.Data);

                    // Reflect worst-case response code: prefer 99 (all failed) > 98 (partial) > 0 (success).
                    if (response.RawCode > aggregated.RawCode)
                        aggregated.RawCode = response.RawCode;
                }
            }

            return aggregated!;
        }

        private Task<MobizonResponse<AddRecipientsResult>> SendAddRecipientsAsync(
            AddRecipientsRequest request, CancellationToken cancellationToken)
        {
            var parameters = new Dictionary<string, string>
            {
                ["id"] = request.CampaignId.ToString()
            };

            if (request.Recipients != null)
            {
                for (var i = 0; i < request.Recipients.Count; i++)
                {
                    var entry = request.Recipients[i];
                    parameters[$"recipients[{i}][recipient]"] = entry.Recipient;

                    if (entry.Placeholders != null)
                        foreach (var kv in entry.Placeholders)
                            parameters[$"recipients[{i}][{kv.Key}]"] = kv.Value;
                }
            }

            if (request.RecipientContacts != null)
                for (var i = 0; i < request.RecipientContacts.Count; i++)
                    parameters[$"recipientContacts[{i}]"] = request.RecipientContacts[i];

            if (request.RecipientGroups != null)
                for (var i = 0; i < request.RecipientGroups.Count; i++)
                    parameters[$"recipientGroups[{i}]"] = request.RecipientGroups[i];

            if (request.Parameters != null)
            {
                var p = request.Parameters;

                if (p.Replace.HasValue)
                    parameters["params[replace]"] = p.Replace.Value.ToString();

                if (p.PlaceholdersFlag.HasValue)
                    parameters["params[placeholdersFlag]"] = p.PlaceholdersFlag.Value.ToString();

                if (p.RecipientsFileEncoding != null)
                    parameters["params[recipientsFileEncoding]"] = p.RecipientsFileEncoding;

                if (p.RecipientsFileSkipHeader.HasValue)
                    parameters["params[recipientsFileSkipHeader]"] = p.RecipientsFileSkipHeader.Value.ToString();

                if (p.RecipientsFileDelimiter != null)
                    parameters["params[recipientsFileDelimiter]"] = p.RecipientsFileDelimiter;

                if (p.RecipientsFileEnclosure != null)
                    parameters["params[recipientsFileEnclosure]"] = p.RecipientsFileEnclosure;
            }

            return _apiClient.SendAsync<AddRecipientsResult>(
                HttpMethod.Post, ModuleName, "AddRecipients", parameters, cancellationToken,
                extraSuccessCodes: new[] { (int)AddRecipientsResponseCode.PartiallyAdded, (int)AddRecipientsResponseCode.NoneAdded });
        }

        private static IReadOnlyList<T> Slice<T>(IReadOnlyList<T> source, int offset, int count)
        {
            var result = new List<T>(count);
            for (var i = 0; i < count; i++)
                result.Add(source[offset + i]);
            return result;
        }
    }
}


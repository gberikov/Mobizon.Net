using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts;
using Mobizon.Net.Internal;
using Mobizon.Net.Internal.Converters;

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

        public async Task<long> CreateAsync(
            CreateCampaignRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

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
                parameters["data[deferredToTs]"] = ApiFormat.DateTime(request.DeferredTo.Value);

            if (request.MessageClass.HasValue)
                parameters["data[mclass]"] = ((int)request.MessageClass.Value).ToString();

            if (request.Validity.HasValue)
                parameters["data[validity]"] = ((int)request.Validity.Value.TotalMinutes).ToString();

            if (request.TrackShortLinkRecipients.HasValue)
                parameters["data[trackShortLinkRecipients]"] = request.TrackShortLinkRecipients.Value ? "1" : "0";

            if (request.ShortenLinks.HasValue)
                parameters["data[shortenLinks]"] = request.ShortenLinks.Value ? "1" : "0";

            return (await _apiClient.SendAsync<long>(
                ModuleName, "Create", parameters, cancellationToken).ConfigureAwait(false)).Data;
        }

        public async Task DeleteAsync(
            long id, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["id"] = id.ToString()
            };

            await _apiClient.SendAsync<object>(
                ModuleName, "Delete", parameters, cancellationToken).ConfigureAwait(false);
        }

        public async Task<CampaignData> GetAsync(
            long id, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["id"] = id.ToString()
            };

            return (await _apiClient.SendAsync<CampaignData>(
                ModuleName, "Get", parameters, cancellationToken).ConfigureAwait(false)).Data;
        }

        public async Task<CampaignInfo> GetInfoAsync(
            long id, bool? fillTemplateText = null, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["id"] = id.ToString()
            };

            if (fillTemplateText.HasValue)
                parameters["getFilledTplCampaignText"] = ApiFormat.Bool(fillTemplateText.Value);

            return (await _apiClient.SendAsync<CampaignInfo>(
                ModuleName, "GetInfo", parameters, cancellationToken).ConfigureAwait(false)).Data;
        }

        public async Task<MobizonListResult<CampaignData>> ListAsync(
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

                    if (c.Status.HasValue)
                        parameters["criteria[status]"] = ApiStatusCodes.ToApiCode(c.Status.Value);

                    if (c.CreatedFrom.HasValue)
                        parameters["criteria[createTsFrom]"] = ApiFormat.DateTime(c.CreatedFrom.Value);
                    if (c.CreatedTo.HasValue)
                        parameters["criteria[createTsTo]"] = ApiFormat.DateTime(c.CreatedTo.Value);
                    if (c.SentFrom.HasValue)
                        parameters["criteria[sentTsFrom]"] = ApiFormat.DateTime(c.SentFrom.Value);
                    if (c.SentTo.HasValue)
                        parameters["criteria[sentTsTo]"] = ApiFormat.DateTime(c.SentTo.Value);

                    if (c.Type.HasValue)
                        parameters["criteria[type]"] = ApiFormat.Int((int)c.Type.Value);

                    if (c.Groups != null)
                        for (var i = 0; i < c.Groups.Count; i++)
                            parameters[$"criteria[groups][{i}]"] = ApiFormat.Int(c.Groups[i]);
                }

                if (request.Pagination != null)
                {
                    parameters["pagination[currentPage]"] = request.Pagination.CurrentPage.ToString();
                    parameters["pagination[pageSize]"] = request.Pagination.PageSize.ToString();
                }

                if (request.Sort != null)
                {
                    parameters[$"sort[{request.Sort.Field}]"] = ApiFormat.Sort(request.Sort.Direction);
                }
            }

            return (await _apiClient.SendAsync<MobizonListResult<CampaignData>>(
                ModuleName, "List", parameters, cancellationToken).ConfigureAwait(false)).Data;
        }

        public async Task<CampaignSendResult> SendAsync(
            long id, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string> { ["id"] = id.ToString() };

            var response = await _apiClient.SendAsync<long>(
                ModuleName, "Send", parameters, cancellationToken).ConfigureAwait(false);

            return new CampaignSendResult
            {
                IsQueued = response.Code == MobizonResponseCode.BackgroundTask,
                Id = response.Data
            };
        }

        private const int AddRecipientsMaxBatchSize = 500;

        public async Task<AddRecipientsResult> AddRecipientsAsync(
            AddRecipientsRequest request, CancellationToken cancellationToken = default)
        {
            var sources = (request.Recipients != null ? 1 : 0)
                        + (request.RecipientContacts != null ? 1 : 0)
                        + (request.RecipientGroups != null ? 1 : 0)
                        + (request.RecipientsFile != null ? 1 : 0);
            if (sources != 1)
                throw new ArgumentException(
                    "Exactly one recipient source must be set (Recipients, RecipientContacts, RecipientGroups, or RecipientsFile).",
                    nameof(request));

            if (request.RecipientsFile != null)
            {
                var fields = new Dictionary<string, string> { ["id"] = request.CampaignId.ToString() };
                AppendParams(fields, request.Parameters);
                return Finalize(await _apiClient.SendMultipartAsync<AddRecipientsResult>(
                    ModuleName, "AddRecipients", fields, request.RecipientsFile,
                    request.RecipientsFileName ?? "recipients.csv",
                    cancellationToken, fileFieldName: "recipientsFile").ConfigureAwait(false));
            }

            // Groups are asynchronous — no limit applies, send as-is.
            if (request.RecipientGroups != null)
                return Finalize(await SendAddRecipientsAsync(request, cancellationToken).ConfigureAwait(false));

            // Split synchronous recipients (phone numbers / contact cards) into batches of 500.
            var recipients = request.Recipients;
            var contacts = request.RecipientContacts;
            var totalCount = (recipients?.Count ?? 0) + (contacts?.Count ?? 0);

            // Fast path: fits within a single batch.
            if (totalCount <= AddRecipientsMaxBatchSize)
                return Finalize(await SendAddRecipientsAsync(request, cancellationToken).ConfigureAwait(false));

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

                // Only the very first batch may honour Replace=true to avoid wiping already-added recipients.
                var batchParams = request.Parameters;
                if (aggregated != null && batchParams?.Replace == true)
                {
                    batchParams = new AddRecipientsParameters
                    {
                        Replace = false,
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
                    // If the first batch came back with a null payload, adopt the next batch's payload
                    // so its entries are not silently dropped; otherwise merge into the aggregate.
                    if (aggregated.Data == null)
                        aggregated.Data = response.Data;
                    else if (response.Data != null)
                        aggregated.Data.MergeEntries(response.Data);

                    // Reflect worst-case response code: prefer 99 (all failed) > 98 (partial) > 0 (success).
                    if (response.RawCode > aggregated.RawCode)
                        aggregated.RawCode = response.RawCode;
                }
            }

            return Finalize(aggregated!);
        }

        private static AddRecipientsResult Finalize(MobizonResponse<AddRecipientsResult> response)
        {
            var result = response.Data ?? new AddRecipientsResult();
            result.Outcome = response.RawCode == 98 ? AddRecipientsOutcome.PartiallyAdded
                           : response.RawCode == 99 ? AddRecipientsOutcome.NoneAdded
                           : AddRecipientsOutcome.AllAdded; // 0 or 100 (async accepted)
            return result;
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
                    parameters[$"recipientGroups[{i}]"] = ApiFormat.Int(request.RecipientGroups[i]);

            AppendParams(parameters, request.Parameters);

            return _apiClient.SendAsync<AddRecipientsResult>(
                ModuleName, "AddRecipients", parameters, cancellationToken,
                extraSuccessCodes: new[] { (int)AddRecipientsOutcome.PartiallyAdded, (int)AddRecipientsOutcome.NoneAdded });
        }

        private static void AppendParams(IDictionary<string, string> parameters, AddRecipientsParameters? prm)
        {
            if (prm == null)
                return;

            if (prm.Replace.HasValue)
                parameters["params[replace]"] = prm.Replace.Value ? "1" : "0";

            if (prm.PlaceholdersFlag.HasValue)
                parameters["params[placeholdersFlag]"] = ((int)prm.PlaceholdersFlag.Value).ToString(System.Globalization.CultureInfo.InvariantCulture);

            if (prm.RecipientsFileEncoding != null)
                parameters["params[recipientsFileEncoding]"] = prm.RecipientsFileEncoding;

            if (prm.RecipientsFileSkipHeader.HasValue)
                parameters["params[recipientsFileSkipHeader]"] = prm.RecipientsFileSkipHeader.Value ? "1" : "0";

            if (prm.RecipientsFileDelimiter != null)
                parameters["params[recipientsFileDelimiter]"] = prm.RecipientsFileDelimiter;

            if (prm.RecipientsFileEnclosure != null)
                parameters["params[recipientsFileEnclosure]"] = prm.RecipientsFileEnclosure;
        }

        public async Task<IReadOnlyList<LinkData>> GetLinksAsync(
            long campaignId, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string> { ["campaignId"] = ApiFormat.Int(campaignId) };
            return (await _apiClient.SendAsync<IReadOnlyList<LinkData>>(
                "link", "getlinks", parameters, cancellationToken).ConfigureAwait(false)).Data;
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

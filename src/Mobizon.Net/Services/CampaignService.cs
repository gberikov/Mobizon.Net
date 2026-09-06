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
                ["data[type]"] = ApiFormat.Int((int)request.Type),
                ["data[text]"] = request.Text
            };

            if (request.Name != null)
                parameters["data[name]"] = request.Name;

            if (request.From != null)
                parameters["data[from]"] = request.From;

            if (request.RateLimit.HasValue)
                parameters["data[rateLimit]"] = ApiFormat.Int(request.RateLimit.Value);

            if (request.RatePeriod.HasValue)
                parameters["data[ratePeriod]"] = ApiFormat.Int(request.RatePeriod.Value);

            if (request.DeferredTo.HasValue)
                parameters["data[deferredToTs]"] = ApiFormat.DateTime(request.DeferredTo.Value);

            if (request.MessageClass.HasValue)
                parameters["data[mclass]"] = ApiFormat.Int((int)request.MessageClass.Value);

            if (request.Validity.HasValue)
                parameters["data[validity]"] = ApiFormat.Int((int)request.Validity.Value.TotalMinutes);

            if (request.TrackShortLinkRecipients.HasValue)
                parameters["data[trackShortLinkRecipients]"] = request.TrackShortLinkRecipients.Value ? "1" : "0";

            if (request.ShortenLinks.HasValue)
                parameters["data[shortenLinks]"] = request.ShortenLinks.Value ? "1" : "0";

            return await _apiClient.SendForIdAsync(
                ModuleName, "Create", parameters, cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteAsync(
            long id, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["id"] = ApiFormat.Int(id)
            };

            await _apiClient.SendCommandAsync(
                ModuleName, "Delete", parameters, cancellationToken).ConfigureAwait(false);
        }

        public async Task<CampaignData> GetAsync(
            long id, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["id"] = ApiFormat.Int(id)
            };

            return (await _apiClient.SendAsync<CampaignData>(
                ModuleName, "Get", parameters, cancellationToken).ConfigureAwait(false)).Data;
        }

        public async Task<CampaignInfo> GetInfoAsync(
            long id, bool? fillTemplateText = null, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["id"] = ApiFormat.Int(id)
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
                        parameters["criteria[id]"] = ApiFormat.Int(c.Id.Value);

                    if (c.Ids != null)
                        for (var i = 0; i < c.Ids.Count; i++)
                            parameters[$"criteria[ids][{i}]"] = ApiFormat.Int(c.Ids[i]);

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
                    parameters["pagination[currentPage]"] = ApiFormat.Int(request.Pagination.CurrentPage);
                    parameters["pagination[pageSize]"] = ApiFormat.Int(request.Pagination.PageSize);
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
            var parameters = new Dictionary<string, string> { ["id"] = ApiFormat.Int(id) };

            var response = await _apiClient.SendAsync<long>(
                ModuleName, "Send", parameters, cancellationToken,
                acceptedCodes: QueuedCodes).ConfigureAwait(false);

            return new CampaignSendResult
            {
                IsQueued = response.Code == MobizonResponseCode.BackgroundTask,
                Id = response.Data
            };
        }

        private const int AddRecipientsMaxBatchSize = 500;

        private static readonly int[] QueuedCodes = { (int)MobizonResponseCode.BackgroundTask };
        private static readonly int[] SyncBatchCodes =
        {
            (int)AddRecipientsOutcome.PartiallyAdded,
            (int)AddRecipientsOutcome.NoneAdded,
            (int)MobizonResponseCode.BackgroundTask
        };

        public async Task<AddRecipientsResult> AddRecipientsAsync(
            AddRecipientsRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var sources = (request.Recipients != null ? 1 : 0)
                        + (request.RecipientContacts != null ? 1 : 0)
                        + (request.RecipientGroups != null ? 1 : 0)
                        + (request.RecipientsFile != null ? 1 : 0);
            if (sources != 1)
                throw new ArgumentException(
                    "Exactly one recipient source must be set (Recipients, RecipientContacts, RecipientGroups, or RecipientsFile).",
                    nameof(request));

            // File and group loads are asynchronous: the only non-zero code they may answer with is 100.
            if (request.RecipientsFile != null)
            {
                var fields = new Dictionary<string, string> { ["id"] = ApiFormat.Int(request.CampaignId) };
                AppendParams(fields, request.Parameters);
                return Finalize(await _apiClient.SendMultipartAsync<AddRecipientsResult>(
                    ModuleName, "AddRecipients", fields, request.RecipientsFile,
                    request.RecipientsFileName ?? "recipients.csv",
                    cancellationToken, acceptedCodes: QueuedCodes, fileFieldName: "recipientsFile",
                    allowNullData: true).ConfigureAwait(false));
            }

            if (request.RecipientGroups != null)
                return Finalize(await SendAddRecipientsAsync(request, QueuedCodes, cancellationToken).ConfigureAwait(false));

            // Synchronous sources (phone numbers / contact cards) are limited to 500 per request.
            var recipients = request.Recipients;
            var contacts = request.RecipientContacts;
            var totalCount = (recipients?.Count ?? 0) + (contacts?.Count ?? 0);

            if (totalCount <= AddRecipientsMaxBatchSize)
                return Finalize(await SendAddRecipientsAsync(request, SyncBatchCodes, cancellationToken).ConfigureAwait(false));

            // Multi-batch path. Entries accumulate in one list (linear in the number of recipients) and the
            // outcome aggregates the per-batch codes: any accepted + any rejected = partial.
            var entries = new List<AddRecipientEntry>(totalCount);
            var anyAccepted = false;
            var anyRejected = false;
            var confirmedCount = 0;
            var inFlight = 0;
            var recipientOffset = 0;
            var contactOffset = 0;

            try
            {
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
                    if (confirmedCount > 0 && batchParams?.Replace == true)
                    {
                        batchParams = new AddRecipientsParameters
                        {
                            Replace = false,
                            PlaceholdersFlag = batchParams.PlaceholdersFlag,
                            RecipientsFileEncoding = batchParams.RecipientsFileEncoding,
                            RecipientsFileSkipHeader = batchParams.RecipientsFileSkipHeader,
                            RecipientsFileDelimiter = batchParams.RecipientsFileDelimiter,
                            RecipientsFileEnclosure = batchParams.RecipientsFileEnclosure
                        };
                    }

                    var batchRequest = new AddRecipientsRequest
                    {
                        CampaignId = request.CampaignId,
                        Recipients = batchRecipients,
                        RecipientContacts = batchContacts,
                        Parameters = batchParams
                    };

                    inFlight = (batchRecipients?.Count ?? 0) + (batchContacts?.Count ?? 0);
                    var response = await SendAddRecipientsAsync(batchRequest, SyncBatchCodes, cancellationToken).ConfigureAwait(false);
                    confirmedCount += inFlight;
                    inFlight = 0;

                    if (response.RawCode == (int)AddRecipientsOutcome.NoneAdded)
                        anyRejected = true;
                    else if (response.RawCode == (int)AddRecipientsOutcome.PartiallyAdded)
                        anyAccepted = anyRejected = true;
                    else
                        anyAccepted = true;

                    if (response.Data?.Entries != null)
                        entries.AddRange(response.Data.Entries);
                }
            }
            catch (Exception ex)
            {
                // Confirmed batches are not lost: the caller can inspect them and resume from ConfirmedCount.
                // The in-flight batch (if any) has an unknown outcome and must not be blindly resent.
                new AddRecipientsProgress(BuildResult(entries, anyAccepted, anyRejected), confirmedCount, inFlight).AttachTo(ex);
                throw;
            }

            return BuildResult(entries, anyAccepted, anyRejected);
        }

        private static AddRecipientsResult BuildResult(List<AddRecipientEntry> entries, bool anyAccepted, bool anyRejected) =>
            new AddRecipientsResult
            {
                Entries = entries,
                Outcome = anyAccepted && anyRejected ? AddRecipientsOutcome.PartiallyAdded
                        : anyRejected ? AddRecipientsOutcome.NoneAdded
                        : AddRecipientsOutcome.AllAdded
            };

        private static AddRecipientsResult Finalize(MobizonResponse<AddRecipientsResult> response)
        {
            var result = response.Data ?? new AddRecipientsResult();
            result.Outcome = response.RawCode == (int)AddRecipientsOutcome.PartiallyAdded ? AddRecipientsOutcome.PartiallyAdded
                           : response.RawCode == (int)AddRecipientsOutcome.NoneAdded ? AddRecipientsOutcome.NoneAdded
                           : AddRecipientsOutcome.AllAdded; // 0, or 100 (queued: see IsQueued)
            return result;
        }

        private Task<MobizonResponse<AddRecipientsResult>> SendAddRecipientsAsync(
            AddRecipientsRequest request, int[] acceptedCodes, CancellationToken cancellationToken)
        {
            var parameters = new Dictionary<string, string>
            {
                ["id"] = ApiFormat.Int(request.CampaignId)
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

            // A rejected batch (99) still describes every recipient in `data`; a null payload is tolerated.
            return _apiClient.SendAsync<AddRecipientsResult>(
                ModuleName, "AddRecipients", parameters, cancellationToken,
                acceptedCodes: acceptedCodes, allowNullData: true);
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

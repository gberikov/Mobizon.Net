using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts;

namespace Mobizon.Contracts
{
    /// <summary>
    /// Provides operations for creating and managing bulk SMS campaigns via the Mobizon API.
    /// </summary>
    public interface ICampaignService
    {
        /// <summary>
        /// Creates a new SMS campaign.
        /// </summary>
        /// <param name="request">The campaign configuration, including type, sender name, and message text.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>The integer ID of the newly created campaign.</returns>
        /// <exception cref="MobizonApiException">
        /// Thrown when the API returns a non-success response code.
        /// </exception>
        Task<long> CreateAsync(
            CreateCampaignRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a campaign by its ID.
        /// The campaign must not have started sending yet (or, if deferred, must be at least 5 minutes away).
        /// </summary>
        /// <param name="id">The ID of the campaign to delete.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <exception cref="MobizonApiException">
        /// Thrown when the API returns a non-success response code.
        /// </exception>
        Task DeleteAsync(
            long id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the full data of a campaign by its ID.
        /// </summary>
        /// <param name="id">The ID of the campaign to retrieve.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>The <see cref="CampaignData"/> for the specified campaign.</returns>
        /// <exception cref="MobizonApiException">
        /// Thrown when the API returns a non-success response code.
        /// </exception>
        Task<CampaignData> GetAsync(
            long id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the full data and delivery statistics for a campaign.
        /// </summary>
        /// <param name="id">The ID of the campaign to query.</param>
        /// <param name="fillTemplateText">
        /// For template campaigns: <see langword="true"/> (API default) returns the text filled with
        /// real recipient data; <see langword="false"/> returns the raw text with placeholders.
        /// </param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A <see cref="CampaignInfo"/> with full data and statistics.</returns>
        /// <exception cref="MobizonApiException">
        /// Thrown when the API returns a non-success response code.
        /// </exception>
        Task<CampaignInfo> GetInfoAsync(
            long id, bool? fillTemplateText = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a paginated, optionally filtered list of campaigns.
        /// </summary>
        /// <param name="request">
        /// Optional search criteria, pagination and sort parameters.
        /// Pass <see langword="null"/> to use API defaults.
        /// </param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>
        /// A <see cref="MobizonListResult{T}"/> of <see cref="CampaignData"/> items. The API documents a list
        /// item as a full <c>campaign/getInfo</c> object, so every element is in fact a <see cref="CampaignInfo"/>:
        /// cast one to read <see cref="CampaignInfo.Counters"/> or <see cref="CampaignInfo.CreationWay"/> instead
        /// of calling <see cref="GetInfoAsync"/> per campaign.
        /// </returns>
        /// <exception cref="MobizonApiException">
        /// Thrown when the API returns a non-success response code.
        /// </exception>
        Task<MobizonListResult<CampaignData>> ListAsync(
            CampaignListRequest? request = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Schedules a campaign for immediate (or deferred) sending.
        /// </summary>
        /// <param name="id">The ID of the campaign to send.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>
        /// A <see cref="CampaignSendResult"/>; when <c>IsQueued</c> is true, <c>Id</c> is the background task id
        /// trackable via <c>TaskQueue/GetStatus</c>.
        /// </returns>
        /// <exception cref="MobizonApiException">
        /// Thrown when the API returns a non-success response code.
        /// </exception>
        Task<CampaignSendResult> SendAsync(
            long id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds recipients to an existing campaign.
        /// <para>
        /// Placeholder names live in the same wire namespace as the recipient's phone number, so
        /// <c>recipient</c> is reserved and names containing <c>[</c> or <c>]</c> are rejected. Every entry is
        /// validated before the first batch is sent, so a bad name never applies to part of the list.
        /// </para>
        /// Only one recipient type (<see cref="AddRecipientsRequest.Recipients"/>,
        /// <see cref="AddRecipientsRequest.RecipientContacts"/>, or
        /// <see cref="AddRecipientsRequest.RecipientGroups"/>) may be specified per call.
        /// <para>
        /// Phone-number and contact-card loads are synchronous. If the list exceeds 500 entries
        /// the SDK automatically splits it into sequential batches of up to 500 and aggregates
        /// the results into a single response (<c>Replace</c> is honoured by the first batch only).
        /// Group and file loads are asynchronous: the API only queues them and returns a background task ID
        /// (<see cref="AddRecipientsResult.IsQueued"/>); poll <see cref="ITaskQueueService.GetStatusAsync"/>
        /// until the task finishes before sending the campaign.
        /// A group/file response must carry code 100 and a positive task ID; otherwise a
        /// <see cref="MobizonException"/> is thrown. Synchronous batches reject queued responses;
        /// any previously confirmed batches remain available through <see cref="AddRecipientsProgress.FromException"/>.
        /// </para>
        /// <para>
        /// If a later batch of a multi-batch send fails (API error, transport error or cancellation), the exception
        /// carries an <see cref="AddRecipientsProgress"/> (<see cref="AddRecipientsProgress.FromException"/>) with
        /// the confirmed batches and the size of the batch whose outcome is unknown. Do not resend the whole list
        /// after a timeout: the in-flight batch may already have been applied.
        /// </para>
        /// </summary>
        /// <param name="request">The recipient data to add, including the campaign ID.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>
        /// An <see cref="AddRecipientsResult"/> whose <see cref="AddRecipientsResult.Outcome"/> reflects the
        /// top-level result (AllAdded / PartiallyAdded / NoneAdded), and whose
        /// <see cref="AddRecipientsResult.Entries"/> contains per-recipient status entries (synchronous loads)
        /// or whose <see cref="AddRecipientsResult.TaskId"/> holds the background task ID (asynchronous loads).
        /// For multi-batch sends, <c>Outcome</c> is aggregated: any accepted batch plus any rejected batch is
        /// <see cref="AddRecipientsOutcome.PartiallyAdded"/>; <see cref="AddRecipientsOutcome.NoneAdded"/> only when
        /// every batch was rejected.
        /// </returns>
        /// <exception cref="MobizonApiException">
        /// Thrown when the API returns a non-success response code.
        /// </exception>
        Task<AddRecipientsResult> AddRecipientsAsync(
            AddRecipientsRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the short links used by a campaign, with their click counters.
        /// Same endpoint as <see cref="ILinkService.GetLinksAsync"/> (<c>link/getLinks</c>), exposed here for discoverability.
        /// </summary>
        /// <param name="campaignId">The campaign ID.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>The campaign's <see cref="LinkData"/> items (bare array — not a paged envelope).</returns>
        /// <exception cref="MobizonApiException">Thrown when the API returns a non-success response code.</exception>
        Task<IReadOnlyList<LinkData>> GetLinksAsync(long campaignId, CancellationToken cancellationToken = default);
    }
}

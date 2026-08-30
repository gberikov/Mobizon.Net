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
        /// <returns>A <see cref="MobizonListResult{T}"/> of <see cref="CampaignData"/> items.</returns>
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
        /// Only one recipient type (<see cref="AddRecipientsRequest.Recipients"/>,
        /// <see cref="AddRecipientsRequest.RecipientContacts"/>, or
        /// <see cref="AddRecipientsRequest.RecipientGroups"/>) may be specified per call.
        /// <para>
        /// Phone-number and contact-card loads are synchronous. If the list exceeds 500 entries
        /// the SDK automatically splits it into sequential batches of up to 500 and aggregates
        /// the results into a single response. Group and file loads are asynchronous and return
        /// a background task ID.
        /// </para>
        /// </summary>
        /// <param name="request">The recipient data to add, including the campaign ID.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>
        /// An <see cref="AddRecipientsResult"/> whose <see cref="AddRecipientsResult.Outcome"/> reflects the
        /// top-level result (AllAdded / PartiallyAdded / NoneAdded), and whose
        /// <see cref="AddRecipientsResult.Entries"/> contains per-recipient status entries (synchronous loads)
        /// or whose <see cref="AddRecipientsResult.TaskId"/> holds the background task ID (asynchronous loads).
        /// For multi-batch sends, <c>Outcome</c> reflects the worst-case outcome across all batches.
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

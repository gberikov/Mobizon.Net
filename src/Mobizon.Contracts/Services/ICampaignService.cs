using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts.Models;
using Mobizon.Contracts.Models.Campaign;

namespace Mobizon.Contracts.Services
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
        /// <returns>
        /// A <see cref="MobizonResponse{T}"/> containing a <see cref="CreateCampaignResult"/> with the new campaign ID.
        /// </returns>
        /// <exception cref="Exceptions.MobizonApiException">
        /// Thrown when the API returns a non-success response code.
        /// </exception>
        Task<MobizonResponse<CreateCampaignResult>> CreateAsync(
            CreateCampaignRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a campaign by its ID.
        /// The campaign must not have started sending yet (or, if deferred, must be at least 5 minutes away).
        /// </summary>
        /// <param name="id">The ID of the campaign to delete.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A <see cref="MobizonResponse{T}"/> confirming the deletion.</returns>
        /// <exception cref="Exceptions.MobizonApiException">
        /// Thrown when the API returns a non-success response code.
        /// </exception>
        Task<MobizonResponse<object>> DeleteAsync(
            int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the full data of a campaign by its ID.
        /// </summary>
        /// <param name="id">The ID of the campaign to retrieve.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>
        /// A <see cref="MobizonResponse{T}"/> containing the <see cref="CampaignData"/> for the specified campaign.
        /// </returns>
        /// <exception cref="Exceptions.MobizonApiException">
        /// Thrown when the API returns a non-success response code.
        /// </exception>
        Task<MobizonResponse<CampaignData>> GetAsync(
            int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the full data and delivery statistics for a campaign.
        /// </summary>
        /// <param name="id">The ID of the campaign to query.</param>
        /// <param name="getFilledTplCampaignText">
        /// Controls the format of the returned campaign text for template campaigns:
        /// <c>0</c> — return the text with placeholders;
        /// <c>1</c> — return the text filled with real recipient data (default).
        /// </param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>
        /// A <see cref="MobizonResponse{T}"/> containing a <see cref="CampaignInfo"/> with full data and statistics.
        /// </returns>
        /// <exception cref="Exceptions.MobizonApiException">
        /// Thrown when the API returns a non-success response code.
        /// </exception>
        Task<MobizonResponse<CampaignInfo>> GetInfoAsync(
            int id, int? getFilledTplCampaignText = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a paginated, optionally filtered list of campaigns.
        /// </summary>
        /// <param name="request">
        /// Optional search criteria, pagination and sort parameters.
        /// Pass <see langword="null"/> to use API defaults.
        /// </param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>
        /// A <see cref="MobizonResponse{T}"/> containing a list of <see cref="CampaignData"/> items.
        /// </returns>
        /// <exception cref="Exceptions.MobizonApiException">
        /// Thrown when the API returns a non-success response code.
        /// </exception>
        Task<MobizonResponse<IReadOnlyList<CampaignData>>> ListAsync(
            CampaignListRequest? request = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Schedules a campaign for immediate (or deferred) sending.
        /// </summary>
        /// <param name="id">The ID of the campaign to send.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>
        /// A <see cref="MobizonResponse{T}"/> containing a <see cref="CampaignSendResult"/> with the
        /// resulting send status.
        /// </returns>
        /// <exception cref="Exceptions.MobizonApiException">
        /// Thrown when the API returns a non-success response code.
        /// </exception>
        Task<MobizonResponse<CampaignSendResult>> SendAsync(
            int id, CancellationToken cancellationToken = default);

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
        /// A <see cref="MobizonResponse{T}"/> containing an <see cref="AddRecipientsResult"/>
        /// with per-recipient status entries (synchronous) or a background task ID (asynchronous).
        /// When multiple batches are sent, the response aggregates all entries and reflects the
        /// worst-case response code across batches.
        /// </returns>
        /// <exception cref="Exceptions.MobizonApiException">
        /// Thrown when the API returns a non-success response code.
        /// </exception>
        Task<MobizonResponse<AddRecipientsResult>> AddRecipientsAsync(
            AddRecipientsRequest request, CancellationToken cancellationToken = default);
    }
}


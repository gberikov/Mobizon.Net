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
        /// Retrieves delivery statistics (sent, delivered, failed counts) for a campaign.
        /// </summary>
        /// <param name="id">The ID of the campaign to query.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>
        /// A <see cref="MobizonResponse{T}"/> containing a <see cref="CampaignInfo"/> with delivery statistics.
        /// </returns>
        /// <exception cref="Exceptions.MobizonApiException">
        /// Thrown when the API returns a non-success response code.
        /// </exception>
        Task<MobizonResponse<CampaignInfo>> GetInfoAsync(
            int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a paginated list of campaigns.
        /// </summary>
        /// <param name="request">Optional pagination and sort criteria. Pass <see langword="null"/> to use API defaults.</param>
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
        /// Schedules a campaign for immediate sending.
        /// </summary>
        /// <param name="id">The ID of the campaign to send.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>
        /// A <see cref="MobizonResponse{T}"/> containing a <see cref="CampaignSendResult"/> with the
        /// resulting status and optional background task ID.
        /// </returns>
        /// <exception cref="Exceptions.MobizonApiException">
        /// Thrown when the API returns a non-success response code.
        /// </exception>
        Task<MobizonResponse<CampaignSendResult>> SendAsync(
            int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds one or more recipients to an existing campaign.
        /// </summary>
        /// <param name="request">The recipient data to add, including the campaign ID and phone numbers.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A <see cref="MobizonResponse{T}"/> confirming the operation.</returns>
        /// <exception cref="Exceptions.MobizonApiException">
        /// Thrown when the API returns a non-success response code.
        /// </exception>
        Task<MobizonResponse<object>> AddRecipientsAsync(
            AddRecipientsRequest request, CancellationToken cancellationToken = default);
    }
}

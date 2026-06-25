using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts.Models.Common;
using Mobizon.Contracts.Models.Links;

namespace Mobizon.Contracts.Services
{
    /// <summary>
    /// Provides operations for creating and managing Mobizon short links.
    /// </summary>
    public interface ILinkService
    {
        /// <summary>
        /// Creates a new short link for the given full URL.
        /// </summary>
        /// <param name="request">The details of the link to create, including the target URL and optional settings.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>
        /// A <see cref="MobizonResponse{T}"/> containing the created <see cref="LinkData"/>.
        /// </returns>
        /// <exception cref="Exceptions.MobizonApiException">
        /// Thrown when the API returns a non-success response code.
        /// </exception>
        Task<MobizonResponse<LinkData>> CreateAsync(
            CreateLinkRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes one or more short links by their IDs.
        /// </summary>
        /// <param name="ids">An array of link IDs to delete.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A <see cref="MobizonResponse{T}"/> confirming the deletion.</returns>
        /// <exception cref="Exceptions.MobizonApiException">
        /// Thrown when the API returns a non-success response code.
        /// </exception>
        Task<MobizonResponse<object>> DeleteAsync(
            long[] ids, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a short link by its numeric ID.
        /// </summary>
        /// <param name="id">The numeric ID that identifies the link.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>
        /// A <see cref="MobizonResponse{T}"/> containing the matching <see cref="LinkData"/>.
        /// </returns>
        /// <exception cref="Exceptions.MobizonApiException">
        /// Thrown when the API returns a non-success response code.
        /// </exception>
        Task<MobizonResponse<LinkData>> GetByIdAsync(
            long id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a short link by its unique short code.
        /// </summary>
        /// <param name="code">The short code that identifies the link.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>
        /// A <see cref="MobizonResponse{T}"/> containing the matching <see cref="LinkData"/>.
        /// </returns>
        /// <exception cref="Exceptions.MobizonApiException">
        /// Thrown when the API returns a non-success response code.
        /// </exception>
        Task<MobizonResponse<LinkData>> GetByCodeAsync(
            string code, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a short link by its full short URL.
        /// </summary>
        /// <param name="shortLink">The full short URL (e.g. <c>https://mbzn.co/x</c>) that identifies the link.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>
        /// A <see cref="MobizonResponse{T}"/> containing the matching <see cref="LinkData"/>.
        /// </returns>
        /// <exception cref="Exceptions.MobizonApiException">
        /// Thrown when the API returns a non-success response code.
        /// </exception>
        Task<MobizonResponse<LinkData>> GetByShortLinkAsync(
            string shortLink, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all short links associated with a specific campaign.
        /// </summary>
        /// <param name="campaignId">The ID of the campaign whose links should be retrieved.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>
        /// A <see cref="MobizonResponse{T}"/> containing the list of <see cref="LinkData"/> items for the campaign.
        /// Note: The API returns a bare array here (not a paged <see cref="MobizonListResult{T}"/> envelope),
        /// so the return type is intentionally <see cref="IReadOnlyList{T}"/>.
        /// </returns>
        /// <exception cref="Exceptions.MobizonApiException">
        /// Thrown when the API returns a non-success response code.
        /// </exception>
        Task<MobizonResponse<IReadOnlyList<LinkData>>> GetLinksAsync(
            long campaignId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves click statistics for one or more short links over a date range.
        /// </summary>
        /// <param name="request">The request specifying link IDs, aggregation type, and optional date range.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>
        /// A <see cref="MobizonResponse{T}"/> containing a <see cref="LinkStatsResult"/> with one
        /// <see cref="LinkStatSeries"/> per requested link — each carrying its resolved
        /// <see cref="LinkStatSeries.LinkId"/>, aggregate totals, and per-period
        /// <see cref="LinkStatPoint"/> data points.
        /// </returns>
        /// <exception cref="Exceptions.MobizonApiException">
        /// Thrown when the API returns a non-success response code.
        /// </exception>
        Task<MobizonResponse<LinkStatsResult>> GetStatsAsync(
            GetLinkStatsRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a paginated list of all short links in the account.
        /// </summary>
        /// <param name="request">Optional pagination and sort criteria. Pass <see langword="null"/> to use API defaults.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>
        /// A <see cref="MobizonResponse{T}"/> containing a paged envelope of <see cref="LinkData"/> items.
        /// </returns>
        /// <exception cref="Exceptions.MobizonApiException">
        /// Thrown when the API returns a non-success response code.
        /// </exception>
        Task<MobizonResponse<MobizonListResult<LinkData>>> ListAsync(
            LinkListRequest? request = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the properties of an existing short link.
        /// </summary>
        /// <param name="request">The updated link data, identified by numeric <see cref="UpdateLinkRequest.Id"/>.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A <see cref="MobizonResponse{T}"/> confirming the update.</returns>
        /// <exception cref="Exceptions.MobizonApiException">
        /// Thrown when the API returns a non-success response code.
        /// </exception>
        Task<MobizonResponse<object>> UpdateAsync(
            UpdateLinkRequest request, CancellationToken cancellationToken = default);
    }
}

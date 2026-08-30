using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts;

namespace Mobizon.Contracts
{
    /// <summary>
    /// Provides operations for managing contact groups via the Mobizon API.
    /// </summary>
    public interface IContactGroupService
    {
        /// <summary>
        /// Returns a paginated list of contact groups.
        /// </summary>
        /// <returns>The list response containing contact group items and pagination info.</returns>
        Task<ContactGroupListResponse> ListAsync(
            ContactGroupListRequest? request = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new contact group and returns its ID.
        /// </summary>
        /// <returns>The ID of the newly created contact group.</returns>
        Task<long> CreateAsync(
            string name,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Renames an existing contact group.
        /// </summary>
        /// <returns>A task that completes when the group has been renamed.</returns>
        Task UpdateAsync(
            long id,
            string name,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a contact group. Returns the lists of processed and not-processed IDs.
        /// </summary>
        /// <returns>A <see cref="DeleteResult"/> containing the processed and not-processed ID lists.</returns>
        Task<DeleteResult> DeleteAsync(
            long id,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the number of contact cards in the specified group.
        /// Pass <c>"-1"</c> to count contacts that have no group.
        /// </summary>
        /// <returns>The number of contact cards in the group.</returns>
        Task<long> GetCardsCountAsync(
            long? id = null,
            CancellationToken cancellationToken = default);
    }
}

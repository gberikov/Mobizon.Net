using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts.Models.Common;
using Mobizon.Contracts.Models.ContactGroups;

namespace Mobizon.Contracts.Services
{
    /// <summary>
    /// Provides operations for managing contact groups via the Mobizon API.
    /// </summary>
    public interface IContactGroupService
    {
        /// <summary>
        /// Returns a paginated list of contact groups.
        /// </summary>
        Task<MobizonResponse<ContactGroupListResponse>> ListAsync(
            ContactGroupListRequest? request = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new contact group and returns its ID.
        /// </summary>
        Task<MobizonResponse<int>> CreateAsync(
            string name,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Renames an existing contact group.
        /// </summary>
        Task<MobizonResponse<bool>> UpdateAsync(
            int id,
            string name,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a contact group. Returns the lists of processed and not-processed IDs.
        /// </summary>
        Task<MobizonResponse<DeleteContactGroupResult>> DeleteAsync(
            int id,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the number of contact cards in the specified group.
        /// Pass <c>"-1"</c> to count contacts that have no group.
        /// </summary>
        Task<MobizonResponse<int>> GetCardsCountAsync(
            int? id = null,
            CancellationToken cancellationToken = default);
    }
}

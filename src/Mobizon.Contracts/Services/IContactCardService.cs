using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts.Models;
using Mobizon.Contracts.Models.ContactCard;

namespace Mobizon.Contracts.Services
{
    /// <summary>
    /// Provides operations for managing contact cards via the Mobizon API.
    /// </summary>
    public interface IContactCardService
    {
        /// <summary>
        /// Returns a paginated list of contact cards, optionally filtered and sorted.
        /// </summary>
        Task<MobizonResponse<ContactCardListResponse>> ListAsync(
            ContactCardListRequest? request = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the full data of a single contact card by ID.
        /// </summary>
        Task<MobizonResponse<ContactCardData>> GetAsync(
            string id,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new contact card and returns its ID.
        /// </summary>
        Task<MobizonResponse<string>> CreateAsync(
            CreateContactCardRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing contact card.
        /// </summary>
        Task<MobizonResponse<bool>> UpdateAsync(
            UpdateContactCardRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets the group membership of a contact card, replacing any existing groups.
        /// </summary>
        Task<MobizonResponse<bool>> SetGroupsAsync(
            string id,
            IReadOnlyList<string> groupIds,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the groups that the specified contact card belongs to.
        /// </summary>
        Task<MobizonResponse<IReadOnlyList<ContactGroupRef>>> GetGroupsAsync(
            string id,
            CancellationToken cancellationToken = default);
    }
}

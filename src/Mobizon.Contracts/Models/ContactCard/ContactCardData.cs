using System.Collections.Generic;

namespace Mobizon.Contracts.Models.ContactCard
{
    /// <summary>
    /// Represents a contact card as returned by <c>contactcard/list</c> and <c>contactcard/get</c>.
    /// </summary>
    public class ContactCardData
    {
        /// <summary>Gets or sets the unique ID of the contact card.</summary>
        public string? Id { get; set; }

        /// <summary>Gets or sets the owner user ID.</summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Gets or sets whether the card has been deleted.
        /// <c>0</c> — active; <c>1</c> — deleted.
        /// </summary>
        public int IsDeleted { get; set; }

        /// <summary>
        /// Gets or sets whether the contact is available for sending.
        /// <c>1</c> — available; <c>0</c> — opted out or blocked.
        /// </summary>
        public int IsAvailable { get; set; }

        /// <summary>Gets or sets the field values of the contact card.</summary>
        public ContactCardFields? Fields { get; set; }

        /// <summary>Gets or sets the groups this contact belongs to.</summary>
        public IReadOnlyList<ContactGroupRef>? Groups { get; set; }
    }
}

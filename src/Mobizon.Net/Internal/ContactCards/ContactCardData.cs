using System.Collections.Generic;
using Mobizon.Contracts;

namespace Mobizon.Net.Internal
{
    /// <summary>
    /// Represents a contact card as returned by <c>contactcard/list</c> and <c>contactcard/get</c>.
    /// </summary>
    internal class ContactCardData
    {
        /// <summary>Gets or sets the unique ID of the contact card.</summary>
        public long? Id { get; set; }

        /// <summary>Gets or sets the owner user ID.</summary>
        public long? UserId { get; set; }

        /// <summary>Gets or sets whether the card has been deleted.</summary>
        public bool IsDeleted { get; set; }

        /// <summary>Gets or sets whether the contact is available for sending.</summary>
        public bool IsAvailable { get; set; }

        /// <summary>Gets or sets the field values of the contact card.</summary>
        public ContactCardFields? Fields { get; set; }

        /// <summary>Gets or sets the groups this contact belongs to.</summary>
        public IReadOnlyList<ContactGroupRef>? Groups { get; set; }
    }
}

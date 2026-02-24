namespace Mobizon.Contracts.Models.ContactGroup
{
    /// <summary>
    /// Represents a contact group as returned by <c>contactgroup/list</c>.
    /// </summary>
    public class ContactGroupData
    {
        /// <summary>Gets or sets the unique ID of the group.</summary>
        public string? Id { get; set; }

        /// <summary>Gets or sets the owner user ID.</summary>
        public string? UserId { get; set; }

        /// <summary>Gets or sets the display name of the group.</summary>
        public string? Name { get; set; }

        /// <summary>Gets or sets the number of contact cards in the group.</summary>
        public int CardsCnt { get; set; }

        /// <summary>Gets or sets whether the group is hidden in the UI.</summary>
        public int IsHidden { get; set; }

        /// <summary>Gets or sets the creation timestamp. Format: <c>YYYY-MM-DD HH:MM:SS</c>.</summary>
        public string? CreateTs { get; set; }
    }
}

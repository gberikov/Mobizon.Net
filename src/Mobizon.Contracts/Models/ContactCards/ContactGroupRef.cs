namespace Mobizon.Contracts.Models.ContactCards
{
    /// <summary>
    /// A lightweight reference to a contact group, embedded in contact card responses.
    /// </summary>
    public class ContactGroupRef
    {
        /// <summary>Gets or sets the group ID.</summary>
        public string? Id { get; set; }

        /// <summary>Gets or sets the group name.</summary>
        public string? Name { get; set; }
    }
}

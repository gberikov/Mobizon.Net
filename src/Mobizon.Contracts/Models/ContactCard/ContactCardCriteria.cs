namespace Mobizon.Contracts.Models.ContactCard
{
    /// <summary>
    /// Defines a single filter criterion for <c>contactcard/list</c>.
    /// </summary>
    public class ContactCardCriteria
    {
        /// <summary>
        /// Gets or sets the field name to filter by (e.g. <c>"groupId"</c>).
        /// </summary>
        public string Field { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the filter operator: <c>"equal"</c> or <c>"empty"</c>.
        /// </summary>
        public string Operator { get; set; } = "equal";

        /// <summary>
        /// Gets or sets the filter value (e.g. a group ID, or <c>"-1"</c> for contacts without a group).
        /// </summary>
        public string Value { get; set; } = string.Empty;
    }
}

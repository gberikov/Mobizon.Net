namespace Mobizon.Contracts.Models.ContactCards
{
    /// <summary>
    /// Represents the gender of a contact.
    /// When used as a filter criterion, <see cref="Undefined"/> maps to the <c>empty</c> API operator
    /// (matches contacts where gender is not set).
    /// </summary>
    public enum Gender
    {
        /// <summary>Gender is not specified. Maps to the <c>empty</c> filter operator.</summary>
        Undefined = 0,

        /// <summary>Male.</summary>
        Male,

        /// <summary>Female.</summary>
        Female
    }
}

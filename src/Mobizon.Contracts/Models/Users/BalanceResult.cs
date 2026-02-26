namespace Mobizon.Contracts.Models.Users
{
    /// <summary>
    /// Contains the account balance information returned by the Mobizon API.
    /// </summary>
    public class BalanceResult
    {
        /// <summary>
        /// Gets or sets the current account balance as a string (e.g. <c>"12.50"</c>).
        /// </summary>
        public string Balance { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ISO 4217 currency code for the balance (e.g. <c>"USD"</c>).
        /// </summary>
        public string Currency { get; set; } = string.Empty;
    }
}

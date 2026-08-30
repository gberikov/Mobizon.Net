namespace Mobizon.Contracts
{
    /// <summary>
    /// Contains the account balance information returned by the Mobizon API.
    /// </summary>
    public class BalanceResult
    {
        /// <summary>
        /// Gets or sets the current balance with 4 decimal places, in <see cref="Currency"/>.
        /// </summary>
        public decimal Balance { get; set; }

        /// <summary>
        /// Gets or sets the ISO 4217 currency code for the balance (e.g. <c>"USD"</c>).
        /// </summary>
        public string Currency { get; set; } = string.Empty;
    }
}

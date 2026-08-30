using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts;

namespace Mobizon.Contracts
{
    /// <summary>
    /// Provides operations for querying account information from the Mobizon API.
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Retrieves the current balance of the authenticated account.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>
        /// A <see cref="BalanceResult"/> with the account balance amount and currency code.
        /// Throws <see cref="MobizonApiException"/> on failure; success is implied by no exception.
        /// </returns>
        /// <exception cref="MobizonApiException">
        /// Thrown when the API returns a non-success response code.
        /// </exception>
        /// <example>
        /// <code>
        /// var balance = await client.User.GetOwnBalanceAsync();
        /// Console.WriteLine($"Balance: {balance.Balance} {balance.Currency}");
        /// </code>
        /// </example>
        Task<BalanceResult> GetOwnBalanceAsync(
            CancellationToken cancellationToken = default);
    }
}

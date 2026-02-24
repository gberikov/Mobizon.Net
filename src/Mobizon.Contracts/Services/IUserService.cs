using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts.Models;
using Mobizon.Contracts.Models.User;

namespace Mobizon.Contracts.Services
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
        /// A <see cref="MobizonResponse{T}"/> containing a <see cref="BalanceResult"/> with
        /// the account balance amount and currency code.
        /// </returns>
        /// <exception cref="Exceptions.MobizonApiException">
        /// Thrown when the API returns a non-success response code.
        /// </exception>
        /// <example>
        /// <code>
        /// var response = await client.User.GetOwnBalanceAsync();
        ///
        /// if (response.Code == MobizonResponseCode.Success)
        ///     Console.WriteLine($"Balance: {response.Data.Balance} {response.Data.Currency}");
        /// </code>
        /// </example>
        Task<MobizonResponse<BalanceResult>> GetOwnBalanceAsync(
            CancellationToken cancellationToken = default);
    }
}

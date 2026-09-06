using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts;
using Mobizon.Net.Internal;

namespace Mobizon.Net.Services
{
    internal class UserService(MobizonApiClient apiClient) : IUserService
    {
        private const string ModuleName = "user";

        public async Task<BalanceResult> GetOwnBalanceAsync(
            CancellationToken cancellationToken = default)
        {
            return (await apiClient.SendAsync<BalanceResult>(ModuleName, "getownbalance", null, cancellationToken).ConfigureAwait(false)).Data!;
        }
    }
}

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts;
using Mobizon.Net.Internal;

namespace Mobizon.Net.Services
{
    internal class UserService : IUserService
    {
        private const string ModuleName = "user";
        private readonly MobizonApiClient _apiClient;

        public UserService(MobizonApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<BalanceResult> GetOwnBalanceAsync(
            CancellationToken cancellationToken = default)
        {
            return (await _apiClient.SendAsync<BalanceResult>(ModuleName, "getownbalance", null, cancellationToken).ConfigureAwait(false)).Data!;
        }
    }
}

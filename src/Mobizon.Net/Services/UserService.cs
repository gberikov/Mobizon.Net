using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts.Models.Common;
using Mobizon.Contracts.Models.Users;
using Mobizon.Contracts.Services;
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

        public Task<MobizonResponse<BalanceResult>> GetOwnBalanceAsync(
            CancellationToken cancellationToken = default)
        {
            return _apiClient.SendAsync<BalanceResult>(HttpMethod.Get, ModuleName, "getownbalance", null, cancellationToken);
        }
    }
}

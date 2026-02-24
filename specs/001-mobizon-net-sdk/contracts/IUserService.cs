// Contract definition — target: src/Mobizon.Contracts/Services/IUserService.cs
using Mobizon.Contracts.Models;
using Mobizon.Contracts.Models.User;

namespace Mobizon.Contracts.Services;

public interface IUserService
{
    Task<MobizonResponse<BalanceResult>> GetOwnBalanceAsync(
        CancellationToken cancellationToken = default);
}

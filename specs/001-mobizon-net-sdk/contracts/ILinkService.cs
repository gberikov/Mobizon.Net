// Contract definition — target: src/Mobizon.Contracts/Services/ILinkService.cs
using Mobizon.Contracts.Models;
using Mobizon.Contracts.Models.Link;

namespace Mobizon.Contracts.Services;

public interface ILinkService
{
    Task<MobizonResponse<LinkData>> CreateAsync(
        CreateLinkRequest request,
        CancellationToken cancellationToken = default);

    Task<MobizonResponse<object>> DeleteAsync(
        int[] ids,
        CancellationToken cancellationToken = default);

    Task<MobizonResponse<LinkData>> GetAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<MobizonResponse<IReadOnlyList<LinkData>>> GetLinksAsync(
        int campaignId,
        CancellationToken cancellationToken = default);

    Task<MobizonResponse<IReadOnlyList<LinkStatsResult>>> GetStatsAsync(
        GetLinkStatsRequest request,
        CancellationToken cancellationToken = default);

    Task<MobizonResponse<PaginatedResponse<LinkData>>> ListAsync(
        LinkListRequest request,
        CancellationToken cancellationToken = default);

    Task<MobizonResponse<object>> UpdateAsync(
        UpdateLinkRequest request,
        CancellationToken cancellationToken = default);
}

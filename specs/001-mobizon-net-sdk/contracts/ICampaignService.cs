// Contract definition — target: src/Mobizon.Contracts/Services/ICampaignService.cs
using Mobizon.Contracts.Models;
using Mobizon.Contracts.Models.Campaign;

namespace Mobizon.Contracts.Services;

public interface ICampaignService
{
    Task<MobizonResponse<CreateCampaignResult>> CreateAsync(
        CreateCampaignRequest request,
        CancellationToken cancellationToken = default);

    Task<MobizonResponse<object>> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<MobizonResponse<CampaignData>> GetAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<MobizonResponse<CampaignInfo>> GetInfoAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<MobizonResponse<PaginatedResponse<CampaignData>>> ListAsync(
        CampaignListRequest request,
        CancellationToken cancellationToken = default);

    Task<MobizonResponse<CampaignSendResult>> SendAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<MobizonResponse<object>> AddRecipientsAsync(
        AddRecipientsRequest request,
        CancellationToken cancellationToken = default);
}

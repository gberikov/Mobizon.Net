// Contract definition — target: src/Mobizon.Contracts/Services/IMessageService.cs
using Mobizon.Contracts.Models;
using Mobizon.Contracts.Models.Message;

namespace Mobizon.Contracts.Services;

public interface IMessageService
{
    Task<MobizonResponse<SendSmsResult>> SendSmsMessageAsync(
        SendSmsMessageRequest request,
        CancellationToken cancellationToken = default);

    Task<MobizonResponse<IReadOnlyList<SmsStatusResult>>> GetSmsStatusAsync(
        int[] ids,
        CancellationToken cancellationToken = default);

    Task<MobizonResponse<PaginatedResponse<MessageInfo>>> ListAsync(
        MessageListRequest request,
        CancellationToken cancellationToken = default);
}

// Contract definition — target: src/Mobizon.Contracts/IMobizonClient.cs
namespace Mobizon.Contracts;

public interface IMobizonClient
{
    IMessageService Messages { get; }
    ICampaignService Campaigns { get; }
    ILinkService Links { get; }
    IUserService User { get; }
    ITaskQueueService TaskQueue { get; }
}

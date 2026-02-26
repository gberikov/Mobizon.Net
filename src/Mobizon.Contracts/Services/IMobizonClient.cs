using System;

namespace Mobizon.Contracts.Services
{
    /// <summary>
    /// Represents the top-level Mobizon API client that provides access to all service groups.
    /// </summary>
    public interface IMobizonClient : IDisposable
    {
        /// <summary>
        /// Gets the service for sending and managing SMS messages.
        /// </summary>
        IMessageService Messages { get; }

        /// <summary>
        /// Gets the service for creating and managing bulk SMS campaigns.
        /// </summary>
        ICampaignService Campaigns { get; }

        /// <summary>
        /// Gets the service for creating and managing short links.
        /// </summary>
        ILinkService Links { get; }

        /// <summary>
        /// Gets the service for retrieving account information such as balance.
        /// </summary>
        IUserService User { get; }

        /// <summary>
        /// Gets the service for querying the status of background tasks.
        /// </summary>
        ITaskQueueService TaskQueue { get; }

        /// <summary>
        /// Gets the service for managing contact groups.
        /// </summary>
        IContactGroupService ContactGroups { get; }

        /// <summary>
        /// Gets the set for managing contact cards.
        /// </summary>
        IContactCardSet ContactCards { get; }

        /// <summary>
        /// Gets the service for managing the number stop-list.
        /// </summary>
        INumberStopListService NumberStopList { get; }
    }
}

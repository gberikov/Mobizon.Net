using System.Collections.Generic;

namespace Mobizon.Contracts.Models.Campaign
{
    /// <summary>
    /// Represents the parameters required to add recipients to an existing campaign.
    /// </summary>
    public class AddRecipientsRequest
    {
        /// <summary>
        /// Gets or sets the ID of the campaign to which the recipients will be added.
        /// </summary>
        public int CampaignId { get; set; }

        /// <summary>
        /// Gets or sets the recipient data type code as defined by the Mobizon API
        /// (e.g. phone numbers, contact group IDs).
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// Gets or sets the list of recipient values (e.g. phone numbers) to add to the campaign.
        /// </summary>
        public IReadOnlyList<string> Data { get; set; } = new List<string>();
    }
}

using Mobizon.Contracts.Models.Campaigns;
using Xunit;

namespace Mobizon.Net.Tests.Models
{
    public class CampaignSendResultTests
    {
        [Fact]
        public void CampaignSendResult_DefaultsToNotQueued()
        {
            var r = new CampaignSendResult();
            Assert.False(r.IsQueued);
            Assert.Equal(0L, r.Id);
        }

        [Fact]
        public void AddRecipientsResult_DefaultsToAllAdded()
        {
            var r = new AddRecipientsResult();
            Assert.Equal(AddRecipientsOutcome.AllAdded, r.Outcome);
        }
    }
}

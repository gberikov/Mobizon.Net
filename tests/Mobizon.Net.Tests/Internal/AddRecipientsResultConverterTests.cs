using System.Net.Http;
using System.Threading.Tasks;
using Mobizon.Contracts.Models.Campaigns;
using Mobizon.Contracts.Models.Common;
using Mobizon.Net.Internal;
using Mobizon.Net.Services;
using RichardSzalay.MockHttp;
using Xunit;

namespace Mobizon.Net.Tests.Internal
{
    public class AddRecipientsResultConverterTests
    {
        private readonly MobizonClientOptions _options = new MobizonClientOptions
        { ApiKey = "k", ApiUrl = "https://api.mobizon.kz" };
        private CampaignService Svc(MockHttpMessageHandler m) => new CampaignService(new MobizonApiClient(m.ToHttpClient(), _options));

        [Fact]
        public async Task SyncArray_PopulatesEntries()
        {
            var m = new MockHttpMessageHandler();
            m.When(HttpMethod.Post, "https://api.mobizon.kz/service/Campaign/AddRecipients")
                .Respond("application/json",
                    @"{""code"":0,""data"":[{""recipient"":""77001112233"",""code"":0,""messageId"":""42"",""type"":""number"",""number"":""77001112233""}],""message"":""""}");
            var r = await Svc(m).AddRecipientsAsync(new AddRecipientsRequest { CampaignId = 1, Recipients = new[] { new RecipientEntry { Recipient = "77001112233" } } });
            Assert.NotNull(r.Entries);
            Assert.Single(r.Entries!);
            Assert.Equal(0, r.Entries![0].Code);
            Assert.Equal("number", r.Entries![0].Type);
            Assert.Null(r.TaskId);
        }

        [Fact]
        public async Task AsyncScalar_PopulatesTaskId()
        {
            var m = new MockHttpMessageHandler();
            m.When(HttpMethod.Post, "https://api.mobizon.kz/service/Campaign/AddRecipients")
                .Respond("application/json", @"{""code"":100,""data"":777,""message"":""""}");
            var r = await Svc(m).AddRecipientsAsync(new AddRecipientsRequest { CampaignId = 1, RecipientGroups = new[] { "9" } });
            Assert.Equal(777, r.TaskId);
            Assert.Null(r.Entries);
            Assert.Equal(AddRecipientsOutcome.AllAdded, r.Outcome);
        }

        [Fact]
        public async Task AsyncScalar_LargeTaskId_PopulatesTaskId()
        {
            var m = new MockHttpMessageHandler();
            m.When(HttpMethod.Post, "https://api.mobizon.kz/service/Campaign/AddRecipients")
                .Respond("application/json", @"{""code"":100,""data"":70000000004,""message"":""""}");
            var r = await Svc(m).AddRecipientsAsync(new AddRecipientsRequest { CampaignId = 1, RecipientGroups = new[] { "9" } });
            Assert.Equal(70000000004L, r.TaskId);
            Assert.Null(r.Entries);
            Assert.Equal(AddRecipientsOutcome.AllAdded, r.Outcome);
        }
    }
}

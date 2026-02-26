using System;
using System.Threading.Tasks;
using Mobizon.Contracts.Models.Common;
using Mobizon.Contracts.Models.Campaigns;
using Mobizon.Net;

namespace Mobizon.Net.ConsoleSample.Samples
{
    static class CampaignSamples
    {
        // POST /service/Campaign/List
        public static async Task ListAsync(MobizonClient client)
        {
            Console.WriteLine("=== Campaign.List ===");
            var result = await client.Campaigns.ListAsync(new CampaignListRequest
            {
                Pagination = new PaginationRequest { CurrentPage = 0, PageSize = 10 },
                Sort       = new SortRequest { Field = "id", Direction = SortDirection.DESC }
            });
            foreach (var c in result.Data)
                Console.WriteLine($"  Id={c.Id}  Name={c.Name}  Status={c.CommonStatus}");
        }

        // POST /service/Campaign/Get
        public static async Task GetAsync(MobizonClient client)
        {
            Console.WriteLine("=== Campaign.Get ===");
            // Replace with a real campaign ID
            var result = await client.Campaigns.GetAsync(1);
            Console.WriteLine($"Id    : {result.Data.Id}");
            Console.WriteLine($"Name  : {result.Data.Name}");
            Console.WriteLine($"Status: {result.Data.CommonStatus}");
            Console.WriteLine($"Text  : {result.Data.Text}");
        }

        // POST /service/Campaign/GetInfo
        public static async Task GetInfoAsync(MobizonClient client)
        {
            Console.WriteLine("=== Campaign.GetInfo ===");
            // Replace with a real campaign ID
            var result = await client.Campaigns.GetInfoAsync(1);
            Console.WriteLine($"Id      : {result.Data.Id}");
            Console.WriteLine($"Sent    : {result.Data.Counters?.TotalDelivrdMsgNum}");
            Console.WriteLine($"Failed  : {result.Data.Counters?.TotalUndelivMsgNum}");
            Console.WriteLine($"Total   : {result.Data.Counters?.TotalMsgNum}");
        }

        // POST /service/Campaign/Create  →  /service/Campaign/Send  →  /service/Campaign/Delete
        public static async Task CreateSendDeleteAsync(MobizonClient client)
        {
            Console.WriteLine("=== Campaign.Create + Send + Delete ===");

            var createResult = await client.Campaigns.CreateAsync(new CreateCampaignRequest
            {
                Name = "SDK Test Campaign",
                Text = "Hello from Mobizon.Net SDK!",
                Type = CampaignType.Bulk,
            });
            var id = createResult.Data.CampaignId;
            Console.WriteLine($"Created Id: {id}");

            // var sendResult = await client.Campaigns.SendAsync(id);
            // Console.WriteLine($"Sent: {sendResult.Data.Status}");

            await client.Campaigns.DeleteAsync(id);
            Console.WriteLine("Deleted.");
        }

        // POST /service/Campaign/AddRecipients
        public static async Task AddRecipientsAsync(MobizonClient client)
        {
            Console.WriteLine("=== Campaign.AddRecipients ===");
            // Replace with a real campaign ID and recipients
            var result = await client.Campaigns.AddRecipientsAsync(new AddRecipientsRequest
            {
                CampaignId = 1,
                Recipients = new[]
                {
                    new RecipientEntry { Recipient = "77017221502" },
                    new RecipientEntry { Recipient = "77029932233" },
                }
            });
            if (result.Data.Entries != null)
                foreach (var e in result.Data.Entries)
                    Console.WriteLine($"  {e.Recipient}  Code={e.Code}  MessageId={e.MessageId}");
        }
    }
}

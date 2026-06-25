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
            foreach (var c in result.Items)
                Console.WriteLine($"  Id={c.Id}  Name={c.Name}  Status={c.CommonStatus}");
        }

        // POST /service/Campaign/Get
        public static async Task GetAsync(MobizonClient client)
        {
            Console.WriteLine("=== Campaign.Get ===");
            // Replace with a real campaign ID
            var result = await client.Campaigns.GetAsync(1);
            Console.WriteLine($"Id    : {result.Id}");
            Console.WriteLine($"Name  : {result.Name}");
            Console.WriteLine($"Status: {result.CommonStatus}");
            Console.WriteLine($"Text  : {result.Text}");
        }

        // POST /service/Campaign/GetInfo
        public static async Task GetInfoAsync(MobizonClient client)
        {
            Console.WriteLine("=== Campaign.GetInfo ===");
            // Replace with a real campaign ID
            var result = await client.Campaigns.GetInfoAsync(1);
            Console.WriteLine($"Id      : {result.Id}");
            Console.WriteLine($"Sent    : {result.Counters?.TotalDelivrdMsgNum}");
            Console.WriteLine($"Failed  : {result.Counters?.TotalUndelivMsgNum}");
            Console.WriteLine($"Total   : {result.Counters?.TotalMsgNum}");
        }

        // POST /service/Campaign/Create  →  /service/Campaign/Send  →  /service/Campaign/Delete
        public static async Task CreateSendDeleteAsync(MobizonClient client)
        {
            Console.WriteLine("=== Campaign.Create + Send + Delete ===");

            var id = await client.Campaigns.CreateAsync(new CreateCampaignRequest
            {
                Name = "SDK Test Campaign",
                Text = "Hello from Mobizon.Net SDK!",
                Type = CampaignType.Bulk,
            });
            Console.WriteLine($"Created Id: {id}");

            // SendAsync returns a CampaignSendResult; IsQueued is true when the API
            // queued the send as a background task (code 100).
            var sendResult = await client.Campaigns.SendAsync(id);
            if (sendResult.IsQueued)
                Console.WriteLine($"Queued as background task id: {sendResult.Id}");
            else
                Console.WriteLine($"Sent (id={sendResult.Id})");

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
            Console.WriteLine($"Outcome: {result.Outcome}");
            if (result.Entries != null)
                foreach (var e in result.Entries)
                    Console.WriteLine($"  {e.Recipient}  Code={e.Code}  MessageId={e.MessageId}");
        }
    }
}

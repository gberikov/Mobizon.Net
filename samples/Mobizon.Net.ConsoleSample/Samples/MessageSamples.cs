using System;
using System.Threading.Tasks;
using Mobizon.Contracts.Models.Common;
using Mobizon.Contracts.Models.Messages;
using Mobizon.Net;

namespace Mobizon.Net.ConsoleSample.Samples
{
    static class MessageSamples
    {
        // POST /service/Message/SendSmsMessage  (minimal params)
        public static async Task QuickSendAsync(MobizonClient client, string recipient, string text)
        {
            Console.WriteLine("=== Message.QuickSend ===");
            var result = await client.Messages.QuickSendAsync(recipient, text);
            Console.WriteLine($"MessageId : {result.Data.MessageId}");
            Console.WriteLine($"CampaignId: {result.Data.CampaignId}");
            Console.WriteLine($"Status    : {result.Data.Status}");
        }

        // POST /service/Message/SendSmsMessage  (with optional params)
        public static async Task SendSmsMessageAsync(MobizonClient client, string recipient, string text)
        {
            Console.WriteLine("=== Message.SendSmsMessage (with params) ===");
            var result = await client.Messages.SendSmsMessageAsync(new SendSmsMessageRequest
            {
                Recipient = recipient,
                Text = text,
                Parameters = new SmsMessageParameters
                {
                    Validity = TimeSpan.FromHours(1),
                    MessageClass = MessageClass.Normal,
                    DeferredTo = DateTime.Now.AddHours(2),
                }
            });
            Console.WriteLine($"MessageId : {result.Data.MessageId}");
        }

        // POST /service/Message/GetSMSStatus
        public static async Task GetStatusAsync(MobizonClient client)
        {
            Console.WriteLine("=== Message.GetSmsStatus ===");
            // Replace with a real message ID
            var singleResult = await client.Messages.GetSmsStatusAsync(800191838);
            foreach (var s in singleResult.Data)
                Console.WriteLine($"Single:  Id={s.Id}  Status={s.Status}  Segments={s.Segments}");
            var multiResult = await client.Messages.GetSmsStatusAsync(new[] { 800196668, 800196662 });
            foreach (var s in multiResult.Data)
                Console.WriteLine($"Multi:   Id={s.Id}  Status={s.Status}  Segments={s.Segments}");
        }

        // POST /service/Message/List
        public static async Task ListAsync(MobizonClient client)
        {
            Console.WriteLine("=== Message.List ===");
            var result = await client.Messages.ListAsync(new MessageListRequest
            {
                // Criteria = new MessageListCriteria { CampaignSentFrom = new DateTime(2025, 12,1), CampaignSentTo =  new DateTime(2025, 12,31) },
                Pagination = new PaginationRequest { CurrentPage = 0, PageSize = 10 },
                Sort = new SortRequest { Field = "campaignId", Direction = SortDirection.DESC }
            });
            Console.WriteLine($"Total: {result.Data.TotalItemCount}");
            foreach (var m in result.Data.Items)
                Console.WriteLine($"  Id={m.Id}  To={m.To}  Status={m.Status}  Text={m.Text}");
        }
    }
}

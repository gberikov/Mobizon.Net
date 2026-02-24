using System;
using System.Threading.Tasks;
using Mobizon.Contracts.Models;
using Mobizon.Contracts.Models.Link;
using Mobizon.Net;

namespace Mobizon.Net.ConsoleSample.Samples
{
    static class LinkSamples
    {
        // POST /service/link/List
        public static async Task ListAsync(MobizonClient client)
        {
            Console.WriteLine("=== Link.List ===");
            var result = await client.Links.ListAsync(new LinkListRequest
            {
                Pagination = new PaginationRequest { CurrentPage = 0, PageSize = 10 },
                Sort       = new SortRequest { Field = "id", Direction = SortDirection.DESC }
            });
            foreach (var l in result.Data)
                Console.WriteLine($"  Id={l.Id}  Code={l.Code}  Url={l.FullLink}  Clicks={l.Clicks}");
        }

        // POST /service/link/Create  →  /service/link/Get  →  /service/link/Update  →  /service/link/Delete
        public static async Task CreateGetUpdateDeleteAsync(MobizonClient client)
        {
            Console.WriteLine("=== Link.Create + Get + Update + Delete ===");

            var createResult = await client.Links.CreateAsync(new CreateLinkRequest
            {
                FullLink = "https://example.com",
                Comment  = "SDK test link"
            });
            var code = createResult.Data.Code!;
            Console.WriteLine($"Created: code={code}  url={createResult.Data.FullLink}");

            var getResult = await client.Links.GetAsync(code);
            Console.WriteLine($"Get    : clicks={getResult.Data.Clicks}");

            await client.Links.UpdateAsync(new UpdateLinkRequest
            {
                Code    = code,
                Comment = "Updated by SDK"
            });
            Console.WriteLine("Updated comment.");

            await client.Links.DeleteAsync(new[] { createResult.Data.Id });
            Console.WriteLine("Deleted.");
        }

        // POST /service/link/GetStats
        public static async Task GetStatsAsync(MobizonClient client)
        {
            Console.WriteLine("=== Link.GetStats ===");
            // Replace with a real link ID
            var result = await client.Links.GetStatsAsync(new GetLinkStatsRequest
            {
                Ids  = new[] { 1 },
                Type = LinkStatsType.Daily
            });
            foreach (var s in result.Data)
                Console.WriteLine($"  LinkId={s.LinkId}  Date={s.Date}  Clicks={s.Clicks}");
        }
    }
}

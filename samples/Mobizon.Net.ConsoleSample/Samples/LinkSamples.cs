using System;
using System.Threading.Tasks;
using Mobizon.Contracts;
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
            foreach (var l in result.Items)
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
            var id   = createResult.Id;
            var code = createResult.Code!;
            Console.WriteLine($"Created: id={id}  code={code}  url={createResult.FullLink}");

            var getResult = await client.Links.GetByCodeAsync(code);
            Console.WriteLine($"Get    : clicks={getResult.Clicks}");

            await client.Links.UpdateAsync(new UpdateLinkRequest
            {
                Id      = id,
                Comment = "Updated by SDK"
            });
            Console.WriteLine("Updated comment.");

            await client.Links.DeleteAsync(new[] { createResult.Id });
            Console.WriteLine("Deleted.");
        }

        // POST /service/link/GetStats
        public static async Task GetStatsAsync(MobizonClient client)
        {
            Console.WriteLine("=== Link.GetStats ===");
            // Replace with a real link ID
            var result = await client.Links.GetStatsAsync(new GetLinkStatsRequest
            {
                Ids  = new[] { 1L },
                Type = LinkStatsType.Daily
            });
            foreach (var s in result.Links)
            {
                Console.WriteLine($"  LinkId={s.LinkId}  TotalClicks={s.TotalClicks}  TotalRedirects={s.TotalRedirects}");
                foreach (var pt in s.Points)
                    Console.WriteLine($"    {pt.Param}  clicks={pt.Clicks}  redirects={pt.Redirects}");
            }
        }
    }
}

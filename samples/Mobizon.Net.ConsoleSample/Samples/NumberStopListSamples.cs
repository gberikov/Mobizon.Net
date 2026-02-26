using System;
using System.Threading.Tasks;
using Mobizon.Contracts.Models.Common;
using Mobizon.Contracts.Models.StopLists;
using Mobizon.Net;

namespace Mobizon.Net.ConsoleSample.Samples
{
    static class NumberStopListSamples
    {
        // POST /service/numberstoplist/list
        public static async Task ListAsync(MobizonClient client)
        {
            Console.WriteLine("=== NumberStopList.List ===");
            var result = await client.NumberStopList.ListAsync(new StopListListRequest
            {
                Pagination = new PaginationRequest { CurrentPage = 0, PageSize = 25 },
                Sort       = new SortRequest { Field = "createTs", Direction = SortDirection.DESC }
            });
            Console.WriteLine($"Total: {result.Data.TotalItemCount}");
            foreach (var e in result.Data.Items)
                Console.WriteLine($"  Id={e.Id}  Number={e.Number}  Country={e.CountryA2}  Operator={e.OperatorName}  Comment={e.Comment}");
        }

        // POST /service/numberstoplist/create  (single number)
        public static async Task AddNumberAsync(MobizonClient client)
        {
            Console.WriteLine("=== NumberStopList.AddNumber ===");
            // Replace with the number you want to block
            var result = await client.NumberStopList.AddNumberAsync(
                "77007782006",
                comment: "SDK test block");
            Console.WriteLine($"Created record Id: {result.Data}");
        }

        // POST /service/numberstoplist/create  (number range)
        public static async Task AddNumberRangeAsync(MobizonClient client)
        {
            Console.WriteLine("=== NumberStopList.AddRange ===");
            // Replace with the range you want to block
            var result = await client.NumberStopList.AddNumberRangeAsync(
                "77470944002",
                "77470944000",
                comment: "SDK range test");
            Console.WriteLine($"Range added: {result.Data}");
        }

        // POST /service/numberstoplist/delete
        public static async Task DeleteAsync(MobizonClient client)
        {
            Console.WriteLine("=== NumberStopList.Delete ===");
            // Replace with the record ID returned by AddNumber/AddRange
            var result = await client.NumberStopList.DeleteAsync(83822);
            Console.WriteLine($"Deleted: {result.Data}");
        }
    }
}

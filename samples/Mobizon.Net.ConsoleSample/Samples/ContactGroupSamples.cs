using System;
using System.Threading.Tasks;
using Mobizon.Contracts.Models.Common;
using Mobizon.Contracts.Models.ContactGroups;
using Mobizon.Net;

namespace Mobizon.Net.ConsoleSample.Samples
{
    static class ContactGroupSamples
    {
        // POST /service/contactgroup/list
        public static async Task ListAsync(MobizonClient client)
        {
            Console.WriteLine("=== ContactGroup.List ===");
            var result = await client.ContactGroups.ListAsync(new ContactGroupListRequest
            {
                Pagination = new PaginationRequest { CurrentPage = 0, PageSize = 25 },
                Sort       = new SortRequest { Field = "name", Direction = SortDirection.ASC }
            });
            Console.WriteLine($"Total: {result.Data.TotalItemCount}");
            foreach (var g in result.Data.Items)
                Console.WriteLine($"  Id={g.Id}  Name={g.Name}  Cards={g.CardsCount}  Created={g.Created}");
        }

        // POST /service/contactgroup/create  →  /update  →  /delete
        public static async Task CreateUpdateDeleteAsync(MobizonClient client)
        {
            Console.WriteLine("=== ContactGroup.Create + Update + Delete ===");

            var createResult = await client.ContactGroups.CreateAsync("SDK Test Group 2");
            var id = createResult.Data;
            Console.WriteLine($"Created Id: {id}");

            var updateResult = await client.ContactGroups.UpdateAsync(id, "SDK Test Group (renamed)");
            Console.WriteLine($"Updated   : {updateResult.Data}");

            var deleteResult = await client.ContactGroups.DeleteAsync(id);
            Console.WriteLine($"Processed : [{string.Join(", ", deleteResult.Data.Processed)}]");
            Console.WriteLine($"Not proc  : [{string.Join(", ", deleteResult.Data.NotProcessed)}]");
        }

        // POST /service/contactgroup/getcardscount
        public static async Task GetCardsCountAsync(MobizonClient client)
        {
            Console.WriteLine("=== ContactGroup.GetCardsCount ===");
            // Pass a group ID, or omit to count contacts without any group
            var result = await client.ContactGroups.GetCardsCountAsync(100604);
            Console.WriteLine($"Count (no group): {result.Data}");
        }
    }
}

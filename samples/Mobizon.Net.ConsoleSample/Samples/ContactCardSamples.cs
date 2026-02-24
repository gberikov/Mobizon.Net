using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mobizon.Contracts.Models;
using Mobizon.Contracts.Models.ContactCard;
using Mobizon.Net;

namespace Mobizon.Net.ConsoleSample.Samples
{
    static class ContactCardSamples
    {
        // POST /service/contactcard/list  (all contacts, no filter)
        public static async Task ListAsync(MobizonClient client)
        {
            Console.WriteLine("=== ContactCard.List ===");
            var result = await client.ContactCards.ListAsync(new ContactCardListRequest
            {
                Pagination = new PaginationRequest { CurrentPage = 0, PageSize = 10 },
                Sort       = new SortRequest { Field = "id", Direction = SortDirection.DESC }
            });
            Console.WriteLine($"Total: {result.Data.TotalItemCount}  (full list: {result.Data.FullListItemCount})");
            foreach (var c in result.Data.Items)
                Console.WriteLine($"  Id={c.Id}  Name={c.Fields?.Name} {c.Fields?.Surname}  Mobile={c.Fields?.Mobile?.Value}");
        }

        // POST /service/contactcard/list  (filtered by group)
        public static async Task ListByGroupAsync(MobizonClient client)
        {
            Console.WriteLine("=== ContactCard.List (by group) ===");
            // Replace with a real group ID
            var result = await client.ContactCards.ListAsync(new ContactCardListRequest
            {
                Criteria = new[]
                {
                    new ContactCardCriteria { Field = "groupId", Operator = "equal", Value = "100604" }
                },
                Pagination = new PaginationRequest { CurrentPage = 0, PageSize = 10 }
            });
            Console.WriteLine($"Total: {result.Data.TotalItemCount}");
            foreach (var c in result.Data.Items)
                Console.WriteLine($"  Id={c.Id}  Name={c.Fields?.Name}  Mobile={c.Fields?.Mobile?.Value}");
        }

        // POST /service/contactcard/get
        public static async Task GetAsync(MobizonClient client)
        {
            Console.WriteLine("=== ContactCard.Get ===");
            // Replace with a real card ID
            var result = await client.ContactCards.GetAsync("77885666");
            var f = result.Data.Fields;
            Console.WriteLine($"Id    : {result.Data.Id}");
            Console.WriteLine($"Name  : {f?.Name} {f?.Surname}");
            Console.WriteLine($"Mobile: {f?.Mobile?.Value}  ({f?.Mobile?.Operator}, {f?.Mobile?.CountryA2})");
            Console.WriteLine($"Email : {f?.Email}");
        }

        // POST /service/contactcard/create  →  /service/contactcard/setgroups
        public static async Task CreateAndSetGroupAsync(MobizonClient client)
        {
            Console.WriteLine("=== ContactCard.Create + SetGroups ===");

            var createResult = await client.ContactCards.CreateAsync(new CreateContactCardRequest
            {
                Name        = "SDK",
                Surname     = "Test",
                MobileValue = "77017221502",
                Email       = "sdk@example.com",
                Info        = "Created by Mobizon.Net SDK"
            });
            var cardId = createResult.Data!;
            Console.WriteLine($"Created card Id: {cardId}");

            // Replace with a real group ID
            // var setResult = await client.ContactCards.SetGroupsAsync(cardId, ["100604"]);
            // Console.WriteLine($"SetGroups: {setResult.Data}");
        }

        // POST /service/contactcard/update
        public static async Task UpdateAsync(MobizonClient client)
        {
            Console.WriteLine("=== ContactCard.Update ===");
            // Replace with a real card ID
            var result = await client.ContactCards.UpdateAsync(new UpdateContactCardRequest
            {
                Id      = "77885666",
                Name    = "Updated",
                Surname = "Name",
            });
            Console.WriteLine($"Updated: {result.Data}");
        }

        // POST /service/contactcard/getgroups
        public static async Task GetGroupsAsync(MobizonClient client)
        {
            Console.WriteLine("=== ContactCard.GetGroups ===");
            // Replace with a real card ID
            var result = await client.ContactCards.GetGroupsAsync("77885666");
            Console.WriteLine($"Groups ({result.Data.Count}):");
            foreach (var g in result.Data)
                Console.WriteLine($"  Id={g.Id}  Name={g.Name}");
        }
    }
}

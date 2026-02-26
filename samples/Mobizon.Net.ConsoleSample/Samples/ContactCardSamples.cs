using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mobizon.Contracts.Models.ContactCards;
using Mobizon.Net;

namespace Mobizon.Net.ConsoleSample.Samples
{
    static class ContactCardSamples
    {
        // POST /service/contactcard/list
        public static async Task ListAsync(MobizonClient client)
        {
            Console.WriteLine("=== ContactCard.List ===");

            var cards = await client.ContactCards
                .Where(x => x.GroupId == 100847)
                .Take(10)
                .OrderByDescending(x => x.Viber.Type)
                .ToListAsync();

            Console.WriteLine($"Loaded: {cards.Count}");
            foreach (var c in cards)
                Console.WriteLine($"  Id={c.Id}  Name={c.Name} {c.Surname}  Mobile={c.Mobile?.Value}");
        }

        // POST /service/contactcard/list  (filtered by group)
        public static async Task ListByGroupAsync(MobizonClient client)
        {
            Console.WriteLine("=== ContactCard.List (by group) ===");

            var cards = await client.ContactCards
                .Where(x => x.GroupId == 100604)
                .Take(10)
                .ToListAsync();

            Console.WriteLine($"Total in group: {cards.Count}");
            foreach (var c in cards)
                Console.WriteLine($"  Id={c.Id}  Name={c.Name}  Mobile={c.Mobile?.Value}");
        }

        // POST /service/contactcard/get
        public static async Task GetAsync(MobizonClient client)
        {
            Console.WriteLine("=== ContactCard.Find ===");
            // Replace with a real card ID
            var card = await client.ContactCards.FindAsync(77885666);
            if (card == null)
            {
                Console.WriteLine("Not found.");
                return;
            }
            Console.WriteLine($"Id      : {card.Id}");
            Console.WriteLine($"Name    : {card.Name} {card.Surname}");
            Console.WriteLine($"Mobile  : {card.Mobile?.Value}");
            Console.WriteLine($"Email   : {card.Email?.Value}");
        }

        // POST /service/contactcard/list  →  First/Single
        public static async Task FirstAndSingleAsync(MobizonClient client)
        {
            Console.WriteLine("=== ContactCard.First / Single ===");

            var first = await client.ContactCards
                .Where(x => x.GroupId == 100604)
                .FirstOrDefaultAsync();
            Console.WriteLine(first != null
                ? $"FirstOrDefault: Id={first.Id}  Name={first.Name}"
                : "FirstOrDefault: null");

            // Single — throws if more than one result
            try
            {
                var single = await client.ContactCards
                    .Where(x => x.GroupId == 100604 && x.Surname == "Петров")
                    .SingleOrDefaultAsync();
                Console.WriteLine(single != null
                    ? $"SingleOrDefault: Id={single.Id}  Name={single.Name}"
                    : "SingleOrDefault: null");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Single error: {ex.Message}");
            }
        }

        // POST /service/contactcard/create  →  /update
        public static async Task AddAndUpdateAsync(MobizonClient client)
        {
            Console.WriteLine("=== ContactCard.Add + Update ===");

            var card = new ContactCard
            {
                Name    = "SDK",
                Surname = "Test",
                Mobile  = new MobileFieldInfo { Value = "77017221502" },
                Email   = new ContactFieldInfo { Value = "sdk@example.com" },
                Info    = "Created by Mobizon.Net SDK"
            };

            await client.ContactCards.AddAsync(card);
            Console.WriteLine($"Created Id: {card.Id}");

            card.Surname = "Test (updated)";
            await client.ContactCards.UpdateAsync(card);
            Console.WriteLine("Updated.");
        }

        // POST /service/contactcard/getgroups  →  /setgroups
        public static async Task GroupsAsync(MobizonClient client)
        {
            Console.WriteLine("=== ContactCard.GetGroups + SetGroups ===");
            // Replace with a real card ID
            const int cardId = 77885666;

            var groups = await client.ContactCards.GetGroupsAsync(cardId);
            Console.WriteLine($"Groups ({groups.Count}):");
            foreach (var g in groups)
                Console.WriteLine($"  Id={g.Id}  Name={g.Name}");

            // Replace with real group IDs
            await client.ContactCards.SetGroupsAsync(cardId, new List<string> { "100604" });
            Console.WriteLine("Groups updated.");
        }

        // POST /service/contactcard/delete
        public static async Task RemoveAsync(MobizonClient client)
        {
            Console.WriteLine("=== ContactCard.Remove ===");
            // Replace with a real card ID to delete
            const int cardId = 77885666;

            await client.ContactCards.RemoveAsync(cardId);
            Console.WriteLine($"Deleted card {cardId}.");
        }

        // POST /service/contactcard/list  →  ToPageAsync (pagination metadata)
        public static async Task ToPageAsync(MobizonClient client)
        {
            Console.WriteLine("=== ContactCard.ToPage ===");

            var page = await client.ContactCards
                .Where(x => x.GroupId == 100604)
                .Take(10)
                .ToPageAsync();

            Console.WriteLine($"Page size : {page.PageSize}");
            Console.WriteLine($"Page index: {page.CurrentPage}");
            Console.WriteLine($"Total     : {page.TotalCount}");
            Console.WriteLine($"Loaded    : {page.Items.Count}");
            foreach (var c in page.Items)
                Console.WriteLine($"  Id={c.Id}  Name={c.Name} {c.Surname}");
        }
    }
}

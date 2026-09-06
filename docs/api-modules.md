# API Modules

[← Back to the README](https://github.com/gberikov/Mobizon.Net/blob/master/README.md)

---

`IMobizonClient` exposes nine sub-services as properties:

```
IMobizonClient
├── .Messages        — IMessageService
├── .Campaigns       — ICampaignService
├── .Links           — ILinkService
├── .User            — IUserService
├── .TaskQueue       — ITaskQueueService
├── .ContactGroups   — IContactGroupService
├── .ContactCards    — IContactCardSet  (query entry point; returns IContactCardQuery)
├── .NumberStopList  — INumberStopListService
└── .Alphanames      — IAlphanameService
```

## Messages

Send individual SMS messages and query delivery status.

**Send an SMS with optional parameters:**

```csharp
var result = await client.Messages.SendSmsMessageAsync(
    new SendSmsMessageRequest
    {
        Recipient  = "77001234567",
        Text       = "Your verification code: 8421",
        From       = "MyBrand",   // optional sender name / alphaname
        Parameters = new SmsMessageParameters
        {
            Validity     = TimeSpan.FromMinutes(60),   // optional delivery window
            ShortenLinks = true,   // shorten URLs in Text via Mobizon's link shortener
        }
    });

Console.WriteLine($"Campaign: {result.CampaignId}");
Console.WriteLine($"Message:  {result.MessageId}");
Console.WriteLine($"Status:   {result.Status}");
```

**Check delivery status** (1–100 message IDs per call; larger sets must be split by the caller):

```csharp
var status = await client.Messages.GetSmsStatusAsync(
    new[] { result.MessageId });

foreach (var item in status)
    Console.WriteLine($"ID={item.Id}  Status={item.Status}  Segments={item.Segments}");
```

**List messages with pagination:**

```csharp
var messages = await client.Messages.ListAsync(
    new MessageListRequest
    {
        Criteria   = new MessageListCriteria { Status = SmsStatus.Delivered },
        Pagination = new PaginationRequest   { CurrentPage = 0, PageSize = 50 },
        Sort       = new SortRequest         { Field = "id", Direction = SortDirection.Descending }
    });

Console.WriteLine($"Total: {messages.TotalItemCount}");
foreach (var msg in messages.Items)
    Console.WriteLine($"[{msg.Id}] {msg.Text}");
```

---

## Campaigns

Create and send bulk SMS campaigns, then retrieve delivery statistics.

```csharp
// 1. Create campaign — returns the new campaign ID directly
long campaignId = await client.Campaigns.CreateAsync(
    new CreateCampaignRequest
    {
        Type = CampaignType.Bulk,
        From = "MyBrand",
        Text = "Flash sale — 50% off today!"
    });

// 2. Add recipients (phone numbers or contact cards are processed synchronously, in batches of
//    up to 500 — the SDK splits larger lists for you). Outcome aggregates the batches:
//    AllAdded, PartiallyAdded (any batch accepted + any rejected) or NoneAdded (every batch rejected).
var addResult = await client.Campaigns.AddRecipientsAsync(
    new AddRecipientsRequest
    {
        CampaignId = campaignId,
        Recipients = new[]
        {
            new RecipientEntry { Recipient = "77001111111" },
            new RecipientEntry { Recipient = "77002222222" }
        }
    });

foreach (var entry in addResult.Entries!.Where(e => e.Code != 0))
    Console.WriteLine($"Rejected {entry.Number}: code {entry.Code}");

// 3. Send — returns CampaignSendResult; IsQueued is true when the API accepted
//    the send as a background task (response code 100).
var sendResult = await client.Campaigns.SendAsync(campaignId);

// 4. If the API queued a background task, poll for completion (at most once per second)
if (sendResult.IsQueued)
{
    var taskStatus = await client.TaskQueue.GetStatusAsync(sendResult.Id);
    Console.WriteLine($"Progress: {taskStatus.Progress}%");
}

// 5. Get delivery statistics
var info = await client.Campaigns.GetInfoAsync(campaignId);
Console.WriteLine($"Delivered: {info.Counters?.TotalDelivrdMsgNum}");

// 6. Short links used by the campaign (same as client.Links.GetLinksAsync)
var links = await client.Campaigns.GetLinksAsync(campaignId);
```

Other available methods: `GetAsync`, `ListAsync`, `DeleteAsync`, `GetLinksAsync`.

`ListAsync` items are `CampaignInfo` objects, as the API documents: cast one to read its statistics instead of
calling `GetInfoAsync` for every campaign.

```csharp
var page = await client.Campaigns.ListAsync();
foreach (var campaign in page.Items)
    if (campaign is CampaignInfo info && info.Counters is { } counters)
        Console.WriteLine($"{info.Id}: {counters.TotalDelivrdMsgNum}/{counters.TotalMsgNum} delivered");
```

Delivery settings are readable through `Validity`, `MessageClass` and `TrackShortLinkRecipients` whichever way
the server sent them: the documentation puts them at the top level, real responses nest them in `extra`
(also exposed as `CampaignExtra`). `Groups` arrives as a comma-separated string and is parsed into a list.

**Recipients from a file or contact groups are queued, not added.** The API answers with a background task;
`IsQueued` is `true` and `Outcome` is `AllAdded` only in the sense of "accepted". Poll the task until it completes
(or is rejected) before sending the campaign:

```csharp
using var csv = File.OpenRead("recipients.csv");            // you own the stream; the SDK never closes it
var queued = await client.Campaigns.AddRecipientsAsync(new AddRecipientsRequest
{
    CampaignId = campaignId, RecipientsFile = csv, RecipientsFileName = "recipients.csv"
});

var status = await client.TaskQueue.GetStatusAsync(queued.TaskId!.Value);
while (status.Status == BackgroundTaskStatus.Pending || status.Status == BackgroundTaskStatus.InProgress)
{
    await Task.Delay(TimeSpan.FromSeconds(1));               // Mobizon: poll no more than once per second
    status = await client.TaskQueue.GetStatusAsync(queued.TaskId.Value);
}
if (status.Status != BackgroundTaskStatus.Completed)
    throw new InvalidOperationException($"Recipient import failed: {status.Status}");

await client.Campaigns.SendAsync(campaignId);
```

**Partial execution of a large list.** When a later batch of a multi-batch send fails — API error, transport error,
timeout or your own cancellation — the earlier batches are already applied on the server. The thrown exception
(any type, including `OperationCanceledException`) carries an `AddRecipientsProgress`: the confirmed entries,
how many recipients they cover, and the size of the batch whose outcome is **unknown** (its request may have been
applied even though no response arrived). Never resend the whole list after a timeout; check the campaign
(`GetInfoAsync` / `Messages.ListAsync`) or resume from `ConfirmedCount + PendingCount` and reconcile the pending batch.

```csharp
try
{
    await client.Campaigns.AddRecipientsAsync(bigRequest);
}
catch (Exception ex) when (AddRecipientsProgress.FromException(ex) is { } progress)
{
    Console.WriteLine($"{progress.ConfirmedCount} confirmed, {progress.PendingCount} unknown, " +
                      $"{progress.Confirmed.Entries!.Count(e => e.Code == 0)} accepted so far");
    throw;
}
```

`Replace = true` is honoured by the first batch only, so later batches never wipe what earlier ones added.

**Placeholder names share the wire namespace with the phone number.** A placeholder called `recipient` would
replace the destination number, so it is rejected, as are empty names and names containing `[` or `]`. The whole
list is checked before the first batch is sent, so a bad name never applies to part of it.

```csharp
Recipients = new[]
{
    new RecipientEntry
    {
        Recipient    = "77001111111",
        Placeholders = new Dictionary<string, string> { ["name"] = "Ivan" }   // "recipient" would throw
    }
}
```

---

## Links

Create and manage short links for click tracking in SMS messages.

**Create a short link:**

```csharp
var link = await client.Links.CreateAsync(
    new CreateLinkRequest
    {
        FullLink       = "https://example.com/promo",
        Comment        = "Summer sale campaign",
        ExpirationDate = new DateTime(2026, 12, 31)
    });

Console.WriteLine($"Short code: {link.Code}");
```

**Retrieve a link by short code, numeric ID, or full short URL:**

```csharp
var byCode      = await client.Links.GetByCodeAsync("abc123");
var byId        = await client.Links.GetByIdAsync(link.Id);
var byShortLink = await client.Links.GetByShortLinkAsync("https://mbzn.co/abc123");

Console.WriteLine($"URL: {byCode.FullLink}  Clicks: {byCode.Clicks}");
```

**Get click statistics:**

```csharp
var stats = await client.Links.GetStatsAsync(
    new GetLinkStatsRequest
    {
        Ids      = new[] { link.Id },
        Type     = LinkStatsType.Daily,   // up to 5 ids per request
        DateFrom = new DateTime(2026, 1, 1),
        DateTo   = new DateTime(2026, 1, 31, 23, 59, 59)
    });

// GetStatsAsync returns a LinkStatsResult with Links (one entry per requested link ID)
foreach (var s in stats.Links)
{
    Console.WriteLine($"LinkId={s.LinkId}  Total clicks={s.TotalClicks}");
    foreach (var entry in s.Points)
        Console.WriteLine($"  {entry.Param}: {entry.Clicks} clicks");
}
```

**Update a link:**

```csharp
await client.Links.UpdateAsync(
    new UpdateLinkRequest
    {
        Id      = link.Id,
        Comment = "Updated comment"
    });
```

**Delete links:**

```csharp
var deleted = await client.Links.DeleteAsync(new[] { link.Id });
if (!deleted.AllProcessed)
    Console.WriteLine($"Not deleted: {string.Join(", ", deleted.NotProcessed)}");
```

Other available methods: `GetLinksAsync` (links by campaign ID), `ListAsync`.

**Updating a link does not merge with what is stored.** The SDK sends only the properties you set, and the API
documents an omitted `data[expirationDate]` as making the link valid indefinitely — a null `ExpirationDate` does
not preserve the current one. Read the link first and pass the value back when you mean to keep it. This follows
the published contract and has not been confirmed against a live account. `FullLink` is not among the documented
update parameters; it is sent when set, but the server may ignore it.

```csharp
var link = await client.Links.GetByIdAsync(id);
await client.Links.UpdateAsync(new UpdateLinkRequest
{
    Id             = id,
    Comment        = "Q3 campaign",
    ExpirationDate = link.ExpirationDate   // omit this and the link may become permanent
});
```

---

## User

Check your Mobizon account balance.

```csharp
var balance = await client.User.GetOwnBalanceAsync();
Console.WriteLine($"{balance.Balance:0.0000} {balance.Currency}"); // decimal
// Output: 4043.0656 KZT
```

All SDK calls are HTTP POST; the API key travels in the request body, never in the URL.

---

## TaskQueue

Poll the progress of a long-running background task (typically returned after `Campaigns.SendAsync`).

```csharp
var taskStatus = await client.TaskQueue.GetStatusAsync(taskId);
Console.WriteLine($"{taskStatus.Status}: {taskStatus.Progress}% complete");
```

`TaskQueueStatus` fields: `Status` (`BackgroundTaskStatus`: Pending / InProgress / Completed / Rejected) and `Progress` (0–100).

---

## Contact cards & groups

`client.ContactCards` is an immutable, LINQ-style query builder over `contactcard/list`; `client.ContactGroups` manages groups.

```csharp
long groupId = await client.ContactGroups.CreateAsync("VIP");

var card = new ContactCard
{
    Name = "Ivan", Surname = "Petrov",
    Mobile = new MobileFieldInfo { Value = "77001234567", Type = ContactType.Main },
    Gender = Gender.Male
};
await client.ContactCards.AddAsync(card);                 // sets card.Id
await client.ContactCards.SetGroupsAsync(card.Id!.Value, new[] { groupId });

// Filter, sort and page on the server. Take = page size (default 25), Page = zero-based page index.
var vip = client.ContactCards.Where(x => x.GroupId == groupId).OrderBy(x => x.Surname).Take(50);
var firstPage  = await vip.Page(0).ToListAsync();
var secondPage = await vip.Page(1).ToPageAsync();          // includes TotalCount
int total      = await vip.CountAsync();                   // whole query; Page/Take do not matter
var head       = await vip.Page(1).FirstOrDefaultAsync();  // first item *of page 1* (same as secondPage.Items[0])
var only       = await client.ContactCards.Where(x => x.Mobile.Value == "77001234567").SingleAsync();
// Single checks uniqueness across the whole query (server-side total), ignoring Page/Take.

// UpdateAsync is a full replace: load, modify, save. Null fields are cleared on the server —
// except Address: a null Address keeps the server's address; an empty AddressFieldInfo clears it.
var existing = await client.ContactCards.FindAsync(card.Id.Value);
existing!.Info    = "Preferred customer";
existing.Address  = new AddressFieldInfo();                 // clear the address
await client.ContactCards.UpdateAsync(existing);
```

Contact fields arrive in more than one shape. An empty array or empty string means "not set" and reads as `null`;
a bare string (the shape the SDK itself writes) is mapped onto `Value`; anything else raises `MobizonException`
rather than silently reading as `null` and being cleared by the next update. A `type` or `gender` value the SDK
does not know stays in `Mobile.TypeRaw` / `GenderRaw`, and an update sends it back unchanged.

`CountAsync`, `FirstOrDefaultAsync` and `SingleOrDefaultAsync` request 25 items, the smallest page size the
documented list endpoints accept (25, 50 or 100).

Supported filter operators: `==` (including `== null` for "empty"), `!=`, `>=`, `<=`, `.Contains()`, combined with `&&`.
The filtered member must be on the left-hand side (`x.GroupId == 33`, not `33 == x.GroupId`), and the value must be a
constant or a captured variable. The API has no "not empty" operator, so `!= null` and `!= Gender.Undefined` are
rejected with `NotSupportedException` rather than sent as something that means the opposite.

---

## Number stop-list

Numbers (or ranges) that must never receive your messages. `campaign/addRecipients` reports such recipients
with per-entry code 5.

```csharp
long entryId = await client.NumberStopList.AddNumberAsync("77001234567", "Opted out via support");
await client.NumberStopList.AddNumberRangeAsync("77001000000", "77001999999", "Test range");

var page = await client.NumberStopList.ListAsync(new StopListListRequest
{
    Pagination = new PaginationRequest { CurrentPage = 0, PageSize = 50 }
});
foreach (var e in page.Items)
    Console.WriteLine($"{e.Id}: {e.Number} ({e.Level}) — {e.Comment}");

await client.NumberStopList.DeleteAsync(entryId);
```

---

## Alphanames

List the registered sender IDs available to the account.

```csharp
var alphanames = await client.Alphanames.ListAsync();
foreach (var a in alphanames.Items)
    Console.WriteLine($"{a.Alphaname?.Name}  (id={a.AlphanameId})");
```


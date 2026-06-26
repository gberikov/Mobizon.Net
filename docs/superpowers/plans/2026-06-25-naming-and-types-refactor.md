# Naming & Types Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rename API-dictated model properties to idiomatic .NET names and strengthen weak types (string→DateTime, int→enum, int→bool) where the value domain is known, plus remove a duplicate enum — with zero wire-behavior change.

**Architecture:** Each rename keeps its `[JsonPropertyName]` so the JSON wire mapping is unchanged. Read-side type changes are absorbed by converters already registered globally in `MobizonApiClient.JsonOptions` (or two new enum-converter registrations). Write-side type changes are formatted explicitly in the service code that builds the request-parameter dictionary.

**Tech Stack:** C# 8.0 / netstandard2.0 (core) + net8.0 (sample/webhooks), System.Text.Json, xUnit + RichardSzalay.MockHttp.

## Global Constraints

- `Mobizon.Contracts` / `Mobizon.Net` core target `netstandard2.0`; webhooks/sample target `net8.0`; C# 8.0 language features only; nullable reference types enabled; no new third-party dependencies.
- **No wire-behavior change.** Every renamed property keeps a `[JsonPropertyName]` mapping to its original JSON key. Write-side formatting must emit the exact same strings the API received before.
- Errors are signalled by throwing `MobizonApiException`; success = no exception.
- Datetime wire format is `yyyy-MM-dd HH:mm:ss` with `CultureInfo.InvariantCulture` (matches `MobizonDateTimeConverter`).
- Build/test the whole solution with `dotnet test` from `C:\Develop\Mobizon.Net`. Build must stay at **0 warnings**.
- Confirmed API value domains: Link `status` 0=Inactive/1=Active; Link `moderatorStatus` 0=Blocked/1=Approved; `placeholdersFlag` 1=KeepAsIs/2=Remove/3=Reject; `replace` 0/1; `recipientsFileSkipHeader` 0/1.
- TDD per task: update/author the test first, watch it fail, change the model/service, watch it pass, run full suite, commit.

---

## Task 1: Enum dedup — delete `AddRecipientsResponseCode`

**Files:**
- Delete: `src/Mobizon.Contracts/Models/Campaigns/AddRecipientsResponseCode.cs`
- Modify: `src/Mobizon.Net/Services/CampaignService.cs:343`
- Test: `tests/Mobizon.Net.Tests/Services/CampaignServiceTests.cs` (existing partial/none-added tests)

**Interfaces:**
- Consumes: existing `AddRecipientsOutcome { AllAdded=0, PartiallyAdded=98, NoneAdded=99 }`.
- Produces: nothing new — removes a public type.

- [ ] **Step 1: Confirm the only consumer.**

Run: `git grep -n "AddRecipientsResponseCode" -- src tests samples`
Expected: hits only in `AddRecipientsResponseCode.cs` (definition) and `CampaignService.cs:343`. If any test references it, update that test in this task.

- [ ] **Step 2: Point the consumer at `AddRecipientsOutcome`.**

In `src/Mobizon.Net/Services/CampaignService.cs`, the `SendAddRecipientsAsync` call currently ends:

```csharp
            return _apiClient.SendAsync<AddRecipientsResult>(
                HttpMethod.Post, ModuleName, "AddRecipients", parameters, cancellationToken,
                extraSuccessCodes: new[] { (int)AddRecipientsResponseCode.PartiallyAdded, (int)AddRecipientsResponseCode.NoneAdded });
```

Change the `extraSuccessCodes` line to:

```csharp
                extraSuccessCodes: new[] { (int)AddRecipientsOutcome.PartiallyAdded, (int)AddRecipientsOutcome.NoneAdded });
```

- [ ] **Step 3: Delete the duplicate enum file.**

```bash
git rm src/Mobizon.Contracts/Models/Campaigns/AddRecipientsResponseCode.cs
```

- [ ] **Step 4: Build + run AddRecipients tests.**

Run: `dotnet test --filter "FullyQualifiedName~CampaignServiceTests|FullyQualifiedName~AddRecipientsResultConverterTests"`
Expected: PASS (the partial-failure test still asserts `AddRecipientsOutcome.PartiallyAdded`; codes 98/99 still flow as non-errors). Then `git grep -n "AddRecipientsResponseCode"` → empty.

- [ ] **Step 5: Commit.**

```bash
git add -A
git commit -m "refactor: remove duplicate AddRecipientsResponseCode in favor of AddRecipientsOutcome"
```

---

## Task 2: `LinkData` read-side — timestamps, counters, status enums

**Files:**
- Create: `src/Mobizon.Contracts/Models/Links/LinkStatus.cs`
- Create: `src/Mobizon.Contracts/Models/Links/LinkModeratorStatus.cs`
- Modify: `src/Mobizon.Contracts/Models/Links/LinkData.cs`
- Modify: `src/Mobizon.Net/Internal/MobizonApiClient.cs:25-46` (converter registration)
- Test: `tests/Mobizon.Net.Tests/Services/LinkServiceTests.cs`

**Interfaces:**
- Produces: `enum LinkStatus { Inactive=0, Active=1 }`; `enum LinkModeratorStatus { Blocked=0, Approved=1 }`. `LinkData` now exposes `DateTime? Created`, `DateTime? Updated`, `int Clicks`, `int Redirects`, `LinkStatus Status`, `LinkModeratorStatus ModeratorStatus`.

- [ ] **Step 1: Update the read tests (TDD red).**

In `LinkServiceTests.cs`, the link `get`/`list` tests deserialize JSON with `"status":1`, `"clickCnt"` etc. Update their assertions to the new surface. Worked example — in `GetByIdAsync_SendsId` (and the create/getbycode/list tests that read these fields), change assertions like `result.ClickCnt` → `result.Clicks`, and add status assertions where a fixture carries them, e.g.:

```csharp
            // payload: {"code":0,"data":{"id":"42","code":"x","fullLink":"https://e.com","status":"1","moderatorStatus":"1","clickCnt":"7","redirectCnt":"3","createTs":"2026-01-02 03:04:05"},"message":""}
            var result = await CreateService(mockHttp).GetByIdAsync(42);
            Assert.Equal(42, result.Id);
            Assert.Equal(LinkStatus.Active, result.Status);
            Assert.Equal(LinkModeratorStatus.Approved, result.ModeratorStatus);
            Assert.Equal(7, result.Clicks);
            Assert.Equal(3, result.Redirects);
            Assert.Equal(new System.DateTime(2026, 1, 2, 3, 4, 5), result.Created);
```

(Adjust the mock payload in the test to include `moderatorStatus`, `redirectCnt`, and `createTs` so the assertions have data.)

- [ ] **Step 2: Run the link read tests to verify they fail.**

Run: `dotnet test --filter "FullyQualifiedName~LinkServiceTests"`
Expected: FAIL — compile errors (`LinkData` has no `Clicks`/`Status` of enum type).

- [ ] **Step 3: Create the two enums.**

```csharp
// src/Mobizon.Contracts/Models/Links/LinkStatus.cs
namespace Mobizon.Contracts.Models.Links
{
    /// <summary>User-set status of a short link.</summary>
    public enum LinkStatus
    {
        /// <summary>Link is inactive (API value 0).</summary>
        Inactive = 0,

        /// <summary>Link is active (API value 1).</summary>
        Active = 1
    }
}
```

```csharp
// src/Mobizon.Contracts/Models/Links/LinkModeratorStatus.cs
namespace Mobizon.Contracts.Models.Links
{
    /// <summary>Administrator moderation status of a short link.</summary>
    public enum LinkModeratorStatus
    {
        /// <summary>Blocked by the administrator (API value 0).</summary>
        Blocked = 0,

        /// <summary>Approved by the administrator (API value 1).</summary>
        Approved = 1
    }
}
```

- [ ] **Step 4: Retype/rename `LinkData`.**

Replace the body of `src/Mobizon.Contracts/Models/Links/LinkData.cs` with:

```csharp
using System;
using System.Text.Json.Serialization;

namespace Mobizon.Contracts.Models.Links
{
    /// <summary>Represents a Mobizon short link.</summary>
    public class LinkData
    {
        public long Id { get; set; }
        public string Code { get; set; } = string.Empty;
        [JsonPropertyName("shortLink")] public string? ShortLink { get; set; }
        public string FullLink { get; set; } = string.Empty;
        public LinkStatus Status { get; set; }
        [JsonPropertyName("moderatorStatus")] public LinkModeratorStatus ModeratorStatus { get; set; }
        [JsonPropertyName("clickCnt")] public int Clicks { get; set; }
        [JsonPropertyName("redirectCnt")] public int Redirects { get; set; }
        public string? ExpirationDate { get; set; }
        [JsonPropertyName("realExpirationDate")] public string? RealExpirationDate { get; set; }
        public string? Comment { get; set; }
        [JsonPropertyName("moderatorComment")] public string? ModeratorComment { get; set; }
        [JsonPropertyName("createTs")] public DateTime? Created { get; set; }
        [JsonPropertyName("updateTs")] public DateTime? Updated { get; set; }
    }
}
```

- [ ] **Step 5: Register the enum converters.**

In `src/Mobizon.Net/Internal/MobizonApiClient.cs`, the `Converters` collection (inside `JsonOptions`) already lists `new StringToNumericEnumConverter<CampaignType>()` and `new StringToNumericEnumConverter<StopListLevel>()`. Add two lines next to them:

```csharp
                new StringToNumericEnumConverter<Mobizon.Contracts.Models.Links.LinkStatus>(),
                new StringToNumericEnumConverter<Mobizon.Contracts.Models.Links.LinkModeratorStatus>(),
```

- [ ] **Step 6: Run the link read tests to verify they pass, then the full suite.**

Run: `dotnet test --filter "FullyQualifiedName~LinkServiceTests"` → PASS. Then `dotnet test` → PASS.

- [ ] **Step 7: Commit.**

```bash
git add -A
git commit -m "refactor: LinkData read side — Created/Updated DateTime, Clicks/Redirects, LinkStatus/LinkModeratorStatus enums"
```

---

## Task 3: Link requests & criteria — write-side types

**Files:**
- Modify: `src/Mobizon.Contracts/Models/Links/CreateLinkRequest.cs`
- Modify: `src/Mobizon.Contracts/Models/Links/UpdateLinkRequest.cs`
- Modify: `src/Mobizon.Contracts/Models/Links/LinkListCriteria.cs`
- Modify: `src/Mobizon.Net/Services/LinkService.cs` (`CreateAsync`, `UpdateAsync`, `ListAsync` param building)
- Test: `tests/Mobizon.Net.Tests/Services/LinkServiceTests.cs`

**Interfaces:**
- Consumes: `LinkStatus`, `LinkModeratorStatus` (Task 2).
- Produces: `CreateLinkRequest.Status` / `UpdateLinkRequest.Status` are `LinkStatus?`; `LinkListCriteria` exposes `LinkStatus? Status`, `LinkModeratorStatus? ModeratorStatus`, `DateTime? CreatedFrom`, `DateTime? CreatedTo`, `int? ClicksFrom`, `int? ClicksTo`.

- [ ] **Step 1: Update the write tests (TDD red).**

In `LinkServiceTests.cs`, the create/update/list tests assert form data. Update them to pass the new typed values and keep asserting the SAME wire strings. Worked example for create with status:

```csharp
            await service.CreateAsync(new CreateLinkRequest { FullLink = "https://example.com", Status = LinkStatus.Active });
            // still emits data[status]=1
```

And a list-criteria test asserting the datetime + status + clicks filters serialize to the prior wire strings:

```csharp
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/link/list")
                .WithFormData("criteria[status]", "1")
                .WithFormData("criteria[moderatorStatus]", "1")
                .WithFormData("criteria[createTsFrom]", "2026-01-02 03:04:05")
                .WithFormData("criteria[clickCntFrom]", "5")
                .Respond("application/json", @"{""code"":0,""data"":{""items"":[],""totalItemCount"":""0""},""message"":""""}");
            await service.ListAsync(new LinkListRequest { Criteria = new LinkListCriteria {
                Status = LinkStatus.Active, ModeratorStatus = LinkModeratorStatus.Approved,
                CreatedFrom = new System.DateTime(2026,1,2,3,4,5), ClicksFrom = 5 } });
            mockHttp.VerifyNoOutstandingExpectation();
```

- [ ] **Step 2: Run to verify failure.**

Run: `dotnet test --filter "FullyQualifiedName~LinkServiceTests"`
Expected: FAIL — compile errors (`Status` is now `LinkStatus?`, criteria members renamed).

- [ ] **Step 3: Retype the request/criteria models.**

In `CreateLinkRequest.cs` change `public int? Status` → `public Mobizon.Contracts.Models.Links.LinkStatus? Status` (it's already in that namespace, so just `public LinkStatus? Status`). Same in `UpdateLinkRequest.cs`.

Replace `LinkListCriteria.cs` with:

```csharp
using System;

namespace Mobizon.Contracts.Models.Links
{
    /// <summary>Filter criteria for <c>link/list</c>.</summary>
    public class LinkListCriteria
    {
        public LinkStatus? Status { get; set; }
        public LinkModeratorStatus? ModeratorStatus { get; set; }
        public string? Code { get; set; }
        public string? FullLink { get; set; }
        public string? Comment { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public int? ClicksFrom { get; set; }
        public int? ClicksTo { get; set; }
    }
}
```

- [ ] **Step 4: Update `LinkService` param building.**

Add `using System.Globalization;` at the top of `LinkService.cs` if absent. In `CreateAsync`/`UpdateAsync`, the status parameter is currently `parameters["data[status]"] = request.Status.Value.ToString();` — change to cast the enum to int:

```csharp
            if (request.Status.HasValue)
                parameters["data[status]"] = ((int)request.Status.Value).ToString(CultureInfo.InvariantCulture);
```

In `ListAsync`, the criteria block currently reads `c.Status`, `c.ModeratorStatus`, `c.Code`, `c.FullLink`, `c.Comment`, `c.CreateTsFrom`, `c.CreateTsTo`, `c.ClickCntFrom`, `c.ClickCntTo`. Replace those lines with:

```csharp
                    if (c.Status.HasValue) parameters["criteria[status]"] = ((int)c.Status.Value).ToString(CultureInfo.InvariantCulture);
                    if (c.ModeratorStatus.HasValue) parameters["criteria[moderatorStatus]"] = ((int)c.ModeratorStatus.Value).ToString(CultureInfo.InvariantCulture);
                    if (c.Code != null) parameters["criteria[code]"] = c.Code;
                    if (c.FullLink != null) parameters["criteria[fullLink]"] = c.FullLink;
                    if (c.Comment != null) parameters["criteria[comment]"] = c.Comment;
                    if (c.CreatedFrom.HasValue) parameters["criteria[createTsFrom]"] = c.CreatedFrom.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    if (c.CreatedTo.HasValue) parameters["criteria[createTsTo]"] = c.CreatedTo.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    if (c.ClicksFrom.HasValue) parameters["criteria[clickCntFrom]"] = c.ClicksFrom.Value.ToString(CultureInfo.InvariantCulture);
                    if (c.ClicksTo.HasValue) parameters["criteria[clickCntTo]"] = c.ClicksTo.Value.ToString(CultureInfo.InvariantCulture);
```

(The wire keys `criteria[createTsFrom]`, `criteria[clickCntFrom]`, etc. are unchanged — only the C# property names and value formatting changed.)

- [ ] **Step 5: Run link tests then full suite.**

Run: `dotnet test --filter "FullyQualifiedName~LinkServiceTests"` → PASS. Then `dotnet test` → PASS.

- [ ] **Step 6: Commit.**

```bash
git add -A
git commit -m "refactor: Link write side — LinkStatus/ModeratorStatus params, CreatedFrom/To DateTime, ClicksFrom/To"
```

---

## Task 4: Campaign list filter + `CampaignInfo` timestamp

**Files:**
- Modify: `src/Mobizon.Contracts/Models/Campaigns/CampaignListRequest.cs` (the `CampaignCriteria` class)
- Modify: `src/Mobizon.Contracts/Models/Campaigns/CampaignInfo.cs` (`UpdateTs` → `Updated`)
- Modify: `src/Mobizon.Net/Services/CampaignService.cs` (`ListAsync` criteria building)
- Test: `tests/Mobizon.Net.Tests/Services/CampaignServiceTests.cs`

**Interfaces:**
- Consumes: existing `CampaignCommonStatus` enum and `ApiStatusCodes.ToApiCode(CampaignCommonStatus)`.
- Produces: `CampaignCriteria` exposes `CampaignCommonStatus? Status`, `DateTime? CreatedFrom`, `DateTime? CreatedTo`, `DateTime? SentFrom`, `DateTime? SentTo`. `CampaignInfo.Updated` (was `UpdateTs`).

- [ ] **Step 1: Update tests (TDD red).**

In `CampaignServiceTests.cs`, update the list-criteria test(s) to use the typed filter and assert the SAME wire strings:

```csharp
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/Campaign/List")
                .WithFormData("criteria[status]", "READY_FOR_SEND")
                .WithFormData("criteria[createTsFrom]", "2026-01-02 03:04:05")
                .WithFormData("criteria[sentTsTo]", "2026-02-03 04:05:06")
                .Respond("application/json", @"{""code"":0,""data"":{""items"":[],""totalItemCount"":""0""},""message"":""""}");
            await service.ListAsync(new CampaignListRequest { Criteria = new CampaignCriteria {
                Status = CampaignCommonStatus.ReadyForSend,
                CreatedFrom = new System.DateTime(2026,1,2,3,4,5),
                SentTo = new System.DateTime(2026,2,3,4,5,6) } });
            mockHttp.VerifyNoOutstandingExpectation();
```

If a `CampaignInfo` GetInfo test asserts `UpdateTs`, rename that assertion to `Updated`.

- [ ] **Step 2: Run to verify failure.**

Run: `dotnet test --filter "FullyQualifiedName~CampaignServiceTests"`
Expected: FAIL — compile errors.

- [ ] **Step 3: Retype `CampaignCriteria`.**

In `CampaignListRequest.cs`, add `using System;` and `using Mobizon.Contracts.Models.Common;` (the latter may already be present). Replace the four timestamp string properties and the `Status` property in `CampaignCriteria`:

```csharp
        /// <summary>Gets or sets the campaign status to filter by.</summary>
        public CampaignCommonStatus? Status { get; set; }

        /// <summary>Gets or sets the lower bound of the campaign creation date range.</summary>
        public DateTime? CreatedFrom { get; set; }

        /// <summary>Gets or sets the upper bound of the campaign creation date range.</summary>
        public DateTime? CreatedTo { get; set; }

        /// <summary>Gets or sets the lower bound of the campaign send date range.</summary>
        public DateTime? SentFrom { get; set; }

        /// <summary>Gets or sets the upper bound of the campaign send date range.</summary>
        public DateTime? SentTo { get; set; }
```

(`CampaignCommonStatus` lives in `Mobizon.Contracts.Models.Campaigns`, same namespace as `CampaignCriteria`, so no extra using is needed for it.)

- [ ] **Step 4: Rename `CampaignInfo.UpdateTs` → `Updated`.**

In `CampaignInfo.cs`, change `public DateTime? UpdateTs { get; set; }` to `public DateTime? Updated { get; set; }`, preserving any `[JsonPropertyName]` it already carries (if none, add `[JsonPropertyName("updateTs")]` so the wire key is preserved — verify the current attribute first with `git grep -n "UpdateTs" -- src/Mobizon.Contracts/Models/Campaigns/CampaignInfo.cs`).

- [ ] **Step 5: Update `CampaignService.ListAsync` criteria building.**

Add `using System.Globalization;` to `CampaignService.cs` if absent. In `ListAsync`, the criteria block currently sets `criteria[status]`, `criteria[createTsFrom]`, `criteria[createTsTo]`, `criteria[sentTsFrom]`, `criteria[sentTsTo]` from string properties. Replace those five with:

```csharp
                    if (c.Status.HasValue)
                        parameters["criteria[status]"] = ApiStatusCodes.ToApiCode(c.Status.Value);

                    if (c.CreatedFrom.HasValue)
                        parameters["criteria[createTsFrom]"] = c.CreatedFrom.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    if (c.CreatedTo.HasValue)
                        parameters["criteria[createTsTo]"] = c.CreatedTo.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    if (c.SentFrom.HasValue)
                        parameters["criteria[sentTsFrom]"] = c.SentFrom.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    if (c.SentTo.HasValue)
                        parameters["criteria[sentTsTo]"] = c.SentTo.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
```

`ApiStatusCodes` is in `Mobizon.Net.Internal.Converters` — add `using Mobizon.Net.Internal.Converters;` to `CampaignService.cs` if absent.

- [ ] **Step 6: Run campaign tests then full suite.**

Run: `dotnet test --filter "FullyQualifiedName~CampaignServiceTests"` → PASS. Then `dotnet test` → PASS.

- [ ] **Step 7: Commit.**

```bash
git add -A
git commit -m "refactor: Campaign list filter — CampaignCommonStatus, DateTime ranges; CampaignInfo.Updated"
```

---

## Task 5: AddRecipients params — `bool` flags + `PlaceholderMissingMode`

**Files:**
- Create: `src/Mobizon.Contracts/Models/Campaigns/PlaceholderMissingMode.cs`
- Modify: `src/Mobizon.Contracts/Models/Campaigns/AddRecipientsRequest.cs` (the `AddRecipientsParameters` class)
- Modify: `src/Mobizon.Net/Services/CampaignService.cs` (`AppendParams`, and the multi-batch `Replace` check)
- Test: `tests/Mobizon.Net.Tests/Services/CampaignServiceTests.cs`

**Interfaces:**
- Produces: `enum PlaceholderMissingMode { KeepAsIs=1, Remove=2, Reject=3 }`; `AddRecipientsParameters.Replace` is `bool?`, `.RecipientsFileSkipHeader` is `bool?`, `.PlaceholdersFlag` is `PlaceholderMissingMode?`.

- [ ] **Step 1: Update tests (TDD red).**

Add/adjust an AddRecipients test that sets the params and asserts the SAME wire strings:

```csharp
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/Campaign/AddRecipients")
                .WithFormData("params[replace]", "1")
                .WithFormData("params[placeholdersFlag]", "2")
                .WithFormData("params[recipientsFileSkipHeader]", "1")
                .Respond("application/json", @"{""code"":0,""data"":[{""recipient"":""77001112233"",""code"":0,""messageId"":""1""}],""message"":""""}");
            await service.AddRecipientsAsync(new AddRecipientsRequest {
                CampaignId = 1,
                Recipients = new[] { new RecipientEntry { Recipient = "77001112233" } },
                Parameters = new AddRecipientsParameters {
                    Replace = true, PlaceholdersFlag = PlaceholderMissingMode.Remove, RecipientsFileSkipHeader = true } });
            mockHttp.VerifyNoOutstandingExpectation();
```

- [ ] **Step 2: Run to verify failure.**

Run: `dotnet test --filter "FullyQualifiedName~CampaignServiceTests"`
Expected: FAIL — compile errors.

- [ ] **Step 3: Create the enum.**

```csharp
// src/Mobizon.Contracts/Models/Campaigns/PlaceholderMissingMode.cs
namespace Mobizon.Contracts.Models.Campaigns
{
    /// <summary>
    /// Behaviour when a template campaign is missing values for some placeholders
    /// (the <c>params[placeholdersFlag]</c> AddRecipients option).
    /// </summary>
    public enum PlaceholderMissingMode
    {
        /// <summary>Keep the placeholders in the text unchanged (API value 1, default).</summary>
        KeepAsIs = 1,

        /// <summary>Remove the unfilled placeholders from the text (API value 2).</summary>
        Remove = 2,

        /// <summary>Reject the message with an error (API value 3).</summary>
        Reject = 3
    }
}
```

- [ ] **Step 4: Retype `AddRecipientsParameters`.**

In `AddRecipientsRequest.cs`, change three properties in `AddRecipientsParameters`:

```csharp
        /// <summary>Gets or sets whether to remove all previously added recipients before adding the new ones (default <see langword="false"/>).</summary>
        public bool? Replace { get; set; }

        /// <summary>Gets or sets the behaviour when placeholder values are missing in a template campaign. Default <see cref="PlaceholderMissingMode.KeepAsIs"/>.</summary>
        public PlaceholderMissingMode? PlaceholdersFlag { get; set; }
```

and

```csharp
        /// <summary>Gets or sets whether to skip the first line (header row) of the recipients file (always <see langword="true"/> for template campaigns).</summary>
        public bool? RecipientsFileSkipHeader { get; set; }
```

(`PlaceholderMissingMode` is in the same namespace, so no extra using is required.)

- [ ] **Step 5: Update `CampaignService.AppendParams` + the multi-batch Replace check.**

In `AppendParams`, replace the `Replace`, `PlaceholdersFlag`, and `RecipientsFileSkipHeader` lines:

```csharp
            if (prm.Replace.HasValue)
                parameters["params[replace]"] = prm.Replace.Value ? "1" : "0";

            if (prm.PlaceholdersFlag.HasValue)
                parameters["params[placeholdersFlag]"] = ((int)prm.PlaceholdersFlag.Value).ToString(System.Globalization.CultureInfo.InvariantCulture);
```

and

```csharp
            if (prm.RecipientsFileSkipHeader.HasValue)
                parameters["params[recipientsFileSkipHeader]"] = prm.RecipientsFileSkipHeader.Value ? "1" : "0";
```

Then fix the multi-batch guard in `AddRecipientsAsync` that currently reads `batchParams?.Replace == 1` and builds a copy with `Replace = 0`. Change the comparison and the override value to `bool`:

```csharp
                if (aggregated != null && batchParams?.Replace == true)
                {
                    batchParams = new AddRecipientsParameters
                    {
                        Replace = false,
                        PlaceholdersFlag = request.Parameters!.PlaceholdersFlag,
                        RecipientsFileEncoding = request.Parameters.RecipientsFileEncoding,
                        RecipientsFileSkipHeader = request.Parameters.RecipientsFileSkipHeader,
                        RecipientsFileDelimiter = request.Parameters.RecipientsFileDelimiter,
                        RecipientsFileEnclosure = request.Parameters.RecipientsFileEnclosure
                    };
                }
```

(Find this block around the multi-batch loop; only the `Replace == true` comparison and `Replace = false` assignment change from the old `int` form.)

- [ ] **Step 6: Run campaign tests then full suite.**

Run: `dotnet test --filter "FullyQualifiedName~CampaignServiceTests"` → PASS. Then `dotnet test` → PASS.

- [ ] **Step 7: Commit.**

```bash
git add -A
git commit -m "refactor: AddRecipients params — bool Replace/SkipHeader, PlaceholderMissingMode enum"
```

---

## Task 6: Alphaname — `IsDefault` bool + timestamps

**Files:**
- Modify: `src/Mobizon.Contracts/Models/Alphanames/AlphanameData.cs`
- Test: `tests/Mobizon.Net.Tests/Services/AlphanameServiceTests.cs`

**Interfaces:**
- Produces: `AlphanameData.IsDefault` is `bool`; `AlphanameData.Created` (was `CreateTs`, `DateTime?`); `AlphanameInfo.Created` (was `CreateTs`, `DateTime?`).
- Note: `GlobalStatus`, `PartnerStatus` (on `AlphanameData`) and `Type` (on `AlphanameInfo`) stay `int` — out of scope.

- [ ] **Step 1: Update tests (TDD red).**

In `AlphanameServiceTests.cs`, the list test deserializes alphaname JSON. Update assertions: any `result.Items[0].CreateTs` → `.Created`; assert `IsDefault` as `bool`; if a fixture carries `createTs`/`isDefault`, assert the parsed values:

```csharp
            // include in mock payload: "isDefault":"1","createTs":"2026-01-02 03:04:05"
            Assert.True(item.IsDefault);
            Assert.Equal(new System.DateTime(2026,1,2,3,4,5), item.Created);
```

- [ ] **Step 2: Run to verify failure.**

Run: `dotnet test --filter "FullyQualifiedName~AlphanameServiceTests"`
Expected: FAIL — compile errors.

- [ ] **Step 3: Retype `AlphanameData` / `AlphanameInfo`.**

In `AlphanameData.cs`, change:

```csharp
        [JsonPropertyName("isDefault")] public bool IsDefault { get; set; }
        [JsonPropertyName("createTs")] public DateTime? Created { get; set; }
```

and in the `AlphanameInfo` class:

```csharp
        [JsonPropertyName("createTs")] public DateTime? Created { get; set; }
```

Add `using System;` at the top of the file. Leave `GlobalStatus`, `PartnerStatus`, and `AlphanameInfo.Type` as `int`.

- [ ] **Step 4: Run alphaname tests then full suite.**

Run: `dotnet test --filter "FullyQualifiedName~AlphanameServiceTests"` → PASS. Then `dotnet test` → PASS.

- [ ] **Step 5: Commit.**

```bash
git add -A
git commit -m "refactor: AlphanameData/Info — bool IsDefault, Created DateTime"
```

---

## Task 7: `MessageInfo.SegUserBuy` → `SegmentCost`

**Files:**
- Modify: `src/Mobizon.Contracts/Models/Messages/MessageInfo.cs:31-32`
- Test: `tests/Mobizon.Net.Tests/Services/MessageServiceTests.cs`

**Interfaces:**
- Produces: `MessageInfo.SegmentCost` (decimal, was `SegUserBuy`).

- [ ] **Step 1: Update tests (TDD red).**

In `MessageServiceTests.cs`, any assertion on `.SegUserBuy` becomes `.SegmentCost`. If the message list/info test fixture carries `segUserBuy`, assert the parsed decimal:

```csharp
            Assert.Equal(0.06m, item.SegmentCost);
```

- [ ] **Step 2: Run to verify failure.**

Run: `dotnet test --filter "FullyQualifiedName~MessageServiceTests"`
Expected: FAIL — compile error (`SegUserBuy` not found).

- [ ] **Step 3: Rename the property.**

In `MessageInfo.cs`:

```csharp
        [JsonPropertyName("segUserBuy")]
        public decimal SegmentCost { get; set; }
```

(Keep the `[JsonPropertyName("segUserBuy")]` and update the XML-doc summary to "Gets or sets the per-segment cost in the user's currency.")

- [ ] **Step 4: Run message tests then full suite.**

Run: `dotnet test --filter "FullyQualifiedName~MessageServiceTests"` → PASS. Then `dotnet test` → PASS.

- [ ] **Step 5: Commit.**

```bash
git add -A
git commit -m "refactor: rename MessageInfo.SegUserBuy to SegmentCost"
```

---

## Task 8: Webhook `SmsDeliveryReport.SegNum` → `Segments`

**Files:**
- Modify: `src/Mobizon.Contracts/Models/Webhooks/SmsDeliveryReport.cs:18-19`
- Test: the existing webhook test that parses an SMS delivery report (find with `git grep -ln "SmsDeliveryReport" -- tests`)

**Interfaces:**
- Produces: `SmsDeliveryReport.Segments` (int, was `SegNum`).

- [ ] **Step 1: Find the webhook deserialization options and the covering test.**

Run: `git grep -n "SegNum" -- tests` and `git grep -rln "JsonSerializerOptions\|PropertyNameCaseInsensitive\|JsonNamingPolicy" -- src/Mobizon.Net.Webhooks`
This tells you (a) which test asserts `SegNum` and (b) how the webhook payload is deserialized (case-insensitive match, naming policy, or explicit attributes). `SmsDeliveryReport.SegNum` currently has NO `[JsonPropertyName]`, so its wire key is matched by name/policy — renaming to `Segments` WILL break parsing unless an explicit mapping is added.

- [ ] **Step 2: Update the covering test (TDD red).**

In the webhook test that parses a delivery report, change the assertion `report.SegNum` → `report.Segments` (keep the same payload, which contains `"segNum": N`).

- [ ] **Step 3: Run to verify failure.**

Run: `dotnet test --filter "FullyQualifiedName~Webhook"` (or the specific webhook test project filter)
Expected: FAIL — compile error.

- [ ] **Step 4: Rename with an explicit wire mapping.**

In `SmsDeliveryReport.cs`, add `using System.Text.Json.Serialization;` if absent and change:

```csharp
        /// <summary>Number of segments the message was split into.</summary>
        [JsonPropertyName("segNum")]
        public int Segments { get; set; }
```

- [ ] **Step 5: Run webhook tests then full suite.**

Run: `dotnet test --filter "FullyQualifiedName~Webhook"` → PASS (the `[JsonPropertyName("segNum")]` keeps `"segNum"` deserializing into `Segments`). Then `dotnet test` → PASS.

- [ ] **Step 6: Commit.**

```bash
git add -A
git commit -m "refactor: rename SmsDeliveryReport.SegNum to Segments (keep segNum wire key)"
```

---

## Task 9: Samples, README, CHANGELOG + final verification

**Files:**
- Modify: `samples/Mobizon.Net.ConsoleSample/Samples/*.cs` (any references to renamed members)
- Modify: `README.md`
- Modify: `CHANGELOG.md`

- [ ] **Step 1: Sweep samples for renamed members.**

Run: `git grep -n "ClickCnt\|RedirectCnt\|CreateTs\|UpdateTs\|SegUserBuy\|SegNum\|\.Status\b\|PlaceholdersFlag\|AddRecipientsResponseCode\|IsDefault" -- samples`
For each hit, update to the new name/type: `.ClickCnt`→`.Clicks`, `.RedirectCnt`→`.Redirects`, `.CreateTs`→`.Created`, `.UpdateTs`→`.Updated`, `.SegUserBuy`→`.SegmentCost`, link `Status = 1`→`Status = LinkStatus.Active`, `Replace = 1`→`Replace = true`, `PlaceholdersFlag = 2`→`PlaceholdersFlag = PlaceholderMissingMode.Remove`. Add the needed `using Mobizon.Contracts.Models.Links;` / `...Campaigns;` where a sample now references the new enums.

- [ ] **Step 2: Update README examples.**

Run: `git grep -n "ClickCnt\|RedirectCnt\|CreateTs\|UpdateTs\|SegUserBuy\|SegNum\|AddRecipientsResponseCode" -- README.md`
Update each example to the new member names/types. Where the README shows a link-status or placeholders example as a raw int, switch it to the enum.

- [ ] **Step 3: Add a CHANGELOG entry.**

In `CHANGELOG.md` under `## [Unreleased]` → `### Breaking`, add:

```markdown
- Idiomatic model names & types: `LinkData.Created`/`Updated` (`DateTime?`, were `CreateTs`/`UpdateTs` strings), `LinkData.Clicks`/`Redirects` (were `ClickCnt`/`RedirectCnt`), `LinkData.Status`/`ModeratorStatus` now `LinkStatus`/`LinkModeratorStatus` enums; `AlphanameData.Created` + `IsDefault` (`bool`); `AlphanameInfo.Created`; `CampaignInfo.Updated`; `MessageInfo.SegmentCost` (was `SegUserBuy`); `SmsDeliveryReport.Segments` (was `SegNum`)
- Filter/request types: `LinkListCriteria.Status`/`ModeratorStatus` enums, `CreatedFrom`/`CreatedTo` (`DateTime?`), `ClicksFrom`/`ClicksTo`; `CreateLinkRequest`/`UpdateLinkRequest.Status` now `LinkStatus?`; `CampaignCriteria.Status` now `CampaignCommonStatus?`, `CreatedFrom`/`CreatedTo`/`SentFrom`/`SentTo` (`DateTime?`); `AddRecipientsParameters.Replace`/`RecipientsFileSkipHeader` now `bool?`, `PlaceholdersFlag` now `PlaceholderMissingMode`
- Removed duplicate `AddRecipientsResponseCode` enum — use `AddRecipientsOutcome`
```

- [ ] **Step 4: Final verification.**

Run: `dotnet build && dotnet test` → build 0 warnings, 243+ tests PASS. Then `dotnet build samples/Mobizon.Net.ConsoleSample/Mobizon.Net.ConsoleSample.csproj` → 0 errors. Then `git grep -n "ClickCnt\|RedirectCnt\|SegUserBuy\|AddRecipientsResponseCode" -- src samples README.md` → only intentional `[JsonPropertyName("clickCnt")]`/`("redirectCnt")`/`("segUserBuy")` wire mappings remain (report them).

- [ ] **Step 5: Commit.**

```bash
git add -A
git commit -m "docs: update samples, README, CHANGELOG for naming & types refactor"
```

---

## Self-Review notes

- **Spec coverage:** Block 1 → Tasks 2,3,4,6 (LinkData/Alphaname/CampaignInfo timestamps + Campaign/Link filter timestamps); Block 2 → Tasks 2,3,7,8 (Clicks/Redirects/SegmentCost/Segments); Block 3 → Tasks 2,3,5 (LinkStatus/ModeratorStatus/PlaceholderMissingMode); Block 4 → Task 4 (CampaignCommonStatus); Block 5 → Tasks 5,6 (bool flags); Block 6 → Task 1 (dedup); Block 7 (out of scope) → respected (Gender untouched, alphaname statuses left int, ExpirationDate left string).
- **Wire-key preservation:** every rename either keeps an existing `[JsonPropertyName]` or adds one (CampaignInfo.UpdateTs verify in Task 4 Step 4; SmsDeliveryReport.SegNum adds it in Task 8 Step 4).
- **Open verification points (resolve during execution):** Task 4 — confirm `CampaignInfo`'s current `[JsonPropertyName]` on the timestamp; Task 8 — confirm the webhook deserialization options and the exact covering test name. Both are local lookups, not design decisions.

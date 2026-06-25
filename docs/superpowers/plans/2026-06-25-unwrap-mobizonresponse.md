# Unwrap `MobizonResponse<T>` Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove `MobizonResponse<T>` from the public service surface so every service method returns its domain type directly; errors continue to surface as `MobizonApiException`.

**Architecture:** `MobizonResponse<T>` becomes an `internal` type living in `Mobizon.Net/Internal`, used only by `MobizonApiClient` (deserialization + throw decision) and the contact-card fluent query. Each service implementation unwraps `.Data` before returning. Two campaign methods fold their meaningful response code into domain types (`CampaignSendResult.IsQueued`, `AddRecipientsResult.Outcome`).

**Tech Stack:** C# 8.0 / netstandard2.0 (core) + net8.0 (sample), System.Text.Json, xUnit + RichardSzalay.MockHttp for tests.

## Global Constraints

- Core packages target `netstandard2.0`; sample/ASP.NET targets `net8.0`. No new third-party dependencies.
- C# 8.0 language features only (nullable reference types are enabled in the projects).
- Errors are signalled by throwing `MobizonApiException` — never by a return value. Success = no exception thrown.
- `MobizonResponseCode` stays **public** (exposed by `MobizonApiException.Code`). Only the `MobizonResponse<T>` wrapper becomes internal.
- Build/test command for the whole solution: `dotnet test` from repo root `C:\Develop\Mobizon.Net`.
- Follow existing file conventions: services are `internal` classes in `src/Mobizon.Net/Services/`, interfaces are `public` in `src/Mobizon.Contracts/Services/`.

---

## Transform Rules (shared conventions — referenced by every service task)

These three mechanical rules define the refactor. They are part of this plan; tasks apply them by name.

**Rule U (Unwrap to T)** — for a data-bearing method currently returning `Task<MobizonResponse<T>>`:
- Interface: change return type to `Task<T>`.
- Implementation: if the body is `return _apiClient.SendAsync<T>(...);`, change it to
  `async Task<T>` and `return (await _apiClient.SendAsync<T>(...).ConfigureAwait(false)).Data;`
  Example (Link.GetByIdAsync):
  ```csharp
  // before
  public Task<MobizonResponse<LinkData>> GetByIdAsync(long id, CancellationToken cancellationToken = default)
      => _apiClient.SendAsync<LinkData>(HttpMethod.Post, ModuleName, "get",
          new Dictionary<string, string> { ["id"] = id.ToString() }, cancellationToken);
  // after
  public async Task<LinkData> GetByIdAsync(long id, CancellationToken cancellationToken = default)
      => (await _apiClient.SendAsync<LinkData>(HttpMethod.Post, ModuleName, "get",
          new Dictionary<string, string> { ["id"] = id.ToString() }, cancellationToken).ConfigureAwait(false)).Data;
  ```

**Rule V (Unwrap to void)** — for a no-payload method currently returning `Task<MobizonResponse<object>>` or `Task<MobizonResponse<bool>>`:
- Interface: change return type to `Task`.
- Implementation: change to `async Task`, `await` the call, discard the result. Example:
  ```csharp
  // before
  public Task<MobizonResponse<object>> DeleteAsync(long[] ids, CancellationToken cancellationToken = default)
  { /* build parameters */ return _apiClient.SendAsync<object>(HttpMethod.Post, ModuleName, "delete", parameters, cancellationToken); }
  // after
  public async Task DeleteAsync(long[] ids, CancellationToken cancellationToken = default)
  { /* build parameters */ await _apiClient.SendAsync<object>(HttpMethod.Post, ModuleName, "delete", parameters, cancellationToken).ConfigureAwait(false); }
  ```

**Rule T (Test update)** — for every affected test:
- Delete any `Assert.Equal(MobizonResponseCode.Success, result.Code);` line (success is now implied by no exception).
- Replace `result.Data.X` with `result.X`; replace `result.Data` used as the value with `result`.
- For Rule-V methods, change `var result = await service.M(...);` to `await service.M(...);` and drop result-based asserts (keep `mockHttp.VerifyNoOutstandingExpectation();`).
- Error-path tests that assert `MobizonApiException` are unchanged.

---

## Task 1: New campaign result types

**Files:**
- Create: `src/Mobizon.Contracts/Models/Campaigns/CampaignSendResult.cs`
- Create: `src/Mobizon.Contracts/Models/Campaigns/AddRecipientsOutcome.cs`
- Modify: `src/Mobizon.Contracts/Models/Campaigns/AddRecipientsResult.cs`
- Test: `tests/Mobizon.Net.Tests/Models/CampaignSendResultTests.cs`

**Interfaces:**
- Produces: `CampaignSendResult { bool IsQueued; long Id; }`; `enum AddRecipientsOutcome { AllAdded, PartiallyAdded, NoneAdded }`; `AddRecipientsResult.Outcome` property of type `AddRecipientsOutcome`.

- [ ] **Step 1: Write the failing test**

```csharp
// tests/Mobizon.Net.Tests/Models/CampaignSendResultTests.cs
using Mobizon.Contracts.Models.Campaigns;
using Xunit;

namespace Mobizon.Net.Tests.Models
{
    public class CampaignSendResultTests
    {
        [Fact]
        public void CampaignSendResult_DefaultsToNotQueued()
        {
            var r = new CampaignSendResult();
            Assert.False(r.IsQueued);
            Assert.Equal(0L, r.Id);
        }

        [Fact]
        public void AddRecipientsResult_DefaultsToAllAdded()
        {
            var r = new AddRecipientsResult();
            Assert.Equal(AddRecipientsOutcome.AllAdded, r.Outcome);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~CampaignSendResultTests"`
Expected: FAIL — `CampaignSendResult` / `AddRecipientsOutcome` / `Outcome` do not exist (compile error).

- [ ] **Step 3: Create the new types and property**

```csharp
// src/Mobizon.Contracts/Models/Campaigns/CampaignSendResult.cs
namespace Mobizon.Contracts.Models.Campaigns
{
    /// <summary>
    /// Result of scheduling a campaign for sending via <c>campaign/send</c>.
    /// </summary>
    public class CampaignSendResult
    {
        /// <summary>
        /// True when the API queued the send as a background task (response code 100).
        /// When true, <see cref="Id"/> is the task id trackable via <c>TaskQueue/GetStatus</c>.
        /// </summary>
        public bool IsQueued { get; set; }

        /// <summary>
        /// The background task id when <see cref="IsQueued"/> is true; otherwise the
        /// synchronous send result returned by the API.
        /// </summary>
        public long Id { get; set; }
    }
}
```

```csharp
// src/Mobizon.Contracts/Models/Campaigns/AddRecipientsOutcome.cs
namespace Mobizon.Contracts.Models.Campaigns
{
    /// <summary>
    /// Top-level outcome of <c>campaign/addRecipients</c>, derived from the API response code
    /// (0 = all added, 98 = partially added, 99 = none added).
    /// </summary>
    public enum AddRecipientsOutcome
    {
        /// <summary>All recipients were added (response code 0), or the load was accepted as a background task.</summary>
        AllAdded = 0,

        /// <summary>Some recipients were added; others were rejected (response code 98).</summary>
        PartiallyAdded = 98,

        /// <summary>No recipients were added; all entries were rejected (response code 99).</summary>
        NoneAdded = 99
    }
}
```

Add the property to `AddRecipientsResult` (in `AddRecipientsResult.cs`, after the `Entries` property, before `MergeEntries`):

```csharp
        /// <summary>
        /// Gets or sets the top-level outcome of the operation, derived from the API response code.
        /// For multi-batch sends this reflects the worst-case outcome across batches.
        /// </summary>
        public AddRecipientsOutcome Outcome { get; set; }
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter "FullyQualifiedName~CampaignSendResultTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Mobizon.Contracts/Models/Campaigns/CampaignSendResult.cs src/Mobizon.Contracts/Models/Campaigns/AddRecipientsOutcome.cs src/Mobizon.Contracts/Models/Campaigns/AddRecipientsResult.cs tests/Mobizon.Net.Tests/Models/CampaignSendResultTests.cs
git commit -m "feat: add CampaignSendResult and AddRecipientsOutcome types"
```

---

## Task 2: LinkService

**Files:**
- Modify: `src/Mobizon.Contracts/Services/ILinkService.cs`
- Modify: `src/Mobizon.Net/Services/LinkService.cs`
- Test: `tests/Mobizon.Net.Tests/Services/LinkServiceTests.cs`

**Interfaces:**
- Produces (new `ILinkService` signatures):
  - `Task<LinkData> CreateAsync(CreateLinkRequest, CancellationToken = default)`
  - `Task DeleteAsync(long[] ids, CancellationToken = default)`
  - `Task<LinkData> GetByIdAsync(long, CancellationToken = default)`
  - `Task<LinkData> GetByCodeAsync(string, CancellationToken = default)`
  - `Task<LinkData> GetByShortLinkAsync(string, CancellationToken = default)`
  - `Task<IReadOnlyList<LinkData>> GetLinksAsync(long campaignId, CancellationToken = default)`
  - `Task<LinkStatsResult> GetStatsAsync(GetLinkStatsRequest, CancellationToken = default)`
  - `Task<MobizonListResult<LinkData>> ListAsync(LinkListRequest? = null, CancellationToken = default)`
  - `Task UpdateAsync(UpdateLinkRequest, CancellationToken = default)`

- [ ] **Step 1: Update the test file (Rule T)**

Apply Rule T to every test in `LinkServiceTests.cs`. Remove all `Assert.Equal(MobizonResponseCode.Success, result.Code);`. Change `result.Data.Id` → `result.Id`, etc. For `DeleteAsync`/`UpdateAsync` tests, drop the `var result =` capture and the `result.Code` assert, keeping the call and `VerifyNoOutstandingExpectation()`. Worked example for `CreateAsync_SendsCorrectParameters`:

```csharp
            var result = await service.CreateAsync(new CreateLinkRequest
            {
                FullLink = "https://example.com"
            });

            Assert.Equal(1, result.Id);
            Assert.Equal("abc123", result.Code);
            Assert.Equal("https://example.com", result.FullLink);
            mockHttp.VerifyNoOutstandingExpectation();
```

For `GetStatsAsync` tests, change `result.Data.Links` → `result.Links` (and any series asserts accordingly).

- [ ] **Step 2: Run tests to verify they fail to compile**

Run: `dotnet test --filter "FullyQualifiedName~LinkServiceTests"`
Expected: FAIL — compile errors (`LinkData` has no member `Data`; `ILinkService` returns differ).

- [ ] **Step 3: Update the interface**

In `ILinkService.cs`, change each method's return type per the **Interfaces** block above. Keep the XML-doc but update `<returns>` text to describe the domain type (e.g. "The created <see cref="LinkData"/>."). For `DeleteAsync`/`UpdateAsync`, `<returns>` becomes "A task that completes when the operation succeeds."

- [ ] **Step 4: Update the implementation**

Apply **Rule U** to `CreateAsync`, `GetByIdAsync`, `GetByCodeAsync`, `GetByShortLinkAsync`, `GetLinksAsync`, `ListAsync`. Apply **Rule V** to `DeleteAsync`, `UpdateAsync`. `GetStatsAsync` is special (it already `await`s and post-processes) — change its signature and final return:

```csharp
        public async Task<LinkStatsResult> GetStatsAsync(
            GetLinkStatsRequest request, CancellationToken cancellationToken = default)
        {
            // ... unchanged parameter building ...
            var response = await _apiClient.SendAsync<LinkStatsResult>(
                HttpMethod.Post, ModuleName, "getstats", parameters, cancellationToken).ConfigureAwait(false);

            if (response.Data?.Links != null)
            {
                foreach (var series in response.Data.Links)
                {
                    if (series.Index >= 0 && series.Index < request.Ids.Length)
                        series.LinkId = request.Ids[series.Index];
                }
            }

            return response.Data;
        }
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~LinkServiceTests"`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/Mobizon.Contracts/Services/ILinkService.cs src/Mobizon.Net/Services/LinkService.cs tests/Mobizon.Net.Tests/Services/LinkServiceTests.cs
git commit -m "refactor: unwrap MobizonResponse from LinkService"
```

---

## Task 3: CampaignService (incl. SendAsync + AddRecipients fold)

**Files:**
- Modify: `src/Mobizon.Contracts/Services/ICampaignService.cs`
- Modify: `src/Mobizon.Net/Services/CampaignService.cs`
- Test: `tests/Mobizon.Net.Tests/Services/CampaignServiceTests.cs`

**Interfaces:**
- Consumes: `CampaignSendResult`, `AddRecipientsOutcome`, `AddRecipientsResult.Outcome` (Task 1).
- Produces (new `ICampaignService` signatures):
  - `Task<long> CreateAsync(CreateCampaignRequest, CancellationToken = default)`
  - `Task DeleteAsync(long id, CancellationToken = default)`
  - `Task<CampaignData> GetAsync(long, CancellationToken = default)`
  - `Task<CampaignInfo> GetInfoAsync(long, int? getFilledTplCampaignText = null, CancellationToken = default)`
  - `Task<MobizonListResult<CampaignData>> ListAsync(CampaignListRequest? = null, CancellationToken = default)`
  - `Task<CampaignSendResult> SendAsync(long id, CancellationToken = default)`
  - `Task<AddRecipientsResult> AddRecipientsAsync(AddRecipientsRequest, CancellationToken = default)`

- [ ] **Step 1: Update the test file (Rule T) and add code-fold tests**

Apply Rule T to all existing `CampaignServiceTests`. Then update the `SendAsync` tests and add an `IsQueued` case. The existing send test (sync path, code 0) should assert:

```csharp
            var result = await service.SendAsync(42);
            Assert.False(result.IsQueued);
            Assert.Equal(2L, result.Id); // whatever the sync payload returns
            mockHttp.VerifyNoOutstandingExpectation();
```

Add a queued-path test:

```csharp
        [Fact]
        public async Task SendAsync_BackgroundTask_SetsIsQueued()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/Campaign/Send")
                .WithFormData("id", "42")
                .Respond("application/json", @"{""code"":100,""data"":777,""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.SendAsync(42);

            Assert.True(result.IsQueued);
            Assert.Equal(777L, result.Id);
            mockHttp.VerifyNoOutstandingExpectation();
        }
```

For `AddRecipientsAsync` tests, change `result.Data.Entries` → `result.Entries`, `result.Data.TaskId` → `result.TaskId`, and add `Outcome` asserts. Add a partial-failure case:

```csharp
        [Fact]
        public async Task AddRecipientsAsync_PartialFailure_SetsOutcomePartiallyAdded()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/Campaign/AddRecipients")
                .Respond("application/json",
                    @"{""code"":98,""data"":[{""recipient"":""77011234567"",""result"":0,""messageId"":1},{""recipient"":""bad"",""result"":3}],""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.AddRecipientsAsync(new AddRecipientsRequest
            {
                CampaignId = 1,
                Recipients = new[] { new RecipientEntry { Recipient = "77011234567" }, new RecipientEntry { Recipient = "bad" } }
            });

            Assert.Equal(AddRecipientsOutcome.PartiallyAdded, result.Outcome);
            mockHttp.VerifyNoOutstandingExpectation();
        }
```

(If `RecipientEntry`'s property names differ, match the existing test file's usage.)

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~CampaignServiceTests"`
Expected: FAIL — compile errors + missing `IsQueued`/`Outcome`.

- [ ] **Step 3: Update the interface**

In `ICampaignService.cs` apply the **Interfaces** block. Update `<returns>` docs: `SendAsync` → "A <see cref=\"CampaignSendResult\"/>; when <c>IsQueued</c> is true, <c>Id</c> is the background task id."; `AddRecipientsAsync` → describe `Outcome` + `Entries`/`TaskId`.

- [ ] **Step 4: Update the implementation**

Apply **Rule U** to `CreateAsync`, `GetAsync`, `GetInfoAsync`, `ListAsync`. Apply **Rule V** to `DeleteAsync`.

Rewrite `SendAsync`:

```csharp
        public async Task<CampaignSendResult> SendAsync(
            long id, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string> { ["id"] = id.ToString() };

            var response = await _apiClient.SendAsync<long>(
                HttpMethod.Post, ModuleName, "Send", parameters, cancellationToken).ConfigureAwait(false);

            return new CampaignSendResult
            {
                IsQueued = response.Code == MobizonResponseCode.BackgroundTask,
                Id = response.Data
            };
        }
```

For `AddRecipientsAsync`: the method already returns `MobizonResponse<AddRecipientsResult>` and the internal helper `SendAddRecipientsAsync` returns the same. Keep the helper internal-typed (returning `MobizonResponse<AddRecipientsResult>`), but change the public method to return `AddRecipientsResult` with `Outcome` set from the final response code. Add a private mapper and apply it at each return point:

```csharp
        private static AddRecipientsResult Finalize(MobizonResponse<AddRecipientsResult> response)
        {
            var result = response.Data ?? new AddRecipientsResult();
            result.Outcome = response.RawCode == 98 ? AddRecipientsOutcome.PartiallyAdded
                           : response.RawCode == 99 ? AddRecipientsOutcome.NoneAdded
                           : AddRecipientsOutcome.AllAdded; // 0 or 100 (async accepted)
            return result;
        }
```

Change the public method signature to `async Task<AddRecipientsResult>` and wrap each of its three `return`s:
  - file path: `return Finalize(await _apiClient.SendMultipartAsync<AddRecipientsResult>(...).ConfigureAwait(false));`
  - groups/single-batch path: `return Finalize(await SendAddRecipientsAsync(request, cancellationToken).ConfigureAwait(false));`
  - multi-batch path: change the final `return aggregated!;` to `return Finalize(aggregated!);`

Leave `SendAddRecipientsAsync`, `MergeEntries`, the batch loop, and the worst-case `RawCode` aggregation (`if (response.RawCode > aggregated.RawCode) aggregated.RawCode = response.RawCode;`) unchanged — `Finalize` reads that aggregated `RawCode`.

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~CampaignServiceTests"`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/Mobizon.Contracts/Services/ICampaignService.cs src/Mobizon.Net/Services/CampaignService.cs tests/Mobizon.Net.Tests/Services/CampaignServiceTests.cs
git commit -m "refactor: unwrap CampaignService; fold codes into CampaignSendResult and Outcome"
```

---

## Task 4: MessageService

**Files:**
- Modify: `src/Mobizon.Contracts/Services/IMessageService.cs`
- Modify: `src/Mobizon.Net/Services/MessageService.cs`
- Test: `tests/Mobizon.Net.Tests/Services/MessageServiceTests.cs`

**Interfaces:**
- Produces:
  - `Task<SendSmsResult> QuickSendAsync(string recipient, ...)` (keep existing non-CancellationToken params)
  - `Task<SendSmsResult> SendSmsMessageAsync(SendSmsMessageRequest, ...)`
  - `Task<IReadOnlyList<SmsStatusResult>> GetSmsStatusAsync(long id, ...)`
  - `Task<IReadOnlyList<SmsStatusResult>> GetSmsStatusAsync(long[] ids, ...)`
  - `Task<MobizonListResult<MessageInfo>> ListAsync(MessageListRequest? = null, ...)`

- [ ] **Step 1: Update tests (Rule T)** in `MessageServiceTests.cs` — remove `.Code` asserts, `result.Data.X` → `result.X`.
- [ ] **Step 2: Run** `dotnet test --filter "FullyQualifiedName~MessageServiceTests"` — Expected: FAIL (compile).
- [ ] **Step 3: Update interface** per Interfaces block; update `<returns>` docs and remove the `if (response.Code == MobizonResponseCode.Success)` snippets in the XML examples (lines ~35, ~65) — replace with direct domain-object usage.
- [ ] **Step 4: Update implementation** — apply **Rule U** to all five methods (none are no-payload).
- [ ] **Step 5: Run** `dotnet test --filter "FullyQualifiedName~MessageServiceTests"` — Expected: PASS.
- [ ] **Step 6: Commit**

```bash
git add src/Mobizon.Contracts/Services/IMessageService.cs src/Mobizon.Net/Services/MessageService.cs tests/Mobizon.Net.Tests/Services/MessageServiceTests.cs
git commit -m "refactor: unwrap MobizonResponse from MessageService"
```

---

## Task 5: ContactCardService (internal, no public interface)

**Note:** `ContactCardService` is an `internal` class with **no public interface** — the public
contact-card surface is the already-unwrapped fluent API (`IContactCardSet` / `IContactCardQuery`,
implemented by `ContactCardSet` / `ContactCardQuery`). So there is no interface file to change here;
we unwrap the internal service and update its two internal consumers.

**Files:**
- Modify: `src/Mobizon.Net/Services/ContactCardService.cs` (internal impl — change return types)
- Modify: `src/Mobizon.Net/ContactCards/ContactCardSet.cs` (consumer — `.Data` accesses at lines ~60–61, ~75–78, ~92, ~98, ~109, ~116–117)
- Modify: `src/Mobizon.Net/ContactCards/ContactCardQuery.cs` (consumer — `ItemsOf` at line ~208 and `.Data` accesses at ~107–108, ~116–120, ~129–130, ~139–140, ~161–162)
- Test: `tests/Mobizon.Net.Tests/Services/ContactCardServiceTests.cs`

**Interfaces:**
- Produces (new `ContactCardService` method signatures — public methods on the internal class):
  - `Task<ContactCardListResult> ListAsync(ContactCardListRequest request, CancellationToken = default)`
  - `Task<ContactCardData> GetAsync(string id, CancellationToken = default)`
  - `Task<string> CreateAsync(...)`
  - `Task UpdateAsync(...)`  (was `bool`)
  - `Task SetGroupsAsync(...)`  (was `bool`)
  - `Task<IReadOnlyList<ContactGroupRef>> GetGroupsAsync(string id, CancellationToken = default)`
  - `Task RemoveAsync(string id, CancellationToken = default)`  (was `bool`)
- Note: `ContactCardListResult : MobizonListResult<ContactCardData>`, so it has `Items` and `TotalItemCount`.

- [ ] **Step 1: Update tests (Rule T)** in `ContactCardServiceTests.cs` — remove `.Code` asserts, `result.Data.X` → `result.X`; for `UpdateAsync`/`SetGroupsAsync`/`RemoveAsync` drop the result capture.
- [ ] **Step 2: Run** `dotnet test --filter "FullyQualifiedName~ContactCardServiceTests"` — Expected: FAIL (compile).
- [ ] **Step 3: Update the internal service impl** — apply **Rule U** to `ListAsync`, `GetAsync`, `CreateAsync`, `GetGroupsAsync`; apply **Rule V** to `UpdateAsync`, `SetGroupsAsync`, `RemoveAsync`.
- [ ] **Step 4: Update `ContactCardSet` consumer.** Each `_service.X(...)` call now returns the domain object directly. Change:
  - line ~60–61: `var response = await _service.GetAsync(...); return response != null ? ContactCardMapper.ToEntity(response) : null;`
  - line ~75–78: `var response = await _service.CreateAsync(...); entity.Id = long.TryParse(response, out var id) ? id : (long?)null;`
  - line ~116–117: `var response = await _service.GetGroupsAsync(...); return response ?? Array.Empty<ContactGroupRef>();`
  - `UpdateAsync`/`RemoveAsync`/`SetGroupsAsync` (lines ~92, ~98, ~109) already just return the service task — now returning `Task` instead of `Task<MobizonResponse<bool>>`; ensure the `ContactCardSet` method's own return type is `Task` (it is — these are fire-and-forget on the fluent API; confirm and adjust if it declared `Task<bool>`).

- [ ] **Step 5: Update `ContactCardQuery` consumer.** `ListAsync` now returns `ContactCardListResult` directly. Change `ItemsOf` and all `.Data` derefs:

```csharp
        private static IReadOnlyList<ContactCardData> ItemsOf(ContactCardListResult response)
            => response?.Items ?? Array.Empty<ContactCardData>();
```

And at the call sites: `Map(ItemsOf(response))` stays (now `response` is the unwrapped result); `response.Data?.TotalItemCount ?? 0` (lines ~120, ~130) → `response?.TotalItemCount ?? 0`.

- [ ] **Step 6: Run** `dotnet test --filter "FullyQualifiedName~ContactCard"` — Expected: PASS (covers service + fluent-query/set tests).
- [ ] **Step 7: Commit**

```bash
git add src/Mobizon.Net/Services/ContactCardService.cs src/Mobizon.Net/ContactCards/ContactCardSet.cs src/Mobizon.Net/ContactCards/ContactCardQuery.cs tests/Mobizon.Net.Tests/Services/ContactCardServiceTests.cs
git commit -m "refactor: unwrap MobizonResponse from internal ContactCardService and fluent consumers"
```

---

## Task 6: ContactGroupService

**Files:**
- Modify: `src/Mobizon.Contracts/Services/IContactGroupService.cs`
- Modify: `src/Mobizon.Net/Services/ContactGroupService.cs`
- Test: `tests/Mobizon.Net.Tests/Services/ContactGroupServiceTests.cs`

**Interfaces:**
- Produces:
  - `Task<ContactGroupListResponse> ListAsync(...)`
  - `Task<long> CreateAsync(string name, ...)`
  - `Task UpdateAsync(...)`  (was `bool`)
  - `Task<DeleteContactGroupResult> DeleteAsync(long id, ...)`  (payload IS meaningful — stays a value)
  - `Task<long> GetCardsCountAsync(...)`

- [ ] **Step 1: Update tests (Rule T)** — note `DeleteAsync` keeps a return value; assert `result.Processed` / `result.NotProcessed` directly.
- [ ] **Step 2: Run** `dotnet test --filter "FullyQualifiedName~ContactGroupServiceTests"` — Expected: FAIL (compile).
- [ ] **Step 3: Update interface + impl** — apply **Rule U** to `ListAsync`, `CreateAsync`, `DeleteAsync`, `GetCardsCountAsync`; apply **Rule V** to `UpdateAsync`.
- [ ] **Step 4: Run** `dotnet test --filter "FullyQualifiedName~ContactGroupServiceTests"` — Expected: PASS.
- [ ] **Step 5: Commit**

```bash
git add src/Mobizon.Contracts/Services/IContactGroupService.cs src/Mobizon.Net/Services/ContactGroupService.cs tests/Mobizon.Net.Tests/Services/ContactGroupServiceTests.cs
git commit -m "refactor: unwrap MobizonResponse from ContactGroupService"
```

---

## Task 7: NumberStopListService

**Files:**
- Modify: `src/Mobizon.Contracts/Services/INumberStopListService.cs`
- Modify: `src/Mobizon.Net/Services/NumberStopListService.cs`
- Test: `tests/Mobizon.Net.Tests/Services/NumberStopListServiceTests.cs`

**Interfaces:**
- Produces:
  - `Task<StopListListResponse> ListAsync(...)`
  - `Task<long> AddNumberAsync(string number, ...)`
  - `Task AddNumberRangeAsync(string numberFrom, ...)`  (was `bool`)
  - `Task DeleteAsync(long id, ...)`  (was `bool`)

- [ ] **Step 1: Update tests (Rule T)**.
- [ ] **Step 2: Run** `dotnet test --filter "FullyQualifiedName~NumberStopListServiceTests"` — Expected: FAIL (compile).
- [ ] **Step 3: Update interface + impl** — apply **Rule U** to `ListAsync`, `AddNumberAsync`; apply **Rule V** to `AddNumberRangeAsync`, `DeleteAsync`.
- [ ] **Step 4: Run** `dotnet test --filter "FullyQualifiedName~NumberStopListServiceTests"` — Expected: PASS.
- [ ] **Step 5: Commit**

```bash
git add src/Mobizon.Contracts/Services/INumberStopListService.cs src/Mobizon.Net/Services/NumberStopListService.cs tests/Mobizon.Net.Tests/Services/NumberStopListServiceTests.cs
git commit -m "refactor: unwrap MobizonResponse from NumberStopListService"
```

---

## Task 8: AlphanameService, TaskQueueService, UserService

**Files:**
- Modify: `src/Mobizon.Contracts/Services/IAlphanameService.cs`, `ITaskQueueService.cs`, `IUserService.cs`
- Modify: `src/Mobizon.Net/Services/AlphanameService.cs`, `TaskQueueService.cs`, `UserService.cs`
- Test: `tests/Mobizon.Net.Tests/Services/AlphanameServiceTests.cs`, `TaskQueueServiceTests.cs`, `UserServiceTests.cs`

**Interfaces:**
- Produces:
  - `Task<MobizonListResult<AlphanameData>> AlphanameService.ListAsync(...)`
  - `Task<TaskQueueStatus> TaskQueueService.GetStatusAsync(long id, ...)`
  - `Task<BalanceResult> UserService.GetOwnBalanceAsync(...)`

- [ ] **Step 1: Update the three test files (Rule T)**. In `IUserService.cs` XML-doc, remove the `if (response.Code == MobizonResponseCode.Success)` example (line ~28) and replace with direct `BalanceResult` usage.
- [ ] **Step 2: Run** `dotnet test --filter "FullyQualifiedName~AlphanameServiceTests|FullyQualifiedName~TaskQueueServiceTests|FullyQualifiedName~UserServiceTests"` — Expected: FAIL (compile).
- [ ] **Step 3: Update the three interfaces + impls** — apply **Rule U** to all three methods (all data-bearing).
- [ ] **Step 4: Run** the same filter — Expected: PASS.
- [ ] **Step 5: Commit**

```bash
git add src/Mobizon.Contracts/Services/IAlphanameService.cs src/Mobizon.Contracts/Services/ITaskQueueService.cs src/Mobizon.Contracts/Services/IUserService.cs src/Mobizon.Net/Services/AlphanameService.cs src/Mobizon.Net/Services/TaskQueueService.cs src/Mobizon.Net/Services/UserService.cs tests/Mobizon.Net.Tests/Services/AlphanameServiceTests.cs tests/Mobizon.Net.Tests/Services/TaskQueueServiceTests.cs tests/Mobizon.Net.Tests/Services/UserServiceTests.cs
git commit -m "refactor: unwrap MobizonResponse from Alphaname, TaskQueue, User services"
```

---

## Task 9: Move `MobizonResponse<T>` to internal

**Files:**
- Delete: `src/Mobizon.Contracts/Models/Common/MobizonResponse.cs`
- Create: `src/Mobizon.Net/Internal/MobizonResponse.cs` (as `internal`)
- Modify: `tests/Mobizon.Net.Tests/Models/MobizonResponseTests.cs` (move namespace usage; the test project already references internals — confirm via existing `using Mobizon.Net.Internal;` in service tests)
- Modify: any remaining `using Mobizon.Contracts.Models.Common;` that was only for `MobizonResponse` (the file still defines other Common types, so the namespace stays; just the wrapper type moves).

**Interfaces:**
- Produces: `internal class MobizonResponse<T>` in namespace `Mobizon.Net.Internal` with the same members (`RawCode`, `Code`, `Data`, `Message`).

- [ ] **Step 1: Verify the test project can see internals.** Check `src/Mobizon.Net/Mobizon.Net.csproj` (or an `AssemblyInfo`) for `InternalsVisibleTo("Mobizon.Net.Tests")`. If absent, add it:

```xml
  <ItemGroup>
    <InternalsVisibleTo Include="Mobizon.Net.Tests" />
  </ItemGroup>
```

Run: `dotnet build` — Expected: success (this change alone is safe).

- [ ] **Step 2: Move the type.** Create `src/Mobizon.Net/Internal/MobizonResponse.cs` with the body of the current `MobizonResponse.cs` but: namespace `Mobizon.Net.Internal`, class `internal class MobizonResponse<T>`. Keep `[JsonPropertyName]` attributes and the `Code` computed property (it references `MobizonResponseCode` — add `using Mobizon.Contracts.Models.Common;`). Delete `src/Mobizon.Contracts/Models/Common/MobizonResponse.cs`.

- [ ] **Step 3: Fix references.** `MobizonApiClient.cs` is already in `Mobizon.Net.Internal` (same namespace — no `using` needed). `ContactCardQuery.cs` was updated in Task 5 to not reference the wrapper; confirm. In `MobizonResponseTests.cs`, change `using Mobizon.Contracts.Models.Common;` for the wrapper to `using Mobizon.Net.Internal;` (keep the `MobizonResponseCode` using). Grep to confirm no other public references remain:

Run: `git grep -n "MobizonResponse<" -- "src/Mobizon.Contracts"`
Expected: no results (all public usages removed by Tasks 2–8).

- [ ] **Step 4: Build and run the full suite.**

Run: `dotnet test`
Expected: PASS (entire solution).

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "refactor: make MobizonResponse internal, move to Mobizon.Net.Internal"
```

---

## Task 10: Update samples and README

**Files:**
- Modify: `samples/Mobizon.Net.ConsoleSample/Samples/CampaignSamples.cs`
- Modify: `samples/Mobizon.Net.ConsoleSample/Samples/LinkSamples.cs` and any other sample modules that read `.Data` or `.Code`
- Modify: `README.md`

- [ ] **Step 1: Fix CampaignSamples.** Replace the `if (sendResult.Code == MobizonResponseCode.BackgroundTask)` branch (lines ~62–65) with:

```csharp
            // SendAsync returns a CampaignSendResult; IsQueued is true when the send was
            // accepted as a background task and Id is the task id.
            var sendResult = await client.Campaigns.SendAsync(campaignId);
            if (sendResult.IsQueued)
                Console.WriteLine($"Queued as background task {sendResult.Id}");
            else
                Console.WriteLine($"Sent (result {sendResult.Id})");
```

- [ ] **Step 2: Sweep all samples for `.Data` / `.Code`.**

Run: `git grep -n "\.Data\|\.Code ==\|MobizonResponseCode" -- "samples"`
For each hit, remove the `.Data` indirection (the call now returns the domain object) and drop `.Code`-based success checks (success is implied; wrap calls that can fail in try/catch on `MobizonApiException` if the sample demonstrates error handling).

- [ ] **Step 3: Update README.** Find each example using `MobizonResponse` / `.Data` (e.g. the README quickstart at line ~210 with `sendResult.Code == MobizonResponseCode.BackgroundTask`) and rewrite to the new surface (`sendResult.IsQueued`). Keep the `MobizonResponseCode` table (still relevant for `MobizonApiException.Code`); add a sentence that codes 98/99/100 surface as `AddRecipientsResult.Outcome` / `CampaignSendResult.IsQueued` rather than on a wrapper.

Run: `git grep -n "MobizonResponse<\|\.Data" -- README.md`
Expected: no stale wrapper/`.Data` references remain (verify each remaining hit is intentional prose).

- [ ] **Step 4: Final full build + test + sample compile.**

Run: `dotnet build && dotnet test`
Expected: PASS. Then `dotnet build samples/Mobizon.Net.ConsoleSample/Mobizon.Net.ConsoleSample.csproj` — Expected: success.

- [ ] **Step 5: Commit**

```bash
git add samples README.md
git commit -m "docs: update samples and README for unwrapped service surface"
```

---

## Self-Review notes

- **Spec coverage:** Category A → Tasks 2,4,5,6,7,8 + parts of 3; Category B (void) → Rule V in Tasks 2,3,5,6,7; Category C (fold) → Task 1 + Task 3; wrapper→internal → Task 9; tests/samples/README → embedded per task + Task 10. CHANGELOG: add an entry in Task 10 Step 3 if the repo keeps `CHANGELOG.md` current (note: this repo has a modified `CHANGELOG.md` — add a "Changed: services return domain types directly; MobizonResponse is now internal" line).
- **Resolved during planning:** ContactCards has no `IContactCardService` (only `IContactCardSet`/`IContactCardQuery`, already unwrapped); `ContactCardListResult` exposes `Items`/`TotalItemCount` via `MobizonListResult<ContactCardData>` — Task 5 reflects this.
- **Open verification point (resolve during execution, not a blocker):** confirm `RecipientEntry` property names in Task 3 Step 1, and `CampaignSendResult` sync payload value (the `Id` in the non-queued case), against the existing `CampaignServiceTests` fixtures. Local lookups, not design decisions.

# Mobizon.Net Phase 1 — Correctness (breaking) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make every response model and request contract match the real Mobizon API (verified by the Phase 0 capture), guarded by tests.

**Architecture:** Per task: write/adjust the model or service, update the tests that break, add tests for the fixed behaviour, commit. Breaking changes are allowed (pre-release); no `[Obsolete]` shims.

**Tech Stack:** C# 8 / `netstandard2.0` core; xunit + RichardSzalay.MockHttp (net8.0 tests); System.Text.Json.

**Spec:** `docs/superpowers/specs/2026-06-24-mobizon-net-refactor-design.md` (§5, §5b)
**Shapes:** `docs/superpowers/notes/2026-06-24-api-shapes.md`
**Fixtures:** `tests/Mobizon.Net.Tests/Payloads/*.json`

## Global Constraints

- Core targets `netstandard2.0`, C# 8, `Nullable enable`, warning-clean under `TreatWarningsAsErrors`.
- No third-party deps in `Mobizon.Contracts` / `Mobizon.Net`.
- **Numeric conversion is automatic:** `MobizonApiClient.JsonOptions` registers `StringToInt/Float/Decimal/Bool` converters globally and they accept BOTH JSON string and number tokens (verified `StringToIntConverter.cs`). So `int`/`decimal`/`bool` properties parse `"2"` and `2` alike — models need only correct `[JsonPropertyName]`, not per-property converter attributes.
- **Test data rule:** fixtures in `Payloads/` validate STRUCTURE (field names, envelope shape, nesting, enum/date string formats). The sanitizer masked every 7+ digit run to the non-numeric placeholder `7000000XXXX`, so fixtures with large IDs do NOT deserialize into `int` fields. For value/ID assertions use **inline JSON** with realistic numeric IDs (the existing test style). Use fixtures for shape/round-trip checks where IDs are small (≤6 digits survived, e.g. `link.list.json`) or not asserted.
- Existing service tests construct: `var apiClient = new MobizonApiClient(mockHttp.ToHttpClient(), _options); return new XService(apiClient);` — follow this exactly.
- Branch `feature/api-contract-refactor`. Commit trailers:
  `Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>` + the `Claude-Session:` trailer.

---

## File Structure (Phase 1)

- Create `src/Mobizon.Contracts/Models/Common/MobizonListResult.cs` — generic `{Items,TotalItemCount}`.
- Create `src/Mobizon.Contracts/Models/Links/LinkStatPoint.cs` — single stats point.
- Create `src/Mobizon.Net/Internal/Converters/AddRecipientsResultConverter.cs` — array|scalar.
- Modify models: `LinkData`, `UpdateLinkRequest`, `LinkStatsResult`, `LinkStatsType`, `AddRecipientsResult`, `CampaignCounters`.
- Delete: `MessageListResponse`, `ContactCardListResponse`, `CreateCampaignResult`, `CampaignSendResult` (replaced).
- Modify services/interfaces: `IMessageService`/`MessageService`, `ILinkService`/`LinkService`, `ICampaignService`/`CampaignService`, `ContactCardService`/`ContactCardQuery`.
- Tests: fill `CampaignServiceTests` (empty), update `LinkServiceTests`, add `MobizonListResultTests`, `AddRecipientsResultConverterTests`; `MessageServiceTests`/`ContactCard*Tests` keep passing (member names unchanged).

---

### Task 1.0: Fixture wiring + `Fixtures` helper

**Files:**
- Modify: `tests/Mobizon.Net.Tests/Mobizon.Net.Tests.csproj`
- Create: `tests/Mobizon.Net.Tests/Fixtures.cs`, `tests/Mobizon.Net.Tests/FixturesTests.cs`

**Interfaces:**
- Produces: `Mobizon.Net.Tests.Fixtures.Load(string fileName) : string`

- [ ] **Step 1: Copy Payloads to test output.** Add to `Mobizon.Net.Tests.csproj` a new `<ItemGroup>`:

```xml
<ItemGroup>
  <Content Include="Payloads\**\*.json" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

- [ ] **Step 2: Write the failing test** `tests/Mobizon.Net.Tests/FixturesTests.cs`:

```csharp
using Xunit;

namespace Mobizon.Net.Tests
{
    public class FixturesTests
    {
        [Fact]
        public void Load_Returns_Fixture_Content()
        {
            var json = Fixtures.Load("user.getOwnBalance.json");
            Assert.Contains("\"currency\"", json);
        }
    }
}
```

- [ ] **Step 3: Run, verify FAIL.** Run: `dotnet test tests/Mobizon.Net.Tests --filter Load_Returns_Fixture_Content` — Expected: FAIL (`Fixtures` not found).

- [ ] **Step 4: Implement** `tests/Mobizon.Net.Tests/Fixtures.cs`:

```csharp
using System;
using System.IO;

namespace Mobizon.Net.Tests
{
    public static class Fixtures
    {
        public static string Load(string fileName) =>
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Payloads", fileName));
    }
}
```

- [ ] **Step 5: Run, verify PASS.** Same filter. Expected: PASS.
- [ ] **Step 6: Commit.** `git add tests/Mobizon.Net.Tests/Fixtures.cs tests/Mobizon.Net.Tests/FixturesTests.cs tests/Mobizon.Net.Tests/Mobizon.Net.Tests.csproj && git commit -m "test: wire captured fixtures into test project"`

---

### Task 1.1: Generic `MobizonListResult<T>` + migrate Message & ContactCard list

**Files:**
- Create: `src/Mobizon.Contracts/Models/Common/MobizonListResult.cs`
- Delete: `src/Mobizon.Contracts/Models/Messages/MessageListResponse.cs`, `src/Mobizon.Contracts/Models/ContactCards/ContactCardListResponse.cs`
- Modify: `IMessageService.cs`/`MessageService.cs:88`, `IContactCardQuery`/`ContactCardService.cs`, `ContactCardQuery.cs:83-99`
- Test: `tests/Mobizon.Net.Tests/Models/MobizonListResultTests.cs`

**Interfaces:**
- Produces: `public class MobizonListResult<T> { IReadOnlyList<T> Items; int TotalItemCount; }`. `MessageService.ListAsync` → `MobizonResponse<MobizonListResult<MessageInfo>>`; `ContactCardService.ListAsync` → `MobizonResponse<MobizonListResult<ContactCardData>>`.

- [ ] **Step 1: Write the failing test** `MobizonListResultTests.cs` (uses the real `message.list.json` — empty items, `totalItemCount:"0"` string):

```csharp
using System.Text.Json;
using Mobizon.Contracts.Models.Common;
using Mobizon.Contracts.Models.Messages;
using Xunit;

namespace Mobizon.Net.Tests.Models
{
    public class MobizonListResultTests
    {
        [Fact]
        public void Deserializes_Items_And_StringTotalItemCount()
        {
            // Real envelope shape; totalItemCount arrives as a STRING.
            var json = @"{""items"":[],""totalItemCount"":""0""}";
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            opts.Converters.Add(new Mobizon.Net.Internal.Converters.StringToIntConverter());
            var result = JsonSerializer.Deserialize<MobizonListResult<MessageInfo>>(json, opts)!;
            Assert.NotNull(result.Items);
            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalItemCount);
        }
    }
}
```

(Note: `StringToIntConverter` is `internal`; add `[assembly: InternalsVisibleTo("Mobizon.Net.Tests")]` to `src/Mobizon.Net/Properties/AssemblyInfo.cs` — create it — OR assert via the service path instead. Prefer the service-path assertion in Step 1b if InternalsVisibleTo is undesired.)

- [ ] **Step 1b (alternative, no InternalsVisibleTo): service-path test** in `MessageServiceTests` already covers envelope deserialization via the client's global converters; keep `MobizonListResultTests` to the structural assert above using a public path. If avoiding internals, assert by deserializing through `MessageService.ListAsync` with a MockHttp fixture response of `message.list.json`. Choose ONE approach and delete the other.

- [ ] **Step 2: Run, verify FAIL.** `dotnet test tests/Mobizon.Net.Tests --filter Deserializes_Items_And_StringTotalItemCount` — FAIL (`MobizonListResult` not found).

- [ ] **Step 3: Implement** `MobizonListResult.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace Mobizon.Contracts.Models.Common
{
    /// <summary>
    /// Generic envelope for Mobizon list endpoints: <c>{ items, totalItemCount }</c>.
    /// </summary>
    public class MobizonListResult<T>
    {
        /// <summary>The items on the current page.</summary>
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

        /// <summary>Total number of items matching the query (API returns this as a string).</summary>
        public int TotalItemCount { get; set; }
    }
}
```

- [ ] **Step 4: Migrate Message list.** In `MessageService.cs:88` change the return type to `Task<MobizonResponse<MobizonListResult<MessageInfo>>>` and `_apiClient.SendAsync<MobizonListResult<MessageInfo>>(...)`; update `IMessageService` signature; delete `MessageListResponse.cs`. The existing `MessageServiceTests.ListAsync_*` use `result.Data.Items`/`.TotalItemCount` — unchanged, so they keep passing.

- [ ] **Step 5: Migrate ContactCard list.** In `ContactCardService.cs` change `ListAsync` to return `MobizonResponse<MobizonListResult<ContactCardData>>`; delete `ContactCardListResponse.cs`. `ContactCardQuery.cs:83-99` references `response.Data.Items`/`response.Data.TotalItemCount` — unchanged. Update `IContactCardQuery`/service signatures accordingly.

- [ ] **Step 6: Run all tests, verify PASS.** `dotnet test tests/Mobizon.Net.Tests` — Expected: PASS (Message/ContactCard tests still green, new test green).
- [ ] **Step 7: Commit.** `git add -A && git commit -m "feat!: generic MobizonListResult<T> for list endpoints"`

---

### Task 1.2: Campaign/List → envelope (fill empty CampaignServiceTests)

**Files:**
- Modify: `ICampaignService.cs:88`, `CampaignService.cs:99-165`
- Test: `tests/Mobizon.Net.Tests/Services/CampaignServiceTests.cs` (currently empty)

**Interfaces:**
- Consumes: `MobizonListResult<T>` (Task 1.1).
- Produces: `CampaignService.ListAsync` → `MobizonResponse<MobizonListResult<CampaignData>>`.

- [ ] **Step 1: Write the failing test** in `CampaignServiceTests.cs` (mirror `LinkServiceTests` scaffolding; inline JSON with a small numeric id so it deserializes):

```csharp
using System.Net.Http;
using System.Threading.Tasks;
using Mobizon.Contracts.Models.Campaigns;
using Mobizon.Contracts.Models.Common;
using Mobizon.Net.Internal;
using Mobizon.Net.Services;
using RichardSzalay.MockHttp;
using Xunit;

namespace Mobizon.Net.Tests.Services
{
    public class CampaignServiceTests
    {
        private readonly MobizonClientOptions _options = new MobizonClientOptions
        { ApiKey = "test-key", ApiUrl = "https://api.mobizon.kz" };

        private CampaignService CreateService(MockHttpMessageHandler mockHttp)
            => new CampaignService(new MobizonApiClient(mockHttp.ToHttpClient(), _options));

        [Fact]
        public async Task ListAsync_Deserializes_Envelope()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/Campaign/List")
                .WithFormData("criteria[type]", "2")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""items"":[{""id"":""123"",""type"":""2"",""text"":""hi""}],""totalItemCount"":""1""},""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.ListAsync(new CampaignListRequest
            {
                Criteria = new CampaignListCriteria { Type = 2 }
            });

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Single(result.Data.Items);
            Assert.Equal(1, result.Data.TotalItemCount);
            Assert.Equal(123, result.Data.Items[0].Id);
            mockHttp.VerifyNoOutstandingExpectation();
        }
    }
}
```

- [ ] **Step 2: Run, verify FAIL.** `dotnet test tests/Mobizon.Net.Tests --filter ListAsync_Deserializes_Envelope` — FAIL (return type is `IReadOnlyList<CampaignData>`; `result.Data.Items` does not compile / cast fails).

- [ ] **Step 3: Implement.** In `CampaignService.cs:99` and `:163`, change to `MobizonResponse<MobizonListResult<CampaignData>>` and `SendAsync<MobizonListResult<CampaignData>>(...)`; update `ICampaignService.ListAsync`.

- [ ] **Step 4: Run, verify PASS.** Same filter. Expected: PASS.
- [ ] **Step 5: Commit.** `git add -A && git commit -m "fix!: Campaign/List returns MobizonListResult<CampaignData>"`

---

### Task 1.3: Link/List → envelope + `LinkData` field fixes

**Files:**
- Modify: `src/Mobizon.Contracts/Models/Links/LinkData.cs`, `ILinkService.cs:93`, `LinkService.cs:102-125`
- Test: update `tests/Mobizon.Net.Tests/Services/LinkServiceTests.cs` (Create/Get/List)

**Interfaces:**
- Produces: `LinkData` with `[JsonPropertyName("clickCnt")] int ClickCnt`, `RedirectCnt`, `ShortLink`, `ModeratorStatus`, `CreateTs`, `UpdateTs`, `RealExpirationDate`, `ModeratorComment` (the always-zero `Clicks` is removed). `LinkService.ListAsync` → `MobizonResponse<MobizonListResult<LinkData>>`.

- [ ] **Step 1: Replace `LinkData.cs`** (numeric strings auto-convert via global converters):

```csharp
using System.Text.Json.Serialization;

namespace Mobizon.Contracts.Models.Links
{
    /// <summary>Represents a Mobizon short link.</summary>
    public class LinkData
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        [JsonPropertyName("shortLink")] public string? ShortLink { get; set; }
        public string FullLink { get; set; } = string.Empty;
        public int Status { get; set; }
        [JsonPropertyName("moderatorStatus")] public int ModeratorStatus { get; set; }
        [JsonPropertyName("clickCnt")] public int ClickCnt { get; set; }
        [JsonPropertyName("redirectCnt")] public int RedirectCnt { get; set; }
        public string? ExpirationDate { get; set; }
        [JsonPropertyName("realExpirationDate")] public string? RealExpirationDate { get; set; }
        public string? Comment { get; set; }
        [JsonPropertyName("moderatorComment")] public string? ModeratorComment { get; set; }
        [JsonPropertyName("createTs")] public string? CreateTs { get; set; }
        [JsonPropertyName("updateTs")] public string? UpdateTs { get; set; }
    }
}
```

- [ ] **Step 2: Update Link/List return** in `LinkService.cs:102,123` to `MobizonResponse<MobizonListResult<LinkData>>` and `SendAsync<MobizonListResult<LinkData>>(...)`; update `ILinkService.ListAsync`.

- [ ] **Step 3: Update existing `LinkServiceTests` that now break.** Replace the three affected tests:
  - `CreateAsync_SendsCorrectParameters`: in the response JSON change `"clicks":0` → `"clickCnt":"0"`; the `Assert` on clicks is absent there (keep id/code/fullLink asserts).
  - `GetAsync_SendsCodeParameter`: response `"clicks":5` → `"clickCnt":"5"`; change `Assert.Equal(5, result.Data.Clicks)` → `Assert.Equal(5, result.Data.ClickCnt)`.
  - `ListAsync_WithPaginationAndSort_SendsFormData` and `ListAsync_WithoutRequest_SendsNoFormData`: wrap response data in the envelope and assert via `.Items`. New bodies:

```csharp
[Fact]
public async Task ListAsync_WithPaginationAndSort_SendsFormData()
{
    var mockHttp = new MockHttpMessageHandler();
    mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/link/list")
        .WithFormData("pagination[currentPage]", "1")
        .WithFormData("pagination[pageSize]", "10")
        .WithFormData("sort[id]", "DESC")
        .Respond("application/json",
            @"{""code"":0,""data"":{""items"":[{""id"":""1"",""code"":""abc"",""fullLink"":""https://example.com"",""status"":""1"",""clickCnt"":""0""}],""totalItemCount"":""1""},""message"":""""}");

    var service = CreateService(mockHttp);
    var result = await service.ListAsync(new LinkListRequest
    {
        Pagination = new PaginationRequest { CurrentPage = 1, PageSize = 10 },
        Sort = new SortRequest { Field = "id", Direction = SortDirection.DESC }
    });

    Assert.Equal(MobizonResponseCode.Success, result.Code);
    Assert.Single(result.Data.Items);
    Assert.Equal(1, result.Data.TotalItemCount);
    mockHttp.VerifyNoOutstandingExpectation();
}

[Fact]
public async Task ListAsync_WithoutRequest_SendsNoFormData()
{
    var mockHttp = new MockHttpMessageHandler();
    mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/link/list")
        .Respond("application/json", @"{""code"":0,""data"":{""items"":[],""totalItemCount"":""0""},""message"":""""}");

    var service = CreateService(mockHttp);
    var result = await service.ListAsync();

    Assert.Equal(MobizonResponseCode.Success, result.Code);
    Assert.Empty(result.Data.Items);
    mockHttp.VerifyNoOutstandingExpectation();
}
```

- [ ] **Step 4: Add a real-fixture structural test** (small IDs in `link.list.json` survive sanitization):

```csharp
[Fact]
public async Task ListAsync_Parses_Real_Fixture_Shape()
{
    var mockHttp = new MockHttpMessageHandler();
    mockHttp.When(HttpMethod.Post, "https://api.mobizon.kz/service/link/list")
        .Respond("application/json", Fixtures.Load("link.list.json"));

    var service = CreateService(mockHttp);
    var result = await service.ListAsync(new LinkListRequest());

    Assert.Equal(2, result.Data.TotalItemCount);
    Assert.Equal(2, result.Data.Items.Count);
    Assert.Equal("tyz2", result.Data.Items[0].Code);
    Assert.Equal(1, result.Data.Items[1].ClickCnt); // second link has clickCnt "1"
}
```

- [ ] **Step 5: Run, verify PASS.** `dotnet test tests/Mobizon.Net.Tests --filter LinkServiceTests` — Expected: PASS.
- [ ] **Step 6: Commit.** `git add -A && git commit -m "fix!: Link/List envelope + LinkData clickCnt/redirectCnt and missing fields"`

---

### Task 1.4: Link/Get (id|code|shortLink) + Link/Update by Id

**Files:**
- Modify: `ILinkService.cs:51`, `LinkService.cs:56-66`, `UpdateLinkRequest.cs`, `LinkService.cs:127-149`
- Test: update `LinkServiceTests` Get/Update tests

**Interfaces:**
- Produces: `GetByIdAsync(int id,…)`, `GetByCodeAsync(string code,…)`, `GetByShortLinkAsync(string shortLink,…)` → `MobizonResponse<LinkData>`. `UpdateLinkRequest { int Id; int? Status; string? ExpirationDate; string? Comment; }` (no `Code`, no `FullLink`).

- [ ] **Step 1: Write failing tests** (replace `GetAsync_SendsCodeParameter`, `UpdateAsync_*`):

```csharp
[Fact]
public async Task GetByCodeAsync_SendsCode()
{
    var mockHttp = new MockHttpMessageHandler();
    mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/link/get")
        .WithFormData("code", "abc123")
        .Respond("application/json", @"{""code"":0,""data"":{""id"":""1"",""code"":""abc123"",""fullLink"":""https://e.com"",""status"":""1"",""clickCnt"":""5""},""message"":""""}");
    var result = await CreateService(mockHttp).GetByCodeAsync("abc123");
    Assert.Equal(5, result.Data.ClickCnt);
    mockHttp.VerifyNoOutstandingExpectation();
}

[Fact]
public async Task GetByIdAsync_SendsId()
{
    var mockHttp = new MockHttpMessageHandler();
    mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/link/get")
        .WithFormData("id", "42")
        .Respond("application/json", @"{""code"":0,""data"":{""id"":""42"",""code"":""x"",""fullLink"":""https://e.com"",""status"":""1"",""clickCnt"":""0""},""message"":""""}");
    var result = await CreateService(mockHttp).GetByIdAsync(42);
    Assert.Equal(42, result.Data.Id);
    mockHttp.VerifyNoOutstandingExpectation();
}

[Fact]
public async Task GetByShortLinkAsync_SendsShortLink()
{
    var mockHttp = new MockHttpMessageHandler();
    mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/link/get")
        .WithFormData("shortLink", "https://mbzn.co/x")
        .Respond("application/json", @"{""code"":0,""data"":{""id"":""1"",""code"":""x"",""fullLink"":""https://e.com"",""status"":""1"",""clickCnt"":""0""},""message"":""""}");
    var result = await CreateService(mockHttp).GetByShortLinkAsync("https://mbzn.co/x");
    Assert.Equal(MobizonResponseCode.Success, result.Code);
    mockHttp.VerifyNoOutstandingExpectation();
}

[Fact]
public async Task UpdateAsync_SendsIdAndFields_NoFullLink()
{
    var mockHttp = new MockHttpMessageHandler();
    mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/link/update")
        .WithFormData("id", "42")
        .WithFormData("data[status]", "0")
        .WithFormData("data[comment]", "c")
        .Respond("application/json", @"{""code"":0,""data"":true,""message"":""""}");
    var result = await CreateService(mockHttp).UpdateAsync(new UpdateLinkRequest { Id = 42, Status = 0, Comment = "c" });
    Assert.Equal(MobizonResponseCode.Success, result.Code);
    mockHttp.VerifyNoOutstandingExpectation();
}
```

(Delete `GetAsync_SendsCodeParameter`, `UpdateAsync_SendsCorrectParameters`, `UpdateAsync_WithOnlyCode_SendsMinimalParams`.)

- [ ] **Step 2: Run, verify FAIL** (`GetByCodeAsync`/etc. not defined). Run: `dotnet test tests/Mobizon.Net.Tests --filter LinkServiceTests`.

- [ ] **Step 3: Implement service.** Replace `LinkService.GetAsync` with three methods, and rewrite `UpdateAsync`:

```csharp
public Task<MobizonResponse<LinkData>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    => _apiClient.SendAsync<LinkData>(HttpMethod.Post, ModuleName, "get",
        new Dictionary<string, string> { ["id"] = id.ToString() }, cancellationToken);

public Task<MobizonResponse<LinkData>> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    => _apiClient.SendAsync<LinkData>(HttpMethod.Post, ModuleName, "get",
        new Dictionary<string, string> { ["code"] = code }, cancellationToken);

public Task<MobizonResponse<LinkData>> GetByShortLinkAsync(string shortLink, CancellationToken cancellationToken = default)
    => _apiClient.SendAsync<LinkData>(HttpMethod.Post, ModuleName, "get",
        new Dictionary<string, string> { ["shortLink"] = shortLink }, cancellationToken);

public Task<MobizonResponse<object>> UpdateAsync(UpdateLinkRequest request, CancellationToken cancellationToken = default)
{
    var parameters = new Dictionary<string, string> { ["id"] = request.Id.ToString() };
    if (request.Status.HasValue) parameters["data[status]"] = request.Status.Value.ToString();
    if (request.ExpirationDate != null) parameters["data[expirationDate]"] = request.ExpirationDate;
    if (request.Comment != null) parameters["data[comment]"] = request.Comment;
    return _apiClient.SendAsync<object>(HttpMethod.Post, ModuleName, "update", parameters, cancellationToken);
}
```

Replace `UpdateLinkRequest.cs` body with `int Id`, `int? Status`, `string? ExpirationDate`, `string? Comment` (drop `Code`, `FullLink`); update `ILinkService` (three Get methods; same `UpdateAsync` signature).

- [ ] **Step 4: Run, verify PASS.** `dotnet test tests/Mobizon.Net.Tests --filter LinkServiceTests`.
- [ ] **Step 5: Commit.** `git add -A && git commit -m "feat!: Link/Get by id|code|shortLink; Link/Update by id"`

---

### Task 1.5: Link/GetStats → `{Items, Totals}` + `LinkStatsType` hourly/minute

**Files:**
- Create: `src/Mobizon.Contracts/Models/Links/LinkStatPoint.cs`
- Modify: `src/Mobizon.Contracts/Models/Links/LinkStatsResult.cs`, `LinkStatsType.cs`, `GetLinkStatsRequest.cs` (doc `ids`≤5), `ILinkService.cs:79`, `LinkService.cs:80-100`
- Test: update `LinkServiceTests` GetStats tests

**Interfaces:**
- Produces: `LinkStatPoint { int LinkId; string Date; int Clicks; }`; `LinkStatsResult { IReadOnlyList<LinkStatPoint> Items; int Totals; }`; `LinkStatsType { Monthly, Daily, Hourly, Minute }`. `GetStatsAsync` → `MobizonResponse<LinkStatsResult>`.

> NOTE: per-item `LinkStatPoint` fields are not yet confirmed by a live capture (the Phase 0 run sent an invalid `type`; the tool is now fixed). Model from the prior test heuristic `{linkId, date, clicks}`; verify with a read-only re-capture (`dotnet run --project tools/Mobizon.Net.ApiCapture`) and adjust field names if the fixture differs. Envelope `{items, totals}` IS confirmed (docs).

- [ ] **Step 1: Write failing tests** (replace `GetStatsAsync_*`):

```csharp
[Fact]
public async Task GetStatsAsync_ReturnsItemsAndTotals()
{
    var mockHttp = new MockHttpMessageHandler();
    mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/link/getstats")
        .WithFormData("ids[0]", "1").WithFormData("type", "daily")
        .Respond("application/json",
            @"{""code"":0,""data"":{""items"":[{""linkId"":""1"",""date"":""2025-01-01"",""clicks"":""10""}],""totals"":""10""},""message"":""""}");
    var result = await CreateService(mockHttp).GetStatsAsync(new GetLinkStatsRequest { Ids = new[] { 1 }, Type = LinkStatsType.Daily });
    Assert.Single(result.Data.Items);
    Assert.Equal(10, result.Data.Items[0].Clicks);
    Assert.Equal(10, result.Data.Totals);
    mockHttp.VerifyNoOutstandingExpectation();
}

[Theory]
[InlineData(LinkStatsType.Hourly, "hourly")]
[InlineData(LinkStatsType.Minute, "minute")]
public async Task GetStatsAsync_SerializesNewTypes(LinkStatsType type, string expected)
{
    var mockHttp = new MockHttpMessageHandler();
    mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/link/getstats")
        .WithFormData("ids[0]", "1").WithFormData("type", expected)
        .Respond("application/json", @"{""code"":0,""data"":{""items"":[],""totals"":""0""},""message"":""""}");
    await CreateService(mockHttp).GetStatsAsync(new GetLinkStatsRequest { Ids = new[] { 1 }, Type = type });
    mockHttp.VerifyNoOutstandingExpectation();
}
```

- [ ] **Step 2: Run, verify FAIL.** `dotnet test tests/Mobizon.Net.Tests --filter GetStats`.

- [ ] **Step 3: Implement.** Create `LinkStatPoint.cs`:

```csharp
using System.Text.Json.Serialization;

namespace Mobizon.Contracts.Models.Links
{
    /// <summary>A single click-statistics data point for a short link.</summary>
    public class LinkStatPoint
    {
        [JsonPropertyName("linkId")] public int LinkId { get; set; }
        public string Date { get; set; } = string.Empty;
        public int Clicks { get; set; }
    }
}
```

Replace `LinkStatsResult.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace Mobizon.Contracts.Models.Links
{
    /// <summary>Click statistics for short links: per-period points plus total clicks.</summary>
    public class LinkStatsResult
    {
        public IReadOnlyList<LinkStatPoint> Items { get; set; } = Array.Empty<LinkStatPoint>();
        public int Totals { get; set; }
    }
}
```

Add to `LinkStatsType.cs` the `Hourly` and `Minute` members (keep `Monthly`, `Daily`). In `LinkService.cs:98` change to `SendAsync<LinkStatsResult>(...)`; update `ILinkService.GetStatsAsync` return type. In `GetLinkStatsRequest.cs` XML-doc that `Ids` max is 5.

- [ ] **Step 4: Run, verify PASS.** `dotnet test tests/Mobizon.Net.Tests --filter LinkServiceTests`.
- [ ] **Step 5: Commit.** `git add -A && git commit -m "fix!: Link/GetStats {items,totals}; add hourly/minute stat types"`

---

### Task 1.6: campaign/create → `int`; campaign/send → `int`

**Files:**
- Delete: `CreateCampaignResult.cs`, `CampaignSendResult.cs`
- Modify: `ICampaignService.cs`, `CampaignService.cs:23-58,167-177`
- Test: `CampaignServiceTests`

**Interfaces:**
- Produces: `CreateAsync` → `MobizonResponse<int>` (new campaign id); `SendAsync` → `MobizonResponse<int>` (when `Code==BackgroundTask`, value is the task id).

- [ ] **Step 1: Write failing tests** (inline JSON; create returns bare string id, send returns bare int):

```csharp
[Fact]
public async Task CreateAsync_ReturnsCampaignId_FromScalar()
{
    var mockHttp = new MockHttpMessageHandler();
    mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/Campaign/Create")
        .WithFormData("data[type]", "2").WithFormData("data[text]", "hi")
        .Respond("application/json", @"{""code"":0,""data"":""123456"",""message"":""""}");
    var result = await CreateService(mockHttp).CreateAsync(new CreateCampaignRequest { Type = CampaignType.Bulk, Text = "hi" });
    Assert.Equal(123456, result.Data);
    mockHttp.VerifyNoOutstandingExpectation();
}

[Fact]
public async Task SendAsync_ReturnsScalar()
{
    var mockHttp = new MockHttpMessageHandler();
    mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/Campaign/Send")
        .WithFormData("id", "5")
        .Respond("application/json", @"{""code"":0,""data"":2,""message"":""""}");
    var result = await CreateService(mockHttp).SendAsync(5);
    Assert.Equal(MobizonResponseCode.Success, result.Code);
    Assert.Equal(2, result.Data);
    mockHttp.VerifyNoOutstandingExpectation();
}
```

- [ ] **Step 2: Run, verify FAIL.**
- [ ] **Step 3: Implement.** In `CampaignService.cs`: `CreateAsync` → `Task<MobizonResponse<int>>` + `SendAsync<int>(...)`; `SendAsync` → `Task<MobizonResponse<int>>` + `SendAsync<int>(...)`. Delete `CreateCampaignResult.cs`, `CampaignSendResult.cs`. Update `ICampaignService` (both signatures; XML-doc that `SendAsync`'s value is the task id when `Code==BackgroundTask`).
- [ ] **Step 4: Run, verify PASS.**
- [ ] **Step 5: Commit.** `git add -A && git commit -m "fix!: Campaign/Create and Campaign/Send return scalar id"`

---

### Task 1.7: campaign/addRecipients custom converter (array | scalar)

**Files:**
- Create: `src/Mobizon.Net/Internal/Converters/AddRecipientsResultConverter.cs`
- Modify: `AddRecipientsResult.cs` (add `[JsonConverter]`; add `Type` to `AddRecipientEntry`), register converter in `MobizonApiClient.cs:25-39`
- Test: `tests/Mobizon.Net.Tests/Internal/AddRecipientsResultConverterTests.cs`

**Interfaces:**
- Consumes: `AddRecipientsResult`, `AddRecipientEntry` (Phase 0 §5b).
- Produces: deserialization where `data` array → `Entries`, `data` scalar number → `TaskId`. `AddRecipientEntry.Type` (string).

- [ ] **Step 1: Write failing tests:**

```csharp
using System.Net.Http;
using System.Threading.Tasks;
using Mobizon.Contracts.Models.Campaigns;
using Mobizon.Contracts.Models.Common;
using Mobizon.Net.Internal;
using Mobizon.Net.Services;
using RichardSzalay.MockHttp;
using Xunit;

namespace Mobizon.Net.Tests.Internal
{
    public class AddRecipientsResultConverterTests
    {
        private readonly MobizonClientOptions _options = new MobizonClientOptions
        { ApiKey = "k", ApiUrl = "https://api.mobizon.kz" };
        private CampaignService Svc(MockHttpMessageHandler m) => new CampaignService(new MobizonApiClient(m.ToHttpClient(), _options));

        [Fact]
        public async Task SyncArray_PopulatesEntries()
        {
            var m = new MockHttpMessageHandler();
            m.When(HttpMethod.Post, "https://api.mobizon.kz/service/Campaign/AddRecipients")
                .Respond("application/json",
                    @"{""code"":0,""data"":[{""recipient"":""77001112233"",""code"":0,""messageId"":""42"",""type"":""number"",""number"":""77001112233""}],""message"":""""}");
            var r = await Svc(m).AddRecipientsAsync(new AddRecipientsRequest { CampaignId = 1, Recipients = new[] { new RecipientEntry { Recipient = "77001112233" } } });
            Assert.NotNull(r.Data.Entries);
            Assert.Single(r.Data.Entries!);
            Assert.Equal(0, r.Data.Entries![0].Code);
            Assert.Equal("number", r.Data.Entries![0].Type);
            Assert.Null(r.Data.TaskId);
        }

        [Fact]
        public async Task AsyncScalar_PopulatesTaskId()
        {
            var m = new MockHttpMessageHandler();
            m.When(HttpMethod.Post, "https://api.mobizon.kz/service/Campaign/AddRecipients")
                .Respond("application/json", @"{""code"":100,""data"":777,""message"":""""}");
            var r = await Svc(m).AddRecipientsAsync(new AddRecipientsRequest { CampaignId = 1, RecipientGroups = new[] { "9" } });
            Assert.Equal(777, r.Data.TaskId);
            Assert.Null(r.Data.Entries);
        }
    }
}
```

- [ ] **Step 2: Run, verify FAIL.**
- [ ] **Step 3: Implement converter** `AddRecipientsResultConverter.cs`:

```csharp
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mobizon.Contracts.Models.Campaigns;

namespace Mobizon.Net.Internal.Converters
{
    internal sealed class AddRecipientsResultConverter : JsonConverter<AddRecipientsResult>
    {
        public override AddRecipientsResult Read(ref Utf8JsonReader reader, System.Type t, JsonSerializerOptions options)
        {
            var result = new AddRecipientsResult();
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                result.Entries = JsonSerializer.Deserialize<List<AddRecipientEntry>>(ref reader, options);
            }
            else if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var taskId))
            {
                result.TaskId = taskId;
            }
            else if (reader.TokenType == JsonTokenType.String && int.TryParse(reader.GetString(), out var taskIdStr))
            {
                result.TaskId = taskIdStr;
            }
            else
            {
                reader.Skip();
            }
            return result;
        }

        public override void Write(Utf8JsonWriter writer, AddRecipientsResult value, JsonSerializerOptions options)
            => throw new System.NotSupportedException();
    }
}
```

Add `[JsonConverter(typeof(AddRecipientsResultConverter))]` is not possible from Contracts (no ref to Net). Instead register it in `MobizonApiClient.JsonOptions.Converters` (add `new AddRecipientsResultConverter()`). Add `[JsonPropertyName("type")] public string? Type { get; set; }` to `AddRecipientEntry`.

- [ ] **Step 4: Run, verify PASS.**
- [ ] **Step 5: Commit.** `git add -A && git commit -m "fix!: AddRecipients deserializes array (entries) or scalar (task id)"`

---

### Task 1.8: CampaignInfo/Counters + item-model audit

**Files:**
- Modify: `CampaignInfo.cs` (`CampaignCounters`), `MessageInfo.cs`, `CampaignData.cs`
- Test: `CampaignServiceTests` (getInfo structural test)

**Interfaces:**
- Produces: `CampaignCounters.UserCurrency` (string), `CampaignCounters.CampaignId` (int); `MessageInfo` gains `Uuid`,`CountryA2`,`OperatorName`,`SegUserBuy`.

- [ ] **Step 1: Write failing test** (use `campaign.getInfo.json`; assert on intact nested counter fields — NOT the scrubbed top-level `id`):

```csharp
[Fact]
public async Task GetInfoAsync_Parses_Counters_From_Fixture()
{
    var mockHttp = new MockHttpMessageHandler();
    mockHttp.When(HttpMethod.Post, "https://api.mobizon.kz/service/Campaign/GetInfo")
        .Respond("application/json", Fixtures.Load("campaign.getInfo.json"));
    var result = await CreateService(mockHttp).GetInfoAsync(1);
    Assert.NotNull(result.Data.Counters);
    Assert.Equal(1, result.Data.Counters!.TotalSegNum);          // "totalSegNum":"1"
    Assert.Equal(16.2000m, result.Data.Counters.TotalCost);      // "totalCost":"16.2000"
    Assert.Equal("KZT", result.Data.Counters.UserCurrency);      // "userCurrency":"KZT"
}
```

- [ ] **Step 2: Run, verify FAIL** (`UserCurrency` missing).
- [ ] **Step 3: Implement.** Add to `CampaignCounters`: `[JsonPropertyName("userCurrency")] public string? UserCurrency { get; set; }` and `[JsonPropertyName("campaignId")] public int CampaignId { get; set; }`. Add to `MessageInfo`: `[JsonPropertyName("uuid")] string? Uuid`, `[JsonPropertyName("countryA2")] string? CountryA2`, `[JsonPropertyName("operatorName")] string? OperatorName`, `[JsonPropertyName("segUserBuy")] decimal SegUserBuy`. (`using System.Text.Json.Serialization;` where needed.)
- [ ] **Step 4: Run, verify PASS.**
- [ ] **Step 5: Run the FULL suite.** `dotnet test Mobizon.Net.sln` — Expected: all green.
- [ ] **Step 6: Commit.** `git add -A && git commit -m "fix: CampaignCounters userCurrency/campaignId; MessageInfo extra fields"`

---

## Self-Review

**Spec coverage (§5 + §5b correctness rows):** envelope→1.1/1.2/1.3; LinkData clickCnt→1.3; Link/Get→1.4; Link/Update→1.4; Link/GetStats + types→1.5; campaign/create scalar→1.6; campaign/send scalar→1.6; addRecipients array|scalar + Type→1.7; CampaignInfo audit→1.8; Message/ContactCard migration→1.1. Coverage gaps (recipientsFile, shortenLinks, Link/List criteria, alphaname) and DX/infra/structure are Phases 2–4 (separate plans). ✓

**Placeholder scan:** every code step has complete code; the one genuinely-unconfirmed shape (`LinkStatPoint` fields) is called out with a verify step, not left as TODO. ✓

**Type consistency:** `MobizonListResult<T>` reused in 1.1/1.2/1.3. `LinkData.ClickCnt` named consistently (1.3→1.4 tests). `LinkStatsResult` (wrapper) vs `LinkStatPoint` (item) consistent (1.5). `AddRecipientsResult.Entries`/`TaskId`/`AddRecipientEntry.Type` consistent (1.7). `CreateAsync`/`SendAsync` → `MobizonResponse<int>` consistent (1.6). ✓

**Known follow-ups for execution:** several existing `LinkServiceTests` are rewritten (1.3–1.5); confirm no other test references removed types (`MessageListResponse`, `CreateCampaignResult`, `CampaignSendResult`, `LinkData.Clicks`) — grep before committing each task.

---

## Execution Handoff

Execute via subagent-driven development (already in progress for this branch). Tasks 1.0→1.8 in order; each is an independent reviewer gate. After Task 1.8, re-run `writing-plans` for Phase 2 (coverage).

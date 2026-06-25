# Mobizon.Net Phase 2 — Coverage Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development. Steps use checkbox (`- [ ]`) syntax.

**Goal:** Close the remaining API coverage gaps: `recipientsFile` upload, single-source validation, `Campaign/Create` `shortenLinks`, `Link/List` criteria, and the `alphaname` module.

**Architecture:** Per task: extend the model/service, add tests, commit. Additive where possible; breaking allowed (pre-release).

**Tech Stack:** C# 8 / `netstandard2.0` core; xunit + MockHttp; System.Text.Json.

**Spec:** `docs/superpowers/specs/2026-06-24-mobizon-net-refactor-design.md` (§7 Phase 2). **Shapes:** `docs/superpowers/notes/2026-06-24-api-shapes.md`. **Fixtures:** `tests/Mobizon.Net.Tests/Payloads/`.

## Global Constraints

- Core targets `netstandard2.0`, C# 8, `Nullable enable`, warning-clean under `TreatWarningsAsErrors`; no third-party deps in core.
- Global JSON converters parse string→int/decimal/bool and accept both string and number tokens; models need only correct `[JsonPropertyName]`.
- `MobizonListResult<T>` (Phase 1) is the list envelope; `alphaname/list` returns it.
- Fixtures validate STRUCTURE; the `alphaname.list.json` fixture has small (≤6 digit) ids that survived sanitization and IS fully usable for value assertions.
- Service tests construct `new XService(new MobizonApiClient(mockHttp.ToHttpClient(), _options))`.
- Branch `feature/api-contract-refactor`. Commit trailers:
  `Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>` + the `Claude-Session:` trailer.

---

## File Structure (Phase 2)

- Modify: `MobizonApiClient.cs` (generalize `SendMultipartAsync` file-field name), `AddRecipientsRequest.cs` (add file), `CampaignService.cs` (file route + validation), `CreateCampaignRequest.cs` (+`ShortenLinks`), `LinkListRequest.cs` (+`Criteria`), `LinkService.cs` (encode criteria).
- Create: `Models/Links/LinkListCriteria.cs`; `Models/Alphanames/AlphanameData.cs` (+`AlphanameInfo`,`AlphanameDetails`); `Services/IAlphanameService.cs`; `src/Mobizon.Net/Services/AlphanameService.cs`.
- Modify: `IMobizonClient.cs` + `MobizonClient.cs` (add `Alphanames`).
- Tests: `CampaignServiceTests`, `LinkServiceTests`, new `AlphanameServiceTests`.

---

### Task 2.1: AddRecipients file upload + single-source validation

**Files:**
- Modify: `src/Mobizon.Net/Internal/MobizonApiClient.cs:92-118`, `src/Mobizon.Contracts/Models/Campaigns/AddRecipientsRequest.cs`, `src/Mobizon.Net/Services/CampaignService.cs:181-324`
- Test: `tests/Mobizon.Net.Tests/Services/CampaignServiceTests.cs`

**Interfaces:**
- Consumes: `MobizonApiClient.SendMultipartAsync` (existing, used by ContactCardService with default file field `data[photo]`).
- Produces: `AddRecipientsRequest.RecipientsFile` (`System.IO.Stream?`) + `RecipientsFileName` (`string?`); `AddRecipientsAsync` throws `ArgumentException` unless exactly one recipient source is set; file path routes through multipart.

- [ ] **Step 1: Write failing tests** in `CampaignServiceTests`:

```csharp
[Fact]
public async Task AddRecipientsAsync_NoSource_Throws()
{
    var service = CreateService(new MockHttpMessageHandler());
    await Assert.ThrowsAsync<System.ArgumentException>(() =>
        service.AddRecipientsAsync(new AddRecipientsRequest { CampaignId = 1 }));
}

[Fact]
public async Task AddRecipientsAsync_MultipleSources_Throws()
{
    var service = CreateService(new MockHttpMessageHandler());
    await Assert.ThrowsAsync<System.ArgumentException>(() =>
        service.AddRecipientsAsync(new AddRecipientsRequest
        {
            CampaignId = 1,
            Recipients = new[] { new RecipientEntry { Recipient = "77001112233" } },
            RecipientGroups = new[] { "9" }
        }));
}

[Fact]
public async Task AddRecipientsAsync_File_SendsMultipart()
{
    var mockHttp = new MockHttpMessageHandler();
    mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/Campaign/AddRecipients")
        .With(req => req.Content is System.Net.Http.MultipartFormDataContent)
        .Respond("application/json", @"{""code"":100,""data"":555,""message"":""""}");

    var service = CreateService(mockHttp);
    using var ms = new System.IO.MemoryStream(new byte[] { 1, 2, 3 });
    var result = await service.AddRecipientsAsync(new AddRecipientsRequest
    {
        CampaignId = 1,
        RecipientsFile = ms,
        RecipientsFileName = "recipients.csv"
    });

    Assert.Equal(555, result.Data.TaskId);
    mockHttp.VerifyNoOutstandingExpectation();
}
```

- [ ] **Step 2: Run, verify FAIL.** `dotnet test tests/Mobizon.Net.Tests --filter AddRecipientsAsync`.

- [ ] **Step 3: Generalize `SendMultipartAsync`.** Add a `string fileFieldName = "data[photo]"` parameter and use it in `multipart.Add(fileContent, fileFieldName, photoFileName ?? "photo")` (`MobizonApiClient.cs:112`). The existing ContactCardService callers omit it and keep the default — unchanged.

- [ ] **Step 4: Add file fields to the request.** In `AddRecipientsRequest.cs` add:

```csharp
/// <summary>Optional CSV/file stream of recipients to upload (asynchronous). Mutually exclusive with the other recipient sources.</summary>
public System.IO.Stream? RecipientsFile { get; set; }
/// <summary>File name for <see cref="RecipientsFile"/>.</summary>
public string? RecipientsFileName { get; set; }
```

- [ ] **Step 5: Validate + route file.** At the top of `CampaignService.AddRecipientsAsync`, add the single-source guard, then route a file request to multipart:

```csharp
var sources = (request.Recipients != null ? 1 : 0)
            + (request.RecipientContacts != null ? 1 : 0)
            + (request.RecipientGroups != null ? 1 : 0)
            + (request.RecipientsFile != null ? 1 : 0);
if (sources != 1)
    throw new ArgumentException(
        "Exactly one recipient source must be set (Recipients, RecipientContacts, RecipientGroups, or RecipientsFile).",
        nameof(request));

if (request.RecipientsFile != null)
{
    var fields = new Dictionary<string, string> { ["id"] = request.CampaignId.ToString() };
    AppendParams(fields, request.Parameters); // factor the existing params[...] building into a helper
    return await _apiClient.SendMultipartAsync<AddRecipientsResult>(
        ModuleName, "AddRecipients", fields, request.RecipientsFile, request.RecipientsFileName ?? "recipients.csv",
        cancellationToken, fileFieldName: "recipientsFile").ConfigureAwait(false);
}
```

Extract the existing `params[...]` dictionary-building block from `SendAddRecipientsAsync` into a private `static void AppendParams(IDictionary<string,string> p, AddRecipientsParameters? prm)` so both the form and multipart paths share it (DRY). Keep the existing batching path for in-memory lists unchanged otherwise.

> NOTE: verify the multipart file field name `recipientsFile` against the live API during the optional re-capture; docs name the param `recipientsFile` (top-level, not under `data[]`).

- [ ] **Step 6: Run, verify PASS.** `dotnet test tests/Mobizon.Net.Tests --filter AddRecipientsAsync`. Then full suite.
- [ ] **Step 7: Commit.** `git add -A && git commit -m "feat: AddRecipients file upload (multipart) + single-source validation"`

---

### Task 2.2: Campaign/Create `shortenLinks`

**Files:**
- Modify: `src/Mobizon.Contracts/Models/Campaigns/CreateCampaignRequest.cs`, `src/Mobizon.Net/Services/CampaignService.cs:23-58`
- Test: `CampaignServiceTests`

**Interfaces:**
- Produces: `CreateCampaignRequest.ShortenLinks` (`bool?`) → sends `data[shortenLinks]` = `1`/`0`.

- [ ] **Step 1: Write failing test:**

```csharp
[Fact]
public async Task CreateAsync_WithShortenLinks_SendsFlag()
{
    var mockHttp = new MockHttpMessageHandler();
    mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/Campaign/Create")
        .WithFormData("data[type]", "2")
        .WithFormData("data[text]", "hi")
        .WithFormData("data[shortenLinks]", "1")
        .Respond("application/json", @"{""code"":0,""data"":""123"",""message"":""""}");

    var service = CreateService(mockHttp);
    await service.CreateAsync(new CreateCampaignRequest { Type = CampaignType.Bulk, Text = "hi", ShortenLinks = true });
    mockHttp.VerifyNoOutstandingExpectation();
}
```

- [ ] **Step 2: Run, verify FAIL.**
- [ ] **Step 3: Implement.** Add to `CreateCampaignRequest`:

```csharp
/// <summary>When <see langword="true"/>, the API shortens links found in the campaign text (<c>data[shortenLinks]</c>).</summary>
public bool? ShortenLinks { get; set; }
```

In `CampaignService.CreateAsync`, after the `trackShortLinkRecipients` block add:

```csharp
if (request.ShortenLinks.HasValue)
    parameters["data[shortenLinks]"] = request.ShortenLinks.Value ? "1" : "0";
```

- [ ] **Step 4: Run, verify PASS.**
- [ ] **Step 5: Commit.** `git add -A && git commit -m "feat: Campaign/Create shortenLinks option"`

---

### Task 2.3: Link/List criteria

**Files:**
- Create: `src/Mobizon.Contracts/Models/Links/LinkListCriteria.cs`
- Modify: `src/Mobizon.Contracts/Models/Links/LinkListRequest.cs`, `src/Mobizon.Net/Services/LinkService.cs:102-125`
- Test: `LinkServiceTests`

**Interfaces:**
- Produces: `LinkListRequest.Criteria` (`LinkListCriteria?`) with `Status`(`int?`), `ModeratorStatus`(`int?`), `Code`(`string?`), `FullLink`(`string?`), `Comment`(`string?`), `CreateTsFrom`/`CreateTsTo`(`string?`), `ClickCntFrom`/`ClickCntTo`(`int?`) → encoded as `criteria[<name>]`.

- [ ] **Step 1: Write failing test:**

```csharp
[Fact]
public async Task ListAsync_WithCriteria_SendsCriteriaFormData()
{
    var mockHttp = new MockHttpMessageHandler();
    mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/link/list")
        .WithFormData("criteria[status]", "1")
        .WithFormData("criteria[code]", "abc")
        .WithFormData("criteria[clickCntFrom]", "5")
        .Respond("application/json", @"{""code"":0,""data"":{""items"":[],""totalItemCount"":""0""},""message"":""""}");

    var service = CreateService(mockHttp);
    await service.ListAsync(new LinkListRequest
    {
        Criteria = new LinkListCriteria { Status = 1, Code = "abc", ClickCntFrom = 5 }
    });
    mockHttp.VerifyNoOutstandingExpectation();
}
```

- [ ] **Step 2: Run, verify FAIL.**
- [ ] **Step 3: Implement.** Create `LinkListCriteria.cs`:

```csharp
namespace Mobizon.Contracts.Models.Links
{
    /// <summary>Filter criteria for <c>link/list</c>.</summary>
    public class LinkListCriteria
    {
        public int? Status { get; set; }
        public int? ModeratorStatus { get; set; }
        public string? Code { get; set; }
        public string? FullLink { get; set; }
        public string? Comment { get; set; }
        public string? CreateTsFrom { get; set; }
        public string? CreateTsTo { get; set; }
        public int? ClickCntFrom { get; set; }
        public int? ClickCntTo { get; set; }
    }
}
```

Add `public LinkListCriteria? Criteria { get; set; }` to `LinkListRequest`. In `LinkService.ListAsync`, inside `if (request != null)` before pagination, add:

```csharp
if (request.Criteria != null)
{
    var c = request.Criteria;
    if (c.Status.HasValue) parameters["criteria[status]"] = c.Status.Value.ToString();
    if (c.ModeratorStatus.HasValue) parameters["criteria[moderatorStatus]"] = c.ModeratorStatus.Value.ToString();
    if (c.Code != null) parameters["criteria[code]"] = c.Code;
    if (c.FullLink != null) parameters["criteria[fullLink]"] = c.FullLink;
    if (c.Comment != null) parameters["criteria[comment]"] = c.Comment;
    if (c.CreateTsFrom != null) parameters["criteria[createTsFrom]"] = c.CreateTsFrom;
    if (c.CreateTsTo != null) parameters["criteria[createTsTo]"] = c.CreateTsTo;
    if (c.ClickCntFrom.HasValue) parameters["criteria[clickCntFrom]"] = c.ClickCntFrom.Value.ToString();
    if (c.ClickCntTo.HasValue) parameters["criteria[clickCntTo]"] = c.ClickCntTo.Value.ToString();
}
```

(Ensure `parameters` is initialized when only `Criteria` is set — mirror the existing `parameters = new Dictionary<string,string>()` init at the top of the `if (request != null)` block.)

- [ ] **Step 4: Run, verify PASS.**
- [ ] **Step 5: Commit.** `git add -A && git commit -m "feat: Link/List criteria filters"`

---

### Task 2.4: `alphaname` module

**Files:**
- Create: `src/Mobizon.Contracts/Models/Alphanames/AlphanameData.cs`, `src/Mobizon.Contracts/Services/IAlphanameService.cs`, `src/Mobizon.Net/Services/AlphanameService.cs`
- Modify: `src/Mobizon.Contracts/Services/IMobizonClient.cs`, `src/Mobizon.Net/MobizonClient.cs`
- Test: `tests/Mobizon.Net.Tests/Services/AlphanameServiceTests.cs`

**Interfaces:**
- Produces: `IAlphanameService.ListAsync(PaginationRequest?, CancellationToken)` → `MobizonResponse<MobizonListResult<AlphanameData>>`; `IMobizonClient.Alphanames`.

- [ ] **Step 1: Write failing test** (uses the real fixture — small ids survive sanitization):

```csharp
using System.Net.Http;
using System.Threading.Tasks;
using Mobizon.Contracts.Models.Alphanames;
using Mobizon.Contracts.Models.Common;
using Mobizon.Net.Internal;
using Mobizon.Net.Services;
using RichardSzalay.MockHttp;
using Xunit;

namespace Mobizon.Net.Tests.Services
{
    public class AlphanameServiceTests
    {
        private readonly MobizonClientOptions _options = new MobizonClientOptions
        { ApiKey = "test-key", ApiUrl = "https://api.mobizon.kz" };

        private AlphanameService CreateService(MockHttpMessageHandler m)
            => new AlphanameService(new MobizonApiClient(m.ToHttpClient(), _options));

        [Fact]
        public async Task ListAsync_Parses_Fixture()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, "https://api.mobizon.kz/service/alphaname/list")
                .Respond("application/json", Fixtures.Load("alphaname.list.json"));

            var result = await CreateService(mockHttp).ListAsync();

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Equal(1, result.Data.TotalItemCount);
            Assert.Single(result.Data.Items);
            Assert.Equal(58356, result.Data.Items[0].AlphanameId);
            Assert.Equal("Profit", result.Data.Items[0].Alphaname!.Name);
            Assert.Equal(1, result.Data.Items[0].GlobalStatus);
        }
    }
}
```

- [ ] **Step 2: Run, verify FAIL.**
- [ ] **Step 3: Implement models** `AlphanameData.cs`:

```csharp
using System.Text.Json.Serialization;

namespace Mobizon.Contracts.Models.Alphanames
{
    /// <summary>A registered sender ID (alphanumeric signature) and its moderation status.</summary>
    public class AlphanameData
    {
        [JsonPropertyName("alphanameId")] public int AlphanameId { get; set; }
        [JsonPropertyName("globalStatus")] public int GlobalStatus { get; set; }
        [JsonPropertyName("partnerStatus")] public int PartnerStatus { get; set; }
        [JsonPropertyName("isDefault")] public int IsDefault { get; set; }
        [JsonPropertyName("createTs")] public string? CreateTs { get; set; }
        [JsonPropertyName("alphaname")] public AlphanameInfo? Alphaname { get; set; }
        [JsonPropertyName("details")] public AlphanameDetails? Details { get; set; }
    }

    /// <summary>The signature value itself.</summary>
    public class AlphanameInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Type { get; set; }
        [JsonPropertyName("createTs")] public string? CreateTs { get; set; }
    }

    /// <summary>Free-form details for a signature.</summary>
    public class AlphanameDetails
    {
        public string? Description { get; set; }
    }
}
```

`IAlphanameService.cs`:

```csharp
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts.Models.Alphanames;
using Mobizon.Contracts.Models.Common;

namespace Mobizon.Contracts.Services
{
    /// <summary>Operations for listing the account's registered sender IDs (alphanames).</summary>
    public interface IAlphanameService
    {
        /// <summary>Returns the registered sender IDs (alphanumeric signatures) available to the account.</summary>
        Task<MobizonResponse<MobizonListResult<AlphanameData>>> ListAsync(
            PaginationRequest? pagination = null, CancellationToken cancellationToken = default);
    }
}
```

`AlphanameService.cs`:

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts.Models.Alphanames;
using Mobizon.Contracts.Models.Common;
using Mobizon.Contracts.Services;
using Mobizon.Net.Internal;

namespace Mobizon.Net.Services
{
    internal class AlphanameService : IAlphanameService
    {
        private const string ModuleName = "alphaname";
        private readonly MobizonApiClient _apiClient;
        public AlphanameService(MobizonApiClient apiClient) => _apiClient = apiClient;

        public Task<MobizonResponse<MobizonListResult<AlphanameData>>> ListAsync(
            PaginationRequest? pagination = null, CancellationToken cancellationToken = default)
        {
            Dictionary<string, string>? parameters = null;
            if (pagination != null)
                parameters = new Dictionary<string, string>
                {
                    ["pagination[currentPage]"] = pagination.CurrentPage.ToString(),
                    ["pagination[pageSize]"] = pagination.PageSize.ToString()
                };
            return _apiClient.SendAsync<MobizonListResult<AlphanameData>>(
                HttpMethod.Post, ModuleName, "list", parameters, cancellationToken);
        }
    }
}
```

- [ ] **Step 4: Wire into the client.** Add to `IMobizonClient` (after `NumberStopList`): `/// <summary>Gets the service for listing registered sender IDs (alphanames).</summary>` + `IAlphanameService Alphanames { get; }`. In `MobizonClient`: add the public property `public IAlphanameService Alphanames { get; }` with XML-doc, and in the private ctor add `Alphanames = new AlphanameService(apiClient);`.

- [ ] **Step 5: Run, verify PASS.** `dotnet test tests/Mobizon.Net.Tests --filter Alphaname`, then the full suite + `dotnet build Mobizon.Net.sln`.
- [ ] **Step 6: Commit.** `git add -A && git commit -m "feat: alphaname module (list registered sender IDs)"`

---

## Self-Review

**Spec coverage (§7 Phase 2):** recipientsFile→2.1; single-source validation→2.1; shortenLinks→2.2; Link/List criteria→2.3; alphaname→2.4. ✓

**Placeholder scan:** every code step has complete code; the one verify-note (multipart field name `recipientsFile`) is a runtime-confirm item, not a content gap. ✓

**Type consistency:** `AddRecipientsResult.TaskId` reused (2.1, matches Phase 1.7). `MobizonListResult<T>` reused (2.4, matches Phase 1.1). `AlphanameData`/`AlphanameInfo`/`AlphanameDetails` consistent across model, service, test. `IAlphanameService.Alphanames` named identically in interface + client. ✓

**Known follow-ups:** `IMobizonClient` is `IDisposable`; adding `Alphanames` does not affect `Dispose`. The DI extension registers `IMobizonClient` (whole client) — no separate registration needed for the new sub-service.

---

## Execution Handoff

Execute via subagent-driven development, Tasks 2.1→2.4. After 2.4, re-run `writing-plans` for Phase 3 (DX/infra).

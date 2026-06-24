# Mobizon.Net API-Contract & DX Refactor — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Align Mobizon.Net with the real Mobizon API contract, close coverage gaps, and harden DX/infra, guarded by fixture-based tests built from captured real responses.

**Architecture:** Phased. Phase 0 builds a raw capture tool and records real API JSON (incl. opt-in real sends) → committed, sanitized fixtures. Phases 1–4 fix correctness, coverage, DX/infra, and structure, each test-first against those fixtures.

**Tech Stack:** C# 8 / `netstandard2.0` (core) + `net8.0` (AspNetCore, sample, tools, tests); `System.Text.Json`; xunit + Moq + RichardSzalay.MockHttp; Polly; MinVer (added in Phase 3).

**Spec:** `docs/superpowers/specs/2026-06-24-mobizon-net-refactor-design.md`

## Global Constraints

- Core/Contracts/Webhooks target `netstandard2.0`; AspNetCore + sample + `tools/*` + tests target `net8.0`.
- `<LangVersion>8.0</LangVersion>`, `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `<GenerateDocumentationFile>true</GenerateDocumentationFile>` are global (Directory.Build.props) — all new code must compile under C# 8 and be warning-clean.
- `System.Text.Json` `8.0.5`; **no third-party dependencies** in `Mobizon.Contracts` / `Mobizon.Net` / `Mobizon.Net.Webhooks`.
- Test libs: `Microsoft.NET.Test.Sdk` 17.12.0, `xunit` 2.9.3, `Moq` 4.20.72, `RichardSzalay.MockHttp` 7.0.0.
- Raw capture output goes to `artifacts/api-captures/` (gitignored). Committed fixtures live under `tests/Mobizon.Net.Tests/Payloads/` and `tests/Mobizon.Net.Webhooks.Tests/Payloads/`, **sanitized** (phones/names/balance scrubbed).
- Real sends only behind the `--send` flag, recipients capped **≤5**, `getOwnBalance` logged before and after.
- Pre-release: breaking changes allowed; **no `[Obsolete]` shims**.
- Branch `feature/api-contract-refactor` (default branch is `master`). Commit messages end with:
  `Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>` and the `Claude-Session:` trailer.

---

## File Structure (whole refactor)

**New:**
- `tools/Mobizon.Net.ApiCapture/Mobizon.Net.ApiCapture.csproj` — dev capture console (net8.0, IsPackable=false)
- `tools/Mobizon.Net.ApiCapture/RawMobizonApi.cs` — raw form-POST caller + `BuildUrl`
- `tools/Mobizon.Net.ApiCapture/Sanitizer.cs` — pure PII scrubber
- `tools/Mobizon.Net.ApiCapture/CaptureRunner.cs` — tiered capture orchestration
- `tools/Mobizon.Net.ApiCapture/Program.cs` — config load + flag parsing
- `src/Mobizon.Contracts/Models/Common/MobizonListResult.cs` — generic list envelope
- `src/Mobizon.Contracts/Models/Links/LinkStatPoint.cs` — single stats point (rename target)
- `src/Mobizon.Contracts/Models/Alphanames/*` — alphaname module models
- `src/Mobizon.Contracts/Services/IAlphanameService.cs` + `src/Mobizon.Net/Services/AlphanameService.cs`
- `tests/Mobizon.Net.Tests/Tools/ApiCaptureTests.cs` — pure-unit tests for the tool
- `tests/Mobizon.Net.Tests/Payloads/*.json` — captured, sanitized fixtures

**Modified (Phases 1–4):** `LinkService`/`ILinkService`, `LinkData`, `UpdateLinkRequest`, `GetLinkStatsRequest`, `LinkStatsResult`, `LinkListRequest`, `CampaignService`/`ICampaignService`, `MessageService` (List return), `ContactCard*` (List return), `CreateCampaignRequest`, `AddRecipientsRequest`, `MobizonClient`, `MobizonApiClient`, `MobizonResponse`, `ServiceCollectionExtensions`, `WebhookEndpointRouteBuilderExtensions`, all hand-rolled param builders (→ `BracketNotationSerializer`), `README.md`, `.github/workflows/ci.yml`, `Directory.Build.props`, `.gitignore`, sample `Program.cs` + csproj, `CHANGELOG.md`.

---

# PHASE 0 — Capture & baseline (fully executable now)

**Deliverable:** a `tools/Mobizon.Net.ApiCapture` console that records real API JSON to `artifacts/api-captures/`, a tested pure sanitizer, committed sanitized fixtures under `Payloads/`, and a short shape report. No `src/` behavior changes.

### Task 0.0: Resolve local build lock (prerequisite)

**Files:** none (environment).

- [ ] **Step 1:** Close any IDE/process holding the build outputs (Rider/VS), then run a clean build.

Run: `dotnet build Mobizon.Net.sln -c Debug`
Expected: PASS. If it fails with `MSB3491 ... obj/...AssemblyInfoInputs.cache access denied`, delete `bin`/`obj` and confirm the Defender exclusion from project memory is active, then rebuild:

Run: `git clean -xdf -e appsettings.Development.json src tests samples && dotnet build Mobizon.Net.sln -c Debug`
Expected: PASS.

- [ ] **Step 2:** Baseline the tests.

Run: `dotnet test Mobizon.Net.sln -c Debug`
Expected: PASS (≈150 core + 49 webhook).

### Task 0.1: Scaffold the capture tool + gitignore

**Files:**
- Create: `tools/Mobizon.Net.ApiCapture/Mobizon.Net.ApiCapture.csproj`
- Modify: `Mobizon.Net.sln`, `.gitignore`

**Interfaces:**
- Produces: a runnable `net8.0` console project excluded from packaging.

- [ ] **Step 1: Create the csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <Nullable>enable</Nullable>
    <NoWarn>$(NoWarn);CS1591</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="10.0.3" />
    <PackageReference Include="Microsoft.Extensions.Configuration.EnvironmentVariables" Version="10.0.3" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Add the project to the solution**

Run: `dotnet sln Mobizon.Net.sln add tools/Mobizon.Net.ApiCapture/Mobizon.Net.ApiCapture.csproj`
Expected: "Project ... added to the solution."

- [ ] **Step 3: Ignore raw captures.** Append to `.gitignore`:

```gitignore
## API capture (raw, may contain PII)
artifacts/api-captures/
```

- [ ] **Step 4: Commit**

```bash
git add tools/Mobizon.Net.ApiCapture/Mobizon.Net.ApiCapture.csproj Mobizon.Net.sln .gitignore
git commit -m "Scaffold Mobizon.Net.ApiCapture tool"
```

### Task 0.2: Raw API caller (TDD the URL builder)

**Files:**
- Create: `tools/Mobizon.Net.ApiCapture/RawMobizonApi.cs`
- Modify: `tests/Mobizon.Net.Tests/Mobizon.Net.Tests.csproj` (ProjectReference to the tool), `tests/Mobizon.Net.Tests/Tools/ApiCaptureTests.cs`

**Interfaces:**
- Produces: `RawMobizonApi.BuildUrl(apiUrl, apiVersion, apiKey, module, method) : string`; `Task<string> CallAsync(module, method, IDictionary<string,string>? form)`.

- [ ] **Step 1: Reference the tool from the test project.** Add to `Mobizon.Net.Tests.csproj` `<ItemGroup>` of project references:

```xml
<ProjectReference Include="..\..\tools\Mobizon.Net.ApiCapture\Mobizon.Net.ApiCapture.csproj" />
```

- [ ] **Step 2: Write the failing test** in `tests/Mobizon.Net.Tests/Tools/ApiCaptureTests.cs`:

```csharp
using Mobizon.Net.ApiCapture;
using Xunit;

namespace Mobizon.Net.Tests.Tools
{
    public class ApiCaptureTests
    {
        [Fact]
        public void BuildUrl_Composes_Service_Path_With_Auth_Query()
        {
            var url = RawMobizonApi.BuildUrl("https://api.mobizon.kz/", "v1", "KEY", "user", "getOwnBalance");
            Assert.Equal(
                "https://api.mobizon.kz/service/user/getOwnBalance?output=json&api=v1&apiKey=KEY",
                url);
        }
    }
}
```

- [ ] **Step 3: Run test, verify it fails**

Run: `dotnet test tests/Mobizon.Net.Tests --filter BuildUrl_Composes_Service_Path_With_Auth_Query`
Expected: FAIL (type `RawMobizonApi` not found).

- [ ] **Step 4: Implement `RawMobizonApi`**

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Mobizon.Net.ApiCapture
{
    public sealed class RawMobizonApi
    {
        private readonly HttpClient _http;
        private readonly string _apiUrl, _apiKey, _apiVersion;

        public RawMobizonApi(HttpClient http, string apiUrl, string apiKey, string apiVersion = "v1")
        {
            _http = http; _apiUrl = apiUrl; _apiKey = apiKey; _apiVersion = apiVersion;
        }

        public static string BuildUrl(string apiUrl, string apiVersion, string apiKey, string module, string method) =>
            $"{apiUrl.TrimEnd('/')}/service/{module}/{method}?output=json&api={apiVersion}&apiKey={apiKey}";

        public async Task<string> CallAsync(string module, string method, IDictionary<string, string>? form = null)
        {
            var url = BuildUrl(_apiUrl, _apiVersion, _apiKey, module, method);
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            if (form != null && form.Count > 0) req.Content = new FormUrlEncodedContent(form);
            using var resp = await _http.SendAsync(req);
            return await resp.Content.ReadAsStringAsync();
        }
    }
}
```

- [ ] **Step 5: Run test, verify it passes**

Run: `dotnet test tests/Mobizon.Net.Tests --filter BuildUrl_Composes_Service_Path_With_Auth_Query`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add tools/Mobizon.Net.ApiCapture/RawMobizonApi.cs tests/Mobizon.Net.Tests/Mobizon.Net.Tests.csproj tests/Mobizon.Net.Tests/Tools/ApiCaptureTests.cs
git commit -m "Add raw Mobizon caller with tested URL builder"
```

### Task 0.3: PII sanitizer (TDD pure function)

**Files:**
- Create: `tools/Mobizon.Net.ApiCapture/Sanitizer.cs`
- Modify: `tests/Mobizon.Net.Tests/Tools/ApiCaptureTests.cs`

**Interfaces:**
- Produces: `Sanitizer.Scrub(string json) : string` — replaces digit runs ≥7 with `7000000XXXX`, and the value of `"balance"` with `"0.0000"`.

- [ ] **Step 1: Write the failing tests**

```csharp
[Fact]
public void Scrub_Masks_Phone_Like_Digit_Runs()
{
    var outp = Sanitizer.Scrub("{\"to\":\"77011234567\"}");
    Assert.DoesNotContain("77011234567", outp);
    Assert.Contains("7000000XXXX", outp);
}

[Fact]
public void Scrub_Masks_Balance_Value()
{
    var outp = Sanitizer.Scrub("{\"balance\":\"4043.0656\",\"currency\":\"KZT\"}");
    Assert.DoesNotContain("4043.0656", outp);
    Assert.Contains("\"currency\":\"KZT\"", outp);
}
```

- [ ] **Step 2: Run, verify fail**

Run: `dotnet test tests/Mobizon.Net.Tests --filter Scrub`
Expected: FAIL (`Sanitizer` not found).

- [ ] **Step 3: Implement `Sanitizer`**

```csharp
using System.Text.RegularExpressions;

namespace Mobizon.Net.ApiCapture
{
    public static class Sanitizer
    {
        private static readonly Regex PhoneLike = new Regex(@"\d{7,}", RegexOptions.Compiled);
        private static readonly Regex Balance = new Regex("\"balance\"\\s*:\\s*\"[^\"]*\"", RegexOptions.Compiled);

        public static string Scrub(string json)
        {
            var s = PhoneLike.Replace(json, "7000000XXXX");
            s = Balance.Replace(s, "\"balance\":\"0.0000\"");
            return s;
        }
    }
}
```

- [ ] **Step 4: Run, verify pass**

Run: `dotnet test tests/Mobizon.Net.Tests --filter Scrub`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add tools/Mobizon.Net.ApiCapture/Sanitizer.cs tests/Mobizon.Net.Tests/Tools/ApiCaptureTests.cs
git commit -m "Add tested PII sanitizer for capture fixtures"
```

### Task 0.4: Capture runner + Program (Tiers 1–3)

**Files:**
- Create: `tools/Mobizon.Net.ApiCapture/CaptureRunner.cs`, `tools/Mobizon.Net.ApiCapture/Program.cs`

**Interfaces:**
- Consumes: `RawMobizonApi`, `Sanitizer`.
- Produces: raw files `artifacts/api-captures/<module>.<method>.json`; honors `--send` (Tier 3), `--tier2` (default on), reads `Mobizon:ApiKey/ApiUrl/TestRecipient/TestGroupId`.

- [ ] **Step 1: Implement `CaptureRunner`** — writes each call's raw JSON; Tier 1 always; Tier 2 link CRUD; Tier 3 (only if `send==true`) does `message/sendSmsMessage` + status poll, then campaign lifecycle, capped at ≤5 recipients, logging balance before/after. (Full method bodies: one `await Capture("module","method", form)` helper that calls `RawMobizonApi.CallAsync`, writes `Path.Combine(outDir, $"{module}.{method}.json")`, and echoes the response code.)

```csharp
// Key helper (full file created in this step):
private async Task Capture(string module, string method, IDictionary<string,string>? form = null)
{
    var json = await _api.CallAsync(module, method, form);
    var path = Path.Combine(_outDir, $"{module}.{method}.json");
    Directory.CreateDirectory(_outDir);
    File.WriteAllText(path, json);
    Console.WriteLine($"[capture] {module}/{method} -> {path}");
}
```

Tier 1 calls: `user/getOwnBalance`; `message/list`; `campaign/list`; `link/list`; `link/getStats` (needs an id from `link/list`); `alphaname/list`; `taskqueue/getStatus` (skip if no known id). Tier 2: `link/create`→`get`→`getStats`→`update`→`delete`. Tier 3 (`--send`): `message/sendSmsMessage` → poll `message/getSMSStatus`; `campaign/create`→`addRecipients`→`send`→poll `taskqueue/getStatus`→`campaign/getInfo`/`campaign/getStatusByList`→`campaign/delete`.

- [ ] **Step 2: Implement `Program`** — load config (mirror sample `Program.cs:19-24`), parse `--send`, build `HttpClient`, run `CaptureRunner`. Refuse Tier 3 if `TestRecipient`/`TestGroupId` missing.

- [ ] **Step 3: Build**

Run: `dotnet build tools/Mobizon.Net.ApiCapture`
Expected: PASS (warning-clean).

- [ ] **Step 4: Commit**

```bash
git add tools/Mobizon.Net.ApiCapture/CaptureRunner.cs tools/Mobizon.Net.ApiCapture/Program.cs
git commit -m "Add tiered capture runner (read-only, link CRUD, opt-in sends)"
```

### Task 0.5: Run capture + commit sanitized fixtures + shape report (USER-RUN)

**Files:**
- Create: `tests/Mobizon.Net.Tests/Payloads/*.json` (sanitized), `docs/superpowers/notes/2026-06-24-api-shapes.md`

- [ ] **Step 1 (USER):** Set `Mobizon:ApiKey`, `Mobizon:TestRecipient` (and a small `Mobizon:TestGroupId`) in `samples/.../appsettings.Development.json` or env, then run read-only first:

Run: `! dotnet run --project tools/Mobizon.Net.ApiCapture`
Then the full set incl. real sends:
Run: `! dotnet run --project tools/Mobizon.Net.ApiCapture -- --send`
Expected: files in `artifacts/api-captures/`; balance logged before/after.

- [ ] **Step 2:** Sanitize each captured file through `Sanitizer.Scrub` (add a `--sanitize <in> <out>` mode or a tiny script) into `tests/Mobizon.Net.Tests/Payloads/<endpoint>.json`. Manually verify no residual PII.

- [ ] **Step 3:** Write `docs/superpowers/notes/2026-06-24-api-shapes.md` recording, per endpoint, the real top-level shape and field names — resolving the spec's §10 open items (`Campaign/List` item model, `link/getStats` `type` values + item shape, `alphaname` fields, send/DLR shapes).

- [ ] **Step 4: Commit**

```bash
git add tests/Mobizon.Net.Tests/Payloads docs/superpowers/notes/2026-06-24-api-shapes.md
git commit -m "Add sanitized API fixtures and response-shape report"
```

> **GATE:** Phases 1–4 task details below are finalized against the Step 3 shape report. Before executing Phase 1, re-invoke `writing-plans` to expand the roadmap below into bite-sized TDD tasks using the captured fixtures as the exact assertion source. The roadmap is concrete (doc-verified), but fixture bytes are the source of truth.

---

# PHASE 1 — Correctness (breaking) — roadmap

Each task is TDD: write a deserialization test that loads the Phase 0 fixture via MockHttp, assert the corrected model; then fix the model/service; then assert request encoding. Commit per task.

### Task 1.1: Generic list envelope
- **Files:** Create `src/Mobizon.Contracts/Models/Common/MobizonListResult.cs`; Test `tests/.../Common/MobizonListResultTests.cs`.
- **Produces:** `public class MobizonListResult<T> { IReadOnlyList<T> Items {get;set;} = Array.Empty<T>(); int TotalItemCount {get;set;} }`.
- **Test:** deserialize `{ "items":[...], "totalItemCount": N }` → `Items.Count`, `TotalItemCount` correct.

### Task 1.2: Campaign/List → envelope
- **Files:** `src/Mobizon.Net/Services/CampaignService.cs:99-165`, `src/Mobizon.Contracts/Services/ICampaignService.cs:88`; Test `tests/.../Services/CampaignServiceTests.cs` (currently empty).
- **Change:** return `MobizonResponse<MobizonListResult<CampaignData>>` (item model confirmed `CampaignData` vs `CampaignInfo` from shape report). 
- **Test:** load `campaign.list.json` fixture → assert items + count; assert request encodes `criteria[...]`/`pagination[...]`/`sort[...]`.

### Task 1.3: Link/List → envelope
- **Files:** `LinkService.cs:102-125`, `ILinkService.cs:93`; Test `tests/.../Services/LinkServiceTests.cs`.
- **Change:** return `MobizonResponse<MobizonListResult<LinkData>>`.

### Task 1.4: Link/GetStats → `{items, totals}`
- **Files:** Create `LinkStatPoint.cs`; rewrite `LinkStatsResult.cs` to `{ IReadOnlyList<LinkStatPoint> Items; int Totals; }`; `LinkService.cs:80-100`, `ILinkService.cs:79`.
- **Change:** rename old single-point `LinkStatsResult` → `LinkStatPoint` (fields confirmed from fixture); return `MobizonResponse<LinkStatsResult>`. Confirm `type` param values from shape report.

### Task 1.5: LinkData field fixes
- **Files:** `src/Mobizon.Contracts/Models/Links/LinkData.cs`.
- **Change:** `[JsonPropertyName("clickCnt")] public int ClickCnt`; add `RedirectCnt` (`redirectCnt`), `ModeratorStatus` (`moderatorStatus`), `CreateTs` (`createTs`), `ModeratorComment` (`moderatorComment`). Remove the always-zero `Clicks`.
- **Test:** deserialize `link.get.json` → `ClickCnt` non-default when fixture has clicks.

### Task 1.6: Link/Get by id|code|shortLink (three methods)
- **Files:** `ILinkService.cs`, `LinkService.cs:56-66`.
- **Produces:** `GetByIdAsync(int id, …)`, `GetByCodeAsync(string code, …)`, `GetByShortLinkAsync(string shortLink, …)` — each sends only its key (`id`/`code`/`shortLink`).
- **Test:** each method encodes the correct single param.

### Task 1.7: Link/Update by Id (drop fullLink)
- **Files:** `UpdateLinkRequest.cs` (`Code`→`int Id`, remove `FullLink`), `LinkService.cs:127-149`.
- **Test:** request encodes `id` + `data[status|expirationDate|comment]`, **no** `data[fullLink]`.

### Task 1.8: Message/List + ContactCard/List onto generic envelope
- **Files:** replace `MessageListResponse`/`ContactCardListResponse` usages with `MobizonListResult<MessageInfo>`/`MobizonListResult<ContactCardData>`; update `MessageService.cs:88`, `ContactCardService`/`ContactCardQuery.cs:83-99`.
- **Test:** existing message/contactcard list tests still pass against fixtures; `ContactCardQuery` unaffected (`Items`/`TotalItemCount` preserved).

### Task 1.9: List-item model audit
- **Files:** `MessageInfo.cs` (add `uuid`,`countryA2`,`operatorName`,`segUserBuy` if present), `CampaignData.cs`.
- **Test:** deserialize fixtures, assert previously-missing fields populate.

---

# PHASE 2 — Coverage — roadmap

### Task 2.1: AddRecipients file upload + multipart
- **Files:** `AddRecipientsRequest.cs` (add `Stream? RecipientsFile`, `string? RecipientsFileName`), `CampaignService.cs:269-324` (route to existing `MobizonApiClient.SendMultipartAsync` when a file is set).
- **Test:** with a file set, the client sends multipart with `data[recipientsFile]`; without, form-url-encoded as today.

### Task 2.2: AddRecipients single-source validation
- **Files:** `CampaignService.cs:181-186`.
- **Change:** throw `ArgumentException` unless exactly one of Recipients/RecipientContacts/RecipientGroups/RecipientsFile is set.
- **Test:** mixed sources → `ArgumentException`; single source → OK.

### Task 2.3: Campaign/Create shortenLinks
- **Files:** `CreateCampaignRequest.cs` (add `bool? ShortenLinks`), `CampaignService.cs:53-55`.
- **Test:** request encodes `data[shortenLinks]=1` when true.

### Task 2.4: Link/List criteria
- **Files:** `LinkListRequest.cs` (+`LinkListCriteria`: status, moderatorStatus, code, fullLink, comment, createTs/clickCnt ranges), `LinkService.cs:102-125`.
- **Test:** criteria encode as `criteria[...]`.

### Task 2.5: alphaname module
- **Files:** Create `Models/Alphanames/AlphanameData.cs` (fields from shape report), `Services/IAlphanameService.cs` (`Task<MobizonResponse<MobizonListResult<AlphanameData>>> ListAsync(…)`), `Services/AlphanameService.cs`; wire `IMobizonClient.Alphanames` + `MobizonClient` ctor.
- **Test:** deserialize `alphaname.list.json`; client exposes `Alphanames`.

---

# PHASE 3 — DX / infra — roadmap

### Task 3.1: README rewrite
- **Files:** `README.md`.
- **Change:** compiling examples (`Parameters = new SmsMessageParameters{...}`, `CampaignType` enum, real `AddRecipientsRequest`, `SmsStatus` enum); correct response-code table from `MobizonResponseCode`; document all 8 services + ContactCards LINQ + webhooks; remove false "Contracts zero-deps"; fix Polly example (`CircuitBreakerFailureThreshold`, no `Timeout`, backoff 1/2/4 s); remove `/service/` from URLs.
- **Verify:** copy each C# block into a scratch file and `dotnet build` it.

### Task 3.2: CI branches + MinVer
- **Files:** `.github/workflows/ci.yml` (triggers `master`,`develop`; PRs to same), `Directory.Build.props` (remove hardcoded `<Version>`, add `MinVer` PackageReference + `<MinVerTagPrefix>v</MinVerTagPrefix>`).
- **Verify:** `dotnet build` resolves version from a `v*` tag locally.

### Task 3.3: DI typed client + timeout fix
- **Files:** `ServiceCollectionExtensions.cs:72-84` (use `AddHttpClient<IMobizonClient, MobizonClient>` or register so the factory owns lifetime), `MobizonClient.cs:91-92` (apply `options.Timeout` only when `ownsHttpClient`).
- **Test:** resolved client works; injected HttpClient `Timeout` not mutated; update DI tests + README "scoped/singleton" wording to match.

### Task 3.4: Webhook endpoint hardening
- **Files:** `WebhookEndpointRouteBuilderExtensions.cs:55-76`.
- **Change:** enforce a max body size (configurable on `MobizonWebhookOptions`); return `Results.Json(problem, status)` for 400/403; XML-doc the verify→enqueue→200 pattern.
- **Test:** oversized body → 413/400; signature mismatch → 403 JSON; success → 200.

### Task 3.5: Sample + validation
- **Files:** `samples/.../Program.cs:74` (comment out the auto-run), `*.ConsoleSample.csproj:11` (`Condition="Exists(...)"`), light pre-flight null/empty checks on public request entry points.
- **Test:** sample builds from a clean clone (no `appsettings.Development.json`).

---

# PHASE 4 — Structure / naming — roadmap

### Task 4.1: Adopt BracketNotationSerializer
- **Files:** `MessageService.cs`, `CampaignService.cs`, `LinkService.cs`, `ContactGroupService.cs`, `NumberStopListService.cs`; reuse `src/Mobizon.Net/Internal/BracketNotationSerializer.cs`.
- **Change:** replace hand-rolled `criteria[i]`/`pagination[...]`/`sort[...]` with serializer calls.
- **Test:** existing service tests stay green (encoding unchanged); add round-trip tests.

### Task 4.2: Consolidate enum↔code mapping
- **Files:** move `SmsStatusToApiCode`/`CampaignCommonStatusToApiCode` (`MessageService.cs:169-198`) next to the converters in `Internal/Converters/`.

### Task 4.3: Module-name casing + Data invariant + query semantics
- **Files:** `LinkService.cs` (`"link"`→`"Link"` etc. — verify still works); `MobizonResponse.cs:28` (document invariant + guard in `MobizonApiClient.SendCoreAsync` for null `data` on success); `ContactCardQuery.cs:35` (combine predicates via `AndAlso` or document single-`Where`; document `Skip` page math).
- **Test:** guard test for null `data`; `Where().Where()` behavior test.

### Task 4.4: CHANGELOG
- **Files:** `CHANGELOG.md` — list breaking changes (Link contracts, list envelopes) for the 1.0 release.

---

## Self-Review

**Spec coverage:** Every §5 matrix row maps to a task — envelopes (1.1–1.3, 1.8), Link contracts (1.4–1.7), clickCnt (1.5), recipientsFile (2.1), validation (2.2), shortenLinks (2.3), Link/List criteria (2.4), alphaname (2.5), CI (3.2), versioning (3.2), DI (3.3), timeout (3.3), README (3.1), XML `/service/` (3.1), webhook endpoint (3.4), sample (3.5), BracketNotationSerializer (4.1), enum mapping (4.2), casing/Data/Where (4.3). Capture (C3) = Phase 0. ✓

**Placeholder scan:** Phase 0 steps contain complete code. Phases 1–4 are an explicit roadmap gated behind the Phase 0 capture (per C3) — to be expanded into bite-sized steps via a second `writing-plans` pass using the fixtures. This deferral is data-driven, not a content placeholder.

**Type consistency:** `MobizonListResult<T>` (1.1) is reused in 1.2/1.3/1.8/2.5. `LinkStatPoint` (1.4) vs `LinkStatsResult` wrapper consistent. `ClickCnt` naming consistent in 1.5. Three Get methods named identically in 1.6 and File Structure.

---

## Execution Handoff

Execute **Phase 0 only** now (it is fully specified and unblocks everything). After Phase 0's shape report + fixtures land, re-run `writing-plans` to expand Phases 1–4 into bite-sized TDD steps against the real fixtures.

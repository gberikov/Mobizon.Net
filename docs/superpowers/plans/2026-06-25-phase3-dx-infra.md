# Mobizon.Net Phase 3 — DX / Infrastructure Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development. Steps use checkbox (`- [ ]`) syntax.

**Goal:** Fix developer-experience and infrastructure debt: DI lifetime + HttpClient timeout, webhook endpoint hardening, sample fixes + pre-flight validation, a corrected README, and working CI + tag-driven versioning.

**Tech Stack:** C# 8 / `netstandard2.0` core + `net8.0`; xunit + MockHttp; GitHub Actions; MinVer (build-only).

**Spec:** `docs/superpowers/specs/2026-06-24-mobizon-net-refactor-design.md` (§7 Phase 3). **Truth for APIs:** the current code/tests after Phases 1–2 (commits up to `6660a3d`).

## Global Constraints

- Core targets `netstandard2.0`, C# 8, `Nullable enable`, warning-clean under `TreatWarningsAsErrors`.
- **No third-party RUNTIME deps** in `Mobizon.Contracts`/`Mobizon.Net`/`Mobizon.Net.Webhooks`. MinVer is allowed because it is build-only (`PrivateAssets="all"` — never flows to the nupkg).
- Default branch is `master`; active dev branch `develop`. CI must trigger on both.
- README examples MUST compile against the current API.
- Service tests construct `new XService(new MobizonApiClient(mockHttp.ToHttpClient(), _options))`; DI tests use `ServiceCollection`.
- Branch `feature/api-contract-refactor`. Commit trailers:
  `Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>` + the `Claude-Session:` trailer.

---

## File Structure (Phase 3)

- Modify: `src/Mobizon.Net/MobizonClient.cs` (timeout guard), `src/Mobizon.Net.Extensions.DependencyInjection/ServiceCollectionExtensions.cs` (lifetime), `src/Mobizon.Net.Webhooks.AspNetCore/MobizonWebhookOptions.cs` + `WebhookEndpointRouteBuilderExtensions.cs` (hardening), `samples/Mobizon.Net.ConsoleSample/Program.cs` + `.csproj`, `src/Mobizon.Net/Services/MessageService.cs` + `CampaignService.cs` (validation guards), `README.md`, `.github/workflows/ci.yml`, `Directory.Build.props`.
- Tests: `MobizonClientTests`, `ServiceCollectionExtensionsTests`, `AspNetCoreWebhookEndpointTests` (in Webhooks.Tests), `MessageServiceTests`/`CampaignServiceTests`.

---

### Task 3.1: DI lifetime (transient) + don't mutate an injected HttpClient's Timeout

**Files:**
- Modify: `src/Mobizon.Net/MobizonClient.cs:86-93`, `src/Mobizon.Net.Extensions.DependencyInjection/ServiceCollectionExtensions.cs:74`
- Test: `tests/Mobizon.Net.Tests/MobizonClientTests.cs`

**Interfaces:**
- Produces: `MobizonClient` applies `options.Timeout` to the `HttpClient` ONLY when it owns it; DI registers `IMobizonClient` as **transient** (so `IHttpClientFactory` rotates handlers per resolution).

- [ ] **Step 1: Write the failing test** in `MobizonClientTests.cs`:

```csharp
[Fact]
public void Ctor_WithInjectedHttpClient_DoesNotMutateItsTimeout()
{
    using var http = new System.Net.Http.HttpClient();
    var original = http.Timeout; // default 100s
    using var client = new MobizonClient(http, new MobizonClientOptions
    {
        ApiKey = "k", ApiUrl = "https://api.mobizon.kz", Timeout = System.TimeSpan.FromSeconds(5)
    });
    Assert.Equal(original, http.Timeout); // injected client untouched
}
```

- [ ] **Step 2: Run, verify FAIL.** `dotnet test tests/Mobizon.Net.Tests --filter Ctor_WithInjectedHttpClient_DoesNotMutateItsTimeout` (currently the ctor sets `_httpClient.Timeout` unconditionally → FAIL).

- [ ] **Step 3: Implement.** In `MobizonClient.cs` private ctor, guard the timeout assignment:

```csharp
if (ownsHttpClient && options.Timeout > TimeSpan.Zero)
    _httpClient.Timeout = options.Timeout;
```

- [ ] **Step 4: DI lifetime → transient.** In `ServiceCollectionExtensions.AddMobizonCore`, change `services.AddSingleton<IMobizonClient>(...)` to `services.AddTransient<IMobizonClient>(...)` (keep the `factory.CreateClient("Mobizon")` body). Update the XML-doc on both `AddMobizon` overloads from "as a singleton" to "as a transient service backed by `IHttpClientFactory`".

- [ ] **Step 5: Run, verify PASS.** `dotnet test tests/Mobizon.Net.Tests` (new test passes; existing DI tests still pass).
- [ ] **Step 6: Commit.** `git add -A && git commit -m "fix: transient IMobizonClient; don't mutate injected HttpClient.Timeout"`

---

### Task 3.2: Webhook endpoint hardening (body cap + JSON problem responses)

**Files:**
- Modify: `src/Mobizon.Net.Webhooks.AspNetCore/MobizonWebhookOptions.cs`, `src/Mobizon.Net.Webhooks.AspNetCore/WebhookEndpointRouteBuilderExtensions.cs:51-77`
- Test: `tests/Mobizon.Net.Webhooks.Tests/AspNetCoreWebhookEndpointTests.cs`

**Interfaces:**
- Produces: `MobizonWebhookOptions.MaxRequestBodyBytes` (`int`, default `262144`); `HandleRequestAsync` returns 413 JSON when the body exceeds the cap, and JSON bodies for 403/400.

- [ ] **Step 1: Write failing tests** (follow the existing `AspNetCoreWebhookEndpointTests` setup — a `DefaultHttpContext` whose `RequestServices` provides `IWebhookProcessor` + `MobizonWebhookOptions`; read that file first to reuse its helpers). Add:
  - a test that sets `context.Request.ContentLength` greater than `MaxRequestBodyBytes` and asserts the result is a 413 status;
  - a test that a signature-mismatch result carries a JSON content-type/body (not an empty 403).

  Use the file's existing helper to build the context and invoke `WebhookEndpointRouteBuilderExtensions.HandleRequestAsync`. Mirror its assertion style for status codes (e.g. casting `IResult` or executing it against the context).

- [ ] **Step 2: Run, verify FAIL.**

- [ ] **Step 3: Implement.** Add to `MobizonWebhookOptions`:

```csharp
/// <summary>Maximum accepted request body size in bytes. Requests larger than this are rejected with 413. Default 262144 (256 KiB).</summary>
public int MaxRequestBodyBytes { get; set; } = 262144;
```

In `HandleRequestAsync`, before reading the body, reject oversized requests, and return JSON for the error statuses:

```csharp
if (context.Request.ContentLength is long len && len > options.MaxRequestBodyBytes)
    return Results.Json(new { error = "payload_too_large" }, statusCode: StatusCodes.Status413PayloadTooLarge);

// ... read body, process ...

switch (result.Status)
{
    case WebhookProcessStatus.Ok:
        await handler(result.Event!, context.RequestAborted).ConfigureAwait(false);
        return Results.Ok();
    case WebhookProcessStatus.SignatureMismatch:
        return Results.Json(new { error = "signature_mismatch" }, statusCode: StatusCodes.Status403Forbidden);
    default:
        return Results.Json(new { error = "parse_error" }, statusCode: StatusCodes.Status400BadRequest);
}
```

Update the method's XML-doc to mention the 413 + JSON bodies and reiterate the verify→enqueue→200 guidance.

- [ ] **Step 4: Run, verify PASS.** `dotnet test tests/Mobizon.Net.Webhooks.Tests`.
- [ ] **Step 5: Commit.** `git add -A && git commit -m "feat: webhook endpoint body-size cap + JSON problem responses"`

---

### Task 3.3: Sample fixes + pre-flight validation

**Files:**
- Modify: `samples/Mobizon.Net.ConsoleSample/Program.cs:74`, `samples/Mobizon.Net.ConsoleSample/Mobizon.Net.ConsoleSample.csproj:11`, `src/Mobizon.Net/Services/MessageService.cs`, `src/Mobizon.Net/Services/CampaignService.cs:23`
- Test: `MessageServiceTests`, `CampaignServiceTests`

**Interfaces:**
- Produces: `SendSmsMessageAsync(null)` and `CampaignService.CreateAsync(null)` throw `ArgumentNullException`; sample no longer auto-runs a block; sample csproj only copies `appsettings.Development.json` when it exists.

- [ ] **Step 1: Write failing tests:**

```csharp
// MessageServiceTests
[Fact]
public async Task SendSmsMessageAsync_NullRequest_Throws()
{
    var service = CreateService(new MockHttpMessageHandler());
    await Assert.ThrowsAsync<System.ArgumentNullException>(() => service.SendSmsMessageAsync(null!));
}

// CampaignServiceTests
[Fact]
public async Task CreateAsync_NullRequest_Throws()
{
    var service = CreateService(new MockHttpMessageHandler());
    await Assert.ThrowsAsync<System.ArgumentNullException>(() => service.CreateAsync(null!));
}
```

- [ ] **Step 2: Run, verify FAIL.**

- [ ] **Step 3: Implement guards.** At the top of `MessageService.SendSmsMessageAsync`: `if (request == null) throw new ArgumentNullException(nameof(request));` (add `using System;` if needed). Same at the top of `CampaignService.CreateAsync`. (These are the most-used write entry points; `AddRecipientsAsync` already validates.)

- [ ] **Step 4: Fix the sample.** In `Program.cs`, comment out the un-commented `await ContactCardSamples.ListAsync(client);` (line 74) so the "Uncomment a block in Program.cs to run a sample." message is accurate. In `Mobizon.Net.ConsoleSample.csproj`, add `Condition="Exists('appsettings.Development.json')"` to the `appsettings.Development.json` `<Content Include=...>` item so a clean clone (without that gitignored file) still builds.

- [ ] **Step 5: Run, verify PASS** + full build. `dotnet test tests/Mobizon.Net.Tests`; `dotnet build Mobizon.Net.sln -c Debug`.
- [ ] **Step 6: Commit.** `git add -A && git commit -m "fix: sample auto-run + conditional dev config; null-request guards"`

---

### Task 3.4: README rewrite (compiling examples, correct facts)

**Files:**
- Modify: `README.md`
- Verify: a throwaway scratch build of the README's C# blocks.

**Interfaces:** none (docs). The current correct API is in `src/Mobizon.Contracts/Services/*.cs` and the passing tests — READ those for exact signatures before writing examples.

- [ ] **Step 1: Read the current API surface** — `IMobizonClient` (9 members incl. `ContactCards`, `NumberStopList`, `ContactGroups`, `Alphanames`), `IMessageService`, `ICampaignService`, `ILinkService`, `IContactCardSet`/`IContactCardQuery`, `MobizonResponseCode`, and the resilience options — so every example matches reality.

- [ ] **Step 2: Rewrite README.md** with these specific corrections:
  - **Features/modules**: list all services (Messages, Campaigns, Links, User, TaskQueue, ContactGroups, ContactCards, NumberStopList, Alphanames) and the ContactCards LINQ-style query API; drop the "5 API modules" claim.
  - **Send SMS example**: `Validity`/`MessageClass`/`DeferredTo` live on `Parameters` (`new SmsMessageParameters { Validity = TimeSpan.FromMinutes(60) }`), not on `SendSmsMessageRequest`.
  - **Campaign create**: `Type = CampaignType.Bulk` (enum, not `1`); `CreateAsync` returns `MobizonResponse<int>` (the campaign id is `result.Data`).
  - **AddRecipients**: real shape — `Recipients`/`RecipientContacts`/`RecipientGroups`/`RecipientsFile` (no `Type`/`Data`); show `Recipients = new[] { new RecipientEntry { Recipient = "7700..." } }`.
  - **Campaign send**: `SendAsync` returns `MobizonResponse<int>`; when `Code == MobizonResponseCode.BackgroundTask`, `Data` is the task id.
  - **List endpoints**: return `MobizonListResult<T>` — iterate `result.Data.Items`, total via `result.Data.TotalItemCount`.
  - **Links**: `GetByCodeAsync`/`GetByIdAsync`/`GetByShortLinkAsync`; `LinkData.ClickCnt` (not `Clicks`); `GetStatsAsync` returns `LinkStatsResult { Items, Totals }`; `LinkStatsType` has Monthly/Daily/Hourly/Minute; `UpdateLinkRequest { Id, Status, ExpirationDate, Comment }`.
  - **Message list criteria**: `Status` is `SmsStatus` (enum), not an int.
  - **Response-code table**: replace with the ACTUAL `MobizonResponseCode` members/values (Success=0, ValidationError=1, NotFound=2, UnknownError=3, InvalidModule=4, InvalidMethod=5, InvalidFormat=6, LoginError=8, AccessDenied=9, SaveError=10, MissingParameters=11, InvalidParameter=12, WrongServer=13, AccountBlocked=14, OperationError=15, RateLimitExceeded=30, BulkPartialSuccess=98, BulkCompleteFailure=99, BackgroundTask=100, ServiceError=999).
  - **Contracts deps**: remove "Zero dependencies" — it references `System.Text.Json`.
  - **DI**: state `IMobizonClient` is registered **transient** (matches Task 3.1).
  - **Polly**: use the real option names (`RetryCount`, `RetryBaseDelay`, `CircuitBreakerFailureThreshold`, `CircuitBreakerDuration`); remove the non-existent `CircuitBreakerCount`/`Timeout`; correct default backoff to 1s/2s/4s; note `AddMobizonResilience` adds retry + circuit breaker (no timeout policy).
  - **URLs**: remove the trailing `/service/` from every `ApiUrl` example (the client appends `/service/`); keep `https://api.mobizon.kz` etc.
  - **Alphanames**: short example `await client.Alphanames.ListAsync()`.
  - **Contributing**: branch from `master` (not `main`).

- [ ] **Step 3: Verify examples compile.** Create a throwaway file `samples/Mobizon.Net.ConsoleSample/ReadmeSnippets.cs.txt`... instead: paste each C# block into a temporary method in a scratch console (or a `#if README_CHECK` region), run `dotnet build`, confirm 0 errors, then remove the scratch. Do NOT commit the scratch. Report which blocks you compiled.

- [ ] **Step 4: Commit.** `git add README.md && git commit -m "docs: rewrite README to match the corrected API"`

---

### Task 3.5: CI branches + tag-driven versioning (MinVer)

**Files:**
- Modify: `.github/workflows/ci.yml`, `Directory.Build.props`

**Interfaces:** none (build/CI).

- [ ] **Step 1: Fix CI triggers.** In `.github/workflows/ci.yml`, change both the `push.branches` and `pull_request.branches` from `- main` to:

```yaml
    branches:
      - master
      - develop
```

(Keep the `tags: ['v*']` push trigger and the tag-gated publish job.)

- [ ] **Step 2: Tag-driven version via MinVer.** In `Directory.Build.props`, remove `<Version>1.0.0</Version>`, and add:

```xml
  <ItemGroup>
    <PackageReference Include="MinVer" Version="6.0.0" PrivateAssets="all" />
  </ItemGroup>
  <PropertyGroup>
    <MinVerTagPrefix>v</MinVerTagPrefix>
  </PropertyGroup>
```

This makes the package version derive from the latest `v*` git tag (e.g. tag `v1.2.0` → package `1.2.0`); untagged builds get a `0.0.0-alpha.*` prerelease version. `PrivateAssets="all"` keeps MinVer out of the shipped nupkg dependencies.

- [ ] **Step 3: Verify build still works** and a version is computed: `dotnet build Mobizon.Net.sln -c Release`. Expected: build succeeds, 0 warnings. Confirm MinVer ran (no error about shallow clone). Optionally `dotnet pack src/Mobizon.Net -c Release -o ./artifacts` and confirm the produced `.nupkg` filename carries a computed version (then delete `./artifacts`).

- [ ] **Step 4: Commit.** `git add .github/workflows/ci.yml Directory.Build.props && git commit -m "ci: trigger on master/develop; version from git tags via MinVer"`

---

## Self-Review

**Spec coverage (§7 Phase 3):** README→3.4; CI branches→3.5; versioning→3.5; DI lifetime→3.1; timeout mutation→3.1; webhook endpoint (body cap + JSON)→3.2; sample fixes→3.3; pre-flight validation→3.3. ✓

**Placeholder scan:** testable tasks (3.1/3.2/3.3) have complete test+impl code; 3.4 (README) and 3.5 (CI/MinVer) are doc/config with concrete edits + a build/compile verification step instead of unit tests (appropriate — there is nothing to unit-test). No "TODO"/"fill in". ✓

**Type consistency:** `MaxRequestBodyBytes` named consistently (3.2). DI lifetime wording (transient) consistent between code (3.1) and README (3.4). README examples reference the exact post-Phase-1/2 API names. ✓

**Known follow-ups:** populating `Mobizon.Net.IntegrationTests` is NOT in this plan (it remains an empty stub; either populate it or document it — defer to the final review). MinVer untagged builds produce a prerelease version — that's expected until the first `v*` tag.

---

## Execution Handoff

Execute via subagent-driven development, Tasks 3.1→3.5. After 3.5, re-run `writing-plans` for Phase 4 (structure/naming).

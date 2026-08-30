# Release Readiness (0.1.0) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the six Mobizon.Net packages publishable as `0.1.0`: truthful metadata + SourceLink, API key out of the URL, actionable exceptions, culture-proof formatting, idempotent-only retries, one flat namespace per package, consistent types, docs that match the code.

**Architecture:** Four phases. Phase 0 (hygiene/metadata/CI/multi-target) changes no public C#. Phase 1 (transport/exceptions/formatting/Polly) changes internals of `MobizonApiClient` and the Polly package. Phase 2 (public surface) starts with a mechanical namespace collapse, then applies the type/naming table from the spec one member group at a time. Phase 3 is docs, the console sample and the release checklist. Every task ends green (`dotnet build -c Release` with 0 warnings, `dotnet test`).

**Tech Stack:** C# 8.0, `netstandard2.0;net8.0` (core) / `net8.0;net10.0` (AspNetCore), System.Text.Json, Microsoft.Extensions.Http(.Polly) 8.0.x, MinVer 6, xUnit 2.9 + RichardSzalay.MockHttp 7, .NET SDK 10.0.400 on the dev box.

**Spec:** `docs/superpowers/specs/2026-08-30-release-readiness-design.md` — read it first; each task cites the decision (D1…D10) it implements.

## Global Constraints

- Work on branch `feature/release-readiness` off `develop`. Commit after every task with the message given in the task.
- `TreatWarningsAsErrors=true` stays. Build must be **0 warnings** on every target framework. Run the full suite from the repo root: `dotnet build -c Release && dotnet test --no-build -c Release`.
- C# 8.0 only (no records, no `init`, no target-typed `new`, no file-scoped namespaces). Nullable reference types enabled. No new third-party runtime dependencies (SourceLink is build-only).
- Wire format for date-times is `yyyy-MM-dd HH:mm:ss`, dates `yyyy-MM-dd`, always `CultureInfo.InvariantCulture`, always via `ApiFormat` (after Task 5).
- Errors = `MobizonApiException` (API envelope code ≠ 0/100) or `MobizonException` (transport/parse). Success = no exception.
- Pre-release: breaking changes are allowed and expected. Do **not** keep obsolete shims/aliases.
- First public version is **`0.1.0`** (tag `v0.1.0`). Do not create or push the tag unless the user explicitly says so — pushing it triggers the NuGet publish job.
- Shell on the dev box is PowerShell 7 (`pwsh`); scripts below are PowerShell unless marked `bash`. Paths are relative to `C:\Develop\Mobizon.Net`.
- `samples/Mobizon.Net.Playground` is git-ignored and not in the solution — ignore it. `tools/Mobizon.Net.ApiCapture` keeps its own URL builder — do not touch it.
- TDD per task: write/adjust the test first, watch it fail, implement, watch it pass, run the whole suite, commit.

---

# Phase 0 — hygiene, metadata, CI, target frameworks (no public C# change)

## Task 1: Repo hygiene — delete empty test project, untrack local settings, remove dead code

**Files:**
- Delete: `tests/Mobizon.Net.IntegrationTests/` (whole directory)
- Modify: `Mobizon.Net.sln` (remove the `Mobizon.Net.IntegrationTests` project and its configuration lines)
- Modify: `.gitignore`
- Modify: `src/Mobizon.Net/Internal/MobizonApiClient.cs:83-99` (delete `SendJsonAsync`)
- Untrack: `.claude/settings.local.json`

**Interfaces:** none.

- [ ] **Step 1: Confirm the integration-test project is empty and `SendJsonAsync` has no callers.**

```pwsh
Get-ChildItem tests/Mobizon.Net.IntegrationTests -File -Recurse | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | Select-Object FullName
git grep -n "SendJsonAsync" -- src tests samples tools
```
Expected: only `Mobizon.Net.IntegrationTests.csproj` listed; `SendJsonAsync` appears only in `src/Mobizon.Net/Internal/MobizonApiClient.cs`.

- [ ] **Step 2: Remove the project from the solution and delete it.**

```pwsh
dotnet sln Mobizon.Net.sln remove tests/Mobizon.Net.IntegrationTests/Mobizon.Net.IntegrationTests.csproj
git rm -r tests/Mobizon.Net.IntegrationTests
Remove-Item -Recurse -Force tests/Mobizon.Net.IntegrationTests -ErrorAction SilentlyContinue
```

- [ ] **Step 3: Untrack `.claude/settings.local.json` and ignore it.**

```pwsh
git rm --cached .claude/settings.local.json
Add-Content .gitignore "`n## Claude Code local permissions (machine-specific)`n.claude/settings.local.json`n"
```

- [ ] **Step 4: Delete `SendJsonAsync` from `MobizonApiClient`.**

In `src/Mobizon.Net/Internal/MobizonApiClient.cs` delete the whole method starting at `public async Task<MobizonResponse<T>> SendJsonAsync<T>(` through its closing brace (currently lines 83–99). Also delete the now-unused field:

```csharp
        private static readonly JsonSerializerOptions JsonWriteOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
```
and the `using System.Text.Encodings.Web;` line.

- [ ] **Step 5: Build + test.**

Run: `dotnet build -c Release; dotnet test --no-build -c Release`
Expected: 0 warnings; 246 tests pass; the "No test is available in …IntegrationTests.dll" line is gone.

- [ ] **Step 6: Commit.**

```pwsh
git add -A
git commit -m "chore: drop empty IntegrationTests project, untrack local Claude settings, remove dead SendJsonAsync"
```

---

## Task 2: Package metadata, SourceLink, symbols, LICENSE (spec D1)

**Files:**
- Modify: `Directory.Build.props`
- Modify: `src/Mobizon.Net/Mobizon.Net.csproj` (Description)
- Modify: `LICENSE:3`

**Interfaces:** none.

- [ ] **Step 1: Replace `Directory.Build.props` with:**

```xml
<Project>
  <PropertyGroup>
    <LangVersion>8.0</LangVersion>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);CS1591</NoWarn>
    <MinVerTagPrefix>v</MinVerTagPrefix>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MinVer" Version="6.0.0" PrivateAssets="all" />
  </ItemGroup>

  <!-- NuGet Package Metadata -->
  <PropertyGroup>
    <Authors>Gany Berikov</Authors>
    <Copyright>Copyright (c) 2026 Gany Berikov</Copyright>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageProjectUrl>https://github.com/gberikov/Mobizon.Net</PackageProjectUrl>
    <RepositoryUrl>https://github.com/gberikov/Mobizon.Net</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageTags>sms mobizon sdk dotnet messaging api</PackageTags>
    <PackageReadmeFile>README.md</PackageReadmeFile>
  </PropertyGroup>

  <!-- Symbols + SourceLink (build-time only) -->
  <PropertyGroup>
    <PublishRepositoryUrl>true</PublishRepositoryUrl>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
    <Deterministic>true</Deterministic>
  </PropertyGroup>
  <PropertyGroup Condition="'$(GITHUB_ACTIONS)' == 'true'">
    <ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="all" />
  </ItemGroup>

  <ItemGroup>
    <None Include="$(MSBuildThisFileDirectory)README.md" Pack="true" PackagePath="\" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Fix the `Mobizon.Net` description.** In `src/Mobizon.Net/Mobizon.Net.csproj` replace the `<Description>` line with:

```xml
    <Description>Unofficial .NET SDK for the Mobizon SMS gateway REST API v1: messages, campaigns, short links, balance, background tasks, contact groups and cards, number stop-list, sender IDs (alphanames).</Description>
```

- [ ] **Step 3: Align LICENSE.** Change line 3 of `LICENSE` to `Copyright (c) 2026 Gany Berikov`.

- [ ] **Step 4: Verify the produced package.**

```pwsh
dotnet build -c Release
dotnet pack --no-build -c Release -o artifacts/pack
Get-ChildItem artifacts/pack
Expand-Archive artifacts/pack/Mobizon.Net.*.nupkg -DestinationPath artifacts/pack/inspect -Force
Get-Content artifacts/pack/inspect/Mobizon.Net.nuspec
```
Expected: 6 `.nupkg` **and** 6 `.snupkg`; nuspec contains `<projectUrl>https://github.com/gberikov/Mobizon.Net</projectUrl>`, `<repository type="git" url="https://github.com/gberikov/Mobizon.Net" commit="…" />`, `<authors>Gany Berikov</authors>`, and a description starting with `Unofficial`. Version is `0.0.0-alpha.0.N` (no tag yet — expected). Clean up: `Remove-Item -Recurse -Force artifacts/pack`.

- [ ] **Step 5: Commit.**

```pwsh
git add -A
git commit -m "build: correct package metadata (repo URL, author, unofficial), add SourceLink + snupkg"
```

---

## Task 3: Multi-target core packages and modernise CI (spec D2, D3)

**Files:**
- Modify: `src/Mobizon.Contracts/Mobizon.Contracts.csproj`
- Modify: `src/Mobizon.Net/Mobizon.Net.csproj`
- Modify: `src/Mobizon.Net.Webhooks/Mobizon.Net.Webhooks.csproj`
- Modify: `src/Mobizon.Net.Extensions.DependencyInjection/Mobizon.Net.Extensions.DependencyInjection.csproj`
- Modify: `src/Mobizon.Net.Extensions.Polly/Mobizon.Net.Extensions.Polly.csproj`
- Modify: `src/Mobizon.Net.Webhooks.AspNetCore/Mobizon.Net.Webhooks.AspNetCore.csproj`
- Modify: `.github/workflows/ci.yml`

**Interfaces:** none (build-only).

- [ ] **Step 1: Contracts and Webhooks — multi-target and condition the STJ reference.** In both `Mobizon.Contracts.csproj` and `Mobizon.Net.Webhooks.csproj`:

Replace `<TargetFramework>netstandard2.0</TargetFramework>` with `<TargetFrameworks>netstandard2.0;net8.0</TargetFrameworks>` and replace the `System.Text.Json` item group with:

```xml
  <ItemGroup Condition="'$(TargetFramework)' == 'netstandard2.0'">
    <PackageReference Include="System.Text.Json" Version="8.0.5" />
  </ItemGroup>
```

- [ ] **Step 2: Mobizon.Net, DI, Polly — multi-target.** In each of the three csproj files replace `<TargetFramework>netstandard2.0</TargetFramework>` with `<TargetFrameworks>netstandard2.0;net8.0</TargetFrameworks>`.

- [ ] **Step 3: AspNetCore — multi-target.** In `Mobizon.Net.Webhooks.AspNetCore.csproj` replace `<TargetFramework>net8.0</TargetFramework>` with `<TargetFrameworks>net8.0;net10.0</TargetFrameworks>`.

- [ ] **Step 4: Build all TFMs.**

Run: `dotnet build -c Release`
Expected: 0 warnings, 0 errors. If a net8.0/net10.0 build reports an obsolete-API warning (CS0618/SYSLIB…), fix the call site rather than suppressing — none are expected at this point.

- [ ] **Step 5: Replace `.github/workflows/ci.yml` with:**

```yaml
name: CI

on:
  push:
    branches: [master, develop]
    tags: ['v*']
  pull_request:
    branches: [master, develop]

permissions:
  contents: read

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4
        with:
          fetch-depth: 0   # MinVer needs tag history

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: |
            8.0.x
            10.0.x

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build -c Release --no-restore

      - name: Test
        run: dotnet test --no-build -c Release

      - name: Pack
        run: dotnet pack --no-build -c Release -o ./artifacts

      - name: Upload artifacts
        uses: actions/upload-artifact@v4
        with:
          name: nupkg
          path: |
            ./artifacts/*.nupkg
            ./artifacts/*.snupkg

  publish:
    runs-on: ubuntu-latest
    needs: build
    if: startsWith(github.ref, 'refs/tags/v')
    steps:
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x

      - name: Download artifacts
        uses: actions/download-artifact@v4
        with:
          name: nupkg
          path: ./artifacts

      - name: Push to NuGet.org
        run: dotnet nuget push "*.nupkg" --source https://api.nuget.org/v3/index.json --api-key ${{ secrets.NUGET_API_KEY }} --skip-duplicate
        working-directory: ./artifacts
```

- [ ] **Step 6: Test + commit.**

Run: `dotnet test -c Release`
Expected: all green.

```pwsh
git add -A
git commit -m "build: multi-target netstandard2.0;net8.0 (core) and net8.0;net10.0 (AspNetCore); CI fetch-depth 0, .NET 8+10, skip-duplicate"
```

---

# Phase 1 — transport, exceptions, formatting, resilience (internal behaviour)

## Task 4: `ApiFormat` — culture-proof wire formatting (spec D7)

**Files:**
- Create: `src/Mobizon.Net/Internal/ApiFormat.cs`
- Modify: `src/Mobizon.Net/Services/MessageService.cs:57,129-150`
- Modify: `src/Mobizon.Net/Services/CampaignService.cs:49,139-145`
- Modify: `src/Mobizon.Net/Services/LinkService.cs:133-134`
- Modify: `src/Mobizon.Net/Internal/Converters/MobizonDateTimeConverter.cs` (accept `yyyy-MM-dd` on read)
- Test: `tests/Mobizon.Net.Tests/Services/MessageServiceTests.cs` (append), `tests/Mobizon.Net.Tests/Internal/MobizonDateTimeConverterTests.cs` (new)

**Interfaces:**
- Produces: `internal static class Mobizon.Net.Internal.ApiFormat` with `DateTimeFormat`, `DateFormat`, `DateTime(DateTime)`, `Date(DateTime)`, `Int(long)`, `Bool(bool)`. (`Sort` and `Gender` are added in Tasks 16/15 when their enums change.)

- [ ] **Step 1: Write the failing culture test.** Append to `tests/Mobizon.Net.Tests/Services/MessageServiceTests.cs` inside the class (add `using System.Globalization;` at the top if missing):

```csharp
        [Fact]
        public async Task ListAsync_DateCriteria_AreGregorianRegardlessOfCurrentCulture()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/Message/List")
                .WithFormData("criteria[startSendTsFrom]", "2026-03-01 10:20:30")
                .Respond("application/json", @"{""code"":0,""data"":{""items"":[],""totalItemCount"":""0""},""message"":""""}");

            var thai = new CultureInfo("th-TH");
            thai.DateTimeFormat.Calendar = new ThaiBuddhistCalendar(); // year 2026 renders as 2569 under this calendar
            var original = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = thai;
            try
            {
                await CreateService(mockHttp).ListAsync(new MessageListRequest
                {
                    Criteria = new MessageListCriteria { SentFrom = new DateTime(2026, 3, 1, 10, 20, 30) }
                });
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }

            mockHttp.VerifyNoOutstandingExpectation();
        }
```
(`CreateService` already exists in that test class; if the helper has a different name there, use the existing one.)

- [ ] **Step 2: Run it — expect FAIL.**

Run: `dotnet test --filter "FullyQualifiedName~MessageServiceTests.ListAsync_DateCriteria_AreGregorian"`
Expected: FAIL — MockHttp reports no matching expectation (the value sent is `2569-03-01 10:20:30`).

- [ ] **Step 3: Create `src/Mobizon.Net/Internal/ApiFormat.cs`.**

```csharp
using System;
using System.Globalization;

namespace Mobizon.Net.Internal
{
    /// <summary>
    /// Single place that renders .NET values into the string form the Mobizon API expects.
    /// Everything is culture-invariant: the API speaks Gregorian dates and ASCII digits only.
    /// </summary>
    internal static class ApiFormat
    {
        public const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        public const string DateFormat = "yyyy-MM-dd";

        public static string DateTime(DateTime value) =>
            value.ToString(DateTimeFormat, CultureInfo.InvariantCulture);

        public static string Date(DateTime value) =>
            value.ToString(DateFormat, CultureInfo.InvariantCulture);

        public static string Int(long value) =>
            value.ToString(CultureInfo.InvariantCulture);

        public static string Bool(bool value) => value ? "1" : "0";
    }
}
```

- [ ] **Step 4: Route every date through `ApiFormat`.** Run this once from the repo root; it rewrites the three services:

```pwsh
$files = 'src/Mobizon.Net/Services/MessageService.cs','src/Mobizon.Net/Services/CampaignService.cs','src/Mobizon.Net/Services/LinkService.cs'
foreach ($f in $files) {
  $c = Get-Content $f -Raw
  $n = $c -replace '(\w[\w\.]*)\.Value\.ToString\("yyyy-MM-dd HH:mm:ss"(, CultureInfo\.InvariantCulture)?\)', 'ApiFormat.DateTime($1.Value)'
  Set-Content $f $n -NoNewline
}
git diff --stat
```
Expected: 10 replacements in `MessageService.cs`, 5 in `CampaignService.cs`, 2 in `LinkService.cs`. Verify with `git grep -n 'ToString("yyyy' -- src` → **no output**.

- [ ] **Step 5: Teach the read-side converter the date-only form.** Replace the body of `MobizonDateTimeConverter.Read` in `src/Mobizon.Net/Internal/Converters/MobizonDateTimeConverter.cs`:

```csharp
        private static readonly string[] Formats = { "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd" };

        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            var s = reader.GetString();
            if (string.IsNullOrWhiteSpace(s))
                return null;

            if (DateTime.TryParseExact(s, Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt;

            throw new JsonException($"Cannot parse \"{s}\" as DateTime (expected \"yyyy-MM-dd HH:mm:ss\" or \"yyyy-MM-dd\").");
        }
```
Keep the existing `private const string Format` for `Write`, or replace its use with `Formats[0]`.

- [ ] **Step 6: Converter test.** Create `tests/Mobizon.Net.Tests/Internal/MobizonDateTimeConverterTests.cs`:

```csharp
using System;
using System.Text.Json;
using Mobizon.Net.Internal.Converters;
using Xunit;

namespace Mobizon.Net.Tests.Internal
{
    public class MobizonDateTimeConverterTests
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            Converters = { new MobizonDateTimeConverter() }
        };

        private class Holder { public DateTime? Value { get; set; } }

        [Theory]
        [InlineData("\"2026-03-10 12:00:00\"", 2026, 3, 10, 12, 0, 0)]
        [InlineData("\"2025-12-31\"", 2025, 12, 31, 0, 0, 0)]
        public void Read_AcceptsDateTimeAndDateOnly(string json, int y, int m, int d, int hh, int mm, int ss)
        {
            var h = JsonSerializer.Deserialize<Holder>("{\"Value\":" + json + "}", Options)!;
            Assert.Equal(new DateTime(y, m, d, hh, mm, ss), h.Value);
        }

        [Theory]
        [InlineData("null")]
        [InlineData("\"\"")]
        public void Read_NullOrEmpty_YieldsNull(string json)
        {
            var h = JsonSerializer.Deserialize<Holder>("{\"Value\":" + json + "}", Options)!;
            Assert.Null(h.Value);
        }

        [Fact]
        public void Read_Garbage_Throws()
        {
            Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize<Holder>("{\"Value\":\"tomorrow\"}", Options));
        }
    }
}
```

- [ ] **Step 7: Run the suite — expect PASS.**

Run: `dotnet build -c Release; dotnet test --no-build -c Release`
Expected: 0 warnings; all green including the two new test classes.

- [ ] **Step 8: Commit.**

```pwsh
git add -A
git commit -m "fix: culture-invariant date formatting via ApiFormat; date-only reads in MobizonDateTimeConverter"
```

---

## Task 5: API key in the POST body, POST-only transport, User-Agent, idempotency marker (spec D4)

**Files:**
- Create: `src/Mobizon.Net/Internal/RequestMarkers.cs`
- Modify: `src/Mobizon.Net/Internal/MobizonApiClient.cs` (signature of `SendAsync`, `SendMultipartAsync`, `BuildUrl`)
- Modify: `src/Mobizon.Net/Mobizon.Net.csproj` (InternalsVisibleTo for Polly)
- Modify: `src/Mobizon.Net/Services/*.cs` (drop the `HttpMethod` argument — scripted)
- Modify: `tests/Mobizon.Net.Tests/Internal/MobizonApiClientTests.cs`, `tests/Mobizon.Net.Tests/Services/UserServiceTests.cs`, `tests/Mobizon.Net.Tests/Extensions/IntegrationTests.cs`
- Test: `tests/Mobizon.Net.Tests/Internal/RequestMarkersTests.cs` (new)

**Interfaces:**
- Produces: `MobizonApiClient.SendAsync<T>(string module, string apiMethod, IDictionary<string,string>? parameters, CancellationToken cancellationToken = default, int[]? extraSuccessCodes = null)`; `SendMultipartAsync<T>(…)` signature unchanged; `internal static bool MobizonApiClient.IsReadMethod(string apiMethod)`; `internal static class RequestMarkers { static void MarkIdempotent(HttpRequestMessage); static bool IsIdempotent(HttpRequestMessage); }`.
- Consumed by: Task 8 (Polly reads `RequestMarkers.IsIdempotent`).

- [ ] **Step 1: Rewrite the transport tests first.** In `tests/Mobizon.Net.Tests/Internal/MobizonApiClientTests.cs` add `using System.Linq;` and replace `SendAsync_Post_BuildsCorrectUrl` with:

```csharp
        [Fact]
        public async Task SendAsync_BuildsUrl_AndSendsApiKeyInBodyNotQuery()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/message/sendsmsmessage")
                .WithQueryString("output", "json")
                .WithQueryString("api", "v1")
                .WithFormData("apiKey", "test-api-key")
                .With(req => !req.RequestUri!.Query.Contains("apiKey"))
                .Respond("application/json",
                    @"{""code"":0,""data"":{},""message"":""""}");

            var client = CreateClient(mockHttp);
            await client.SendAsync<object>("message", "sendsmsmessage", null);

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task SendAsync_SetsSdkUserAgent()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/message/sendsmsmessage")
                .With(req => req.Headers.UserAgent.Any(p => p.Product?.Name == "Mobizon.Net" && !string.IsNullOrEmpty(p.Product.Version)))
                .Respond("application/json", @"{""code"":0,""data"":{},""message"":""""}");

            await CreateClient(mockHttp).SendAsync<object>("message", "sendsmsmessage", null);

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Theory]
        [InlineData("getSMSStatus", true)]
        [InlineData("get", true)]
        [InlineData("getInfo", true)]
        [InlineData("getlinks", true)]
        [InlineData("getstats", true)]
        [InlineData("list", true)]
        [InlineData("getownbalance", true)]
        [InlineData("getstatus", true)]
        [InlineData("getgroups", true)]
        [InlineData("getcardscount", true)]
        [InlineData("sendsmsmessage", false)]
        [InlineData("create", false)]
        [InlineData("delete", false)]
        [InlineData("send", false)]
        [InlineData("addrecipients", false)]
        [InlineData("update", false)]
        [InlineData("setgroups", false)]
        public void IsReadMethod_ClassifiesEveryApiMethod(string method, bool expected)
        {
            Assert.Equal(expected, MobizonApiClient.IsReadMethod(method));
        }

        [Fact]
        public async Task SendAsync_ReadMethod_IsMarkedIdempotent_WriteIsNot()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/user/getownbalance")
                .With(req => RequestMarkers.IsIdempotent(req))
                .Respond("application/json", @"{""code"":0,""data"":{},""message"":""""}");
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/message/sendsmsmessage")
                .With(req => !RequestMarkers.IsIdempotent(req))
                .Respond("application/json", @"{""code"":0,""data"":{},""message"":""""}");

            var client = CreateClient(mockHttp);
            await client.SendAsync<object>("user", "getownbalance", null);
            await client.SendAsync<object>("message", "sendsmsmessage", null);

            mockHttp.VerifyNoOutstandingExpectation();
        }
```

Replace `SendAsync_Get_DoesNotSendBody` with:

```csharp
        [Fact]
        public async Task SendAsync_GetOwnBalance_IsPostWithApiKeyInBody()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/user/getownbalance")
                .WithFormData("apiKey", "test-api-key")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""balance"":""100.50"",""currency"":""KZT""},""message"":""""}");

            var client = CreateClient(mockHttp);
            var result = await client.SendAsync<TestBalanceResult>("user", "getownbalance", null);

            Assert.Equal("100.50", result.Data.Balance);
            Assert.Equal("KZT", result.Data.Currency);
            mockHttp.VerifyNoOutstandingExpectation();
        }
```

The remaining `client.SendAsync<…>(HttpMethod.Post, …)` calls in this file are rewritten by the script in Step 7.

In `tests/Mobizon.Net.Tests/Services/UserServiceTests.cs`: rename `GetOwnBalanceAsync_UsesGetMethod` → `GetOwnBalanceAsync_PostsWithApiKeyInBody`, change `HttpMethod.Get` → `HttpMethod.Post`, and replace the three `.WithQueryString(...)` lines with a single `.WithFormData("apiKey", "test-key")`.

In `tests/Mobizon.Net.Tests/Extensions/IntegrationTests.cs` change both `.Expect(HttpMethod.Get, "https://api.mobizon.kz/service/user/getownbalance")` to `HttpMethod.Post`.

- [ ] **Step 2: Add `tests/Mobizon.Net.Tests/Internal/RequestMarkersTests.cs`.**

```csharp
using System.Net.Http;
using Mobizon.Net.Internal;
using Xunit;

namespace Mobizon.Net.Tests.Internal
{
    public class RequestMarkersTests
    {
        [Fact]
        public void Unmarked_IsNotIdempotent()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://example.test/");
            Assert.False(RequestMarkers.IsIdempotent(request));
        }

        [Fact]
        public void Marked_IsIdempotent()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://example.test/");
            RequestMarkers.MarkIdempotent(request);
            Assert.True(RequestMarkers.IsIdempotent(request));
        }
    }
}
```

- [ ] **Step 3: Run — expect compile failure** (new signature / `RequestMarkers` missing).

Run: `dotnet build tests/Mobizon.Net.Tests`
Expected: CS1503 / CS0103 errors in the edited tests.

- [ ] **Step 4: Create `src/Mobizon.Net/Internal/RequestMarkers.cs`.**

```csharp
using System.Net.Http;

namespace Mobizon.Net.Internal
{
    /// <summary>
    /// Per-request flags carried on the <see cref="HttpRequestMessage"/> so outer layers
    /// (e.g. the Polly package) can decide whether a request is safe to retry.
    /// </summary>
    internal static class RequestMarkers
    {
        private const string IdempotentKey = "Mobizon.Idempotent";

#if NET5_0_OR_GREATER
        private static readonly HttpRequestOptionsKey<bool> IdempotentOption = new HttpRequestOptionsKey<bool>(IdempotentKey);

        public static void MarkIdempotent(HttpRequestMessage request) => request.Options.Set(IdempotentOption, true);

        public static bool IsIdempotent(HttpRequestMessage request) =>
            request.Options.TryGetValue(IdempotentOption, out var value) && value;
#else
        public static void MarkIdempotent(HttpRequestMessage request) => request.Properties[IdempotentKey] = true;

        public static bool IsIdempotent(HttpRequestMessage request) =>
            request.Properties.TryGetValue(IdempotentKey, out var value) && value is bool b && b;
#endif
    }
}
```

- [ ] **Step 5: Rewrite the request-building half of `MobizonApiClient`.** In `src/Mobizon.Net/Internal/MobizonApiClient.cs` add `using System.Reflection;` and replace everything from `private readonly HttpClient _httpClient;` through the end of `BuildUrl` with:

```csharp
        private static readonly ProductInfoHeaderValue UserAgent =
            new ProductInfoHeaderValue("Mobizon.Net", GetSdkVersion());

        private readonly HttpClient _httpClient;
        private readonly MobizonClientOptions _options;

        public MobizonApiClient(HttpClient httpClient, MobizonClientOptions options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            options.Validate();
        }

        /// <summary>
        /// Sends a form-encoded POST. The API key is always a body field (never part of the URL), so it
        /// does not leak into proxy / HttpClient request logs.
        /// </summary>
        public async Task<MobizonResponse<T>> SendAsync<T>(
            string module,
            string apiMethod,
            IDictionary<string, string>? parameters,
            CancellationToken cancellationToken = default,
            int[]? extraSuccessCodes = null)
        {
            var form = new Dictionary<string, string> { ["apiKey"] = _options.ApiKey };
            if (parameters != null)
                foreach (var kv in parameters)
                    form[kv.Key] = kv.Value;

            var request = CreateRequest(module, apiMethod);
            request.Content = new FormUrlEncodedContent(form);

            return await SendCoreAsync<T>(request, cancellationToken, extraSuccessCodes).ConfigureAwait(false);
        }

        public async Task<MobizonResponse<T>> SendMultipartAsync<T>(
            string module,
            string apiMethod,
            IDictionary<string, string> fields,
            Stream? photo = null,
            string? photoFileName = null,
            CancellationToken cancellationToken = default,
            int[]? extraSuccessCodes = null,
            string fileFieldName = "data[photo]")
        {
            var request = CreateRequest(module, apiMethod);

            var multipart = new MultipartFormDataContent();
            multipart.Add(new StringContent(_options.ApiKey), "apiKey");

            foreach (var kv in fields)
                multipart.Add(new StringContent(kv.Value ?? string.Empty), kv.Key);

            if (photo != null)
            {
                var fileContent = new StreamContent(photo);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                multipart.Add(fileContent, fileFieldName, photoFileName ?? "photo");
            }

            request.Content = multipart;

            return await SendCoreAsync<T>(request, cancellationToken, extraSuccessCodes).ConfigureAwait(false);
        }

        /// <summary>Read-only API methods are safe to retry. Every Mobizon read is <c>get*</c> or <c>list</c>.</summary>
        internal static bool IsReadMethod(string apiMethod) =>
            apiMethod.StartsWith("get", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(apiMethod, "list", StringComparison.OrdinalIgnoreCase);

        private HttpRequestMessage CreateRequest(string module, string apiMethod)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, BuildUrl(module, apiMethod));
            request.Headers.UserAgent.Add(UserAgent);
            if (IsReadMethod(apiMethod))
                RequestMarkers.MarkIdempotent(request);
            return request;
        }

        private string BuildUrl(string module, string apiMethod) =>
            $"{_options.ApiUrl.TrimEnd('/')}/service/{module}/{apiMethod}?output=json&api={_options.ApiVersion}";

        private static string GetSdkVersion()
        {
            var v = typeof(MobizonApiClient).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
            var plus = v.IndexOf('+');
            return plus > 0 ? v.Substring(0, plus) : v;
        }
```
Leave `SendCoreAsync` untouched (Task 6 rewrites it).

- [ ] **Step 6: Expose internals to the Polly package.** In `src/Mobizon.Net/Mobizon.Net.csproj` add a second attribute next to the existing one:

```xml
    <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
      <_Parameter1>Mobizon.Net.Extensions.Polly</_Parameter1>
    </AssemblyAttribute>
```

- [ ] **Step 7: Drop the `HttpMethod` argument at every call site (services + api-client tests).**

```pwsh
$files = @(Get-ChildItem src/Mobizon.Net/Services -Filter *.cs | ForEach-Object FullName) + @('tests/Mobizon.Net.Tests/Internal/MobizonApiClientTests.cs')
foreach ($f in $files) {
  $c = Get-Content $f -Raw
  $n = $c -replace '(Send(?:Multipart)?Async<.+?>\(\s*)HttpMethod\.(?:Post|Get),\s*', '$1'
  if ($n -ne $c) { Set-Content $f $n -NoNewline; Write-Host "rewrote $f" }
}
git grep -n "HttpMethod\." -- src/Mobizon.Net/Services
```
Expected: every service file rewritten; the final grep prints **nothing**. (A leftover unused `using System.Net.Http;` is not a compiler warning — leave it or delete it.)

- [ ] **Step 8: Build + full suite — expect PASS.**

Run: `dotnet build -c Release; dotnet test --no-build -c Release`
Expected: 0 warnings; all green.

- [ ] **Step 9: Commit.**

```pwsh
git add -A
git commit -m "fix(security): send apiKey in POST body instead of URL; POST-only transport; User-Agent; idempotency marker on read methods"
```

---

## Task 6: Actionable exceptions — HTTP status, non-JSON bodies, timeout vs. cancellation (spec D5)

**Files:**
- Modify: `src/Mobizon.Contracts/Exceptions/MobizonException.cs`
- Modify: `src/Mobizon.Contracts/Exceptions/MobizonApiException.cs`
- Modify: `src/Mobizon.Net/Internal/MobizonApiClient.cs` (`SendCoreAsync`)
- Test: `tests/Mobizon.Net.Tests/Internal/MobizonApiClientTests.cs`, `tests/Mobizon.Net.Tests/Exceptions/MobizonExceptionTests.cs`

**Interfaces:**
- Produces: `MobizonException.StatusCode : HttpStatusCode?`; `MobizonException(string message, HttpStatusCode? statusCode, Exception? innerException = null)`; `MobizonApiException(int rawCode, string apiMessage, HttpStatusCode? statusCode = null)`.

- [ ] **Step 1: Write the failing tests.** Append to `MobizonApiClientTests` (`using System.Net;` is already present):

```csharp
        [Fact]
        public async Task SendAsync_Non2xxWithHtmlBody_ThrowsMobizonExceptionWithStatusAndSnippet()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, "https://api.mobizon.kz/service/*")
                .Respond(HttpStatusCode.BadGateway, "text/html", "<html><body>502 Bad Gateway</body></html>");

            var ex = await Assert.ThrowsAsync<MobizonException>(() =>
                CreateClient(mockHttp).SendAsync<object>("message", "sendsmsmessage", null));

            Assert.IsNotType<MobizonApiException>(ex);
            Assert.Equal(HttpStatusCode.BadGateway, ex.StatusCode);
            Assert.Contains("HTTP 502", ex.Message);
            Assert.Contains("502 Bad Gateway", ex.Message);
        }

        [Fact]
        public async Task SendAsync_2xxWithGarbageBody_ThrowsMobizonExceptionWithStatus()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, "https://api.mobizon.kz/service/*")
                .Respond("text/plain", "not json at all");

            var ex = await Assert.ThrowsAsync<MobizonException>(() =>
                CreateClient(mockHttp).SendAsync<object>("message", "sendsmsmessage", null));

            Assert.Equal(HttpStatusCode.OK, ex.StatusCode);
            Assert.Contains("Failed to deserialize", ex.Message);
            Assert.Contains("not json at all", ex.Message);
        }

        [Fact]
        public async Task SendAsync_ApiError_CarriesHttpStatus()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, "https://api.mobizon.kz/service/*")
                .Respond(HttpStatusCode.OK, "application/json", @"{""code"":8,""data"":null,""message"":""Login error""}");

            var ex = await Assert.ThrowsAsync<MobizonApiException>(() =>
                CreateClient(mockHttp).SendAsync<object>("message", "sendsmsmessage", null));

            Assert.Equal(HttpStatusCode.OK, ex.StatusCode);
            Assert.Equal(MobizonResponseCode.LoginError, ex.Code);
        }

        [Fact]
        public async Task SendAsync_HttpClientTimeout_ThrowsMobizonException()
        {
            var httpClient = new HttpClient(new HangingHandler()) { Timeout = TimeSpan.FromMilliseconds(200) };
            var client = new MobizonApiClient(httpClient, _options);

            var ex = await Assert.ThrowsAsync<MobizonException>(() =>
                client.SendAsync<object>("message", "sendsmsmessage", null));

            Assert.Contains("timed out", ex.Message);
            Assert.Null(ex.StatusCode);
            Assert.IsAssignableFrom<OperationCanceledException>(ex.InnerException);
        }

        [Fact]
        public async Task SendAsync_CallerCancellation_IsNotWrapped()
        {
            var httpClient = new HttpClient(new HangingHandler());
            var client = new MobizonApiClient(httpClient, _options);
            using var cts = new CancellationTokenSource(50);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                client.SendAsync<object>("message", "sendsmsmessage", null, cts.Token));
        }

        /// <summary>Never answers; completes only when the request's token is cancelled.</summary>
        private sealed class HangingHandler : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        }
```

Append to `tests/Mobizon.Net.Tests/Exceptions/MobizonExceptionTests.cs` (add `using System.Net;`):

```csharp
        [Fact]
        public void MobizonException_StatusCode_IsExposed()
        {
            var ex = new MobizonException("boom", HttpStatusCode.BadGateway);
            Assert.Equal(HttpStatusCode.BadGateway, ex.StatusCode);
            Assert.Null(ex.InnerException);

            var plain = new MobizonException("boom");
            Assert.Null(plain.StatusCode);
        }

        [Fact]
        public void MobizonApiException_StatusCode_FlowsToBase()
        {
            var ex = new MobizonApiException(2, "not found", HttpStatusCode.OK);
            Assert.Equal(HttpStatusCode.OK, ex.StatusCode);
            Assert.Equal(2, ex.RawCode);
        }
```

- [ ] **Step 2: Run — expect compile errors** (`StatusCode` / constructor missing).

Run: `dotnet build tests/Mobizon.Net.Tests`

- [ ] **Step 3: Extend the exceptions.** Replace `src/Mobizon.Contracts/Exceptions/MobizonException.cs` with:

```csharp
using System;
using System.Net;

namespace Mobizon.Contracts.Exceptions
{
    /// <summary>
    /// Base exception for all errors raised by the Mobizon.Net SDK: transport failures, timeouts,
    /// unexpected (non-JSON) responses and — via <see cref="MobizonApiException"/> — API-level errors.
    /// </summary>
    public class MobizonException : Exception
    {
        /// <summary>
        /// HTTP status code of the response that caused this exception, or <see langword="null"/>
        /// when no response was received (network error, timeout).
        /// </summary>
        public HttpStatusCode? StatusCode { get; }

        /// <summary>Initializes a new instance with a message.</summary>
        public MobizonException(string message)
            : base(message)
        {
        }

        /// <summary>Initializes a new instance with a message and the causing exception.</summary>
        public MobizonException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>Initializes a new instance with a message, the HTTP status of the offending response and an optional cause.</summary>
        public MobizonException(string message, HttpStatusCode? statusCode, Exception? innerException = null)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }
    }
}
```

In `src/Mobizon.Contracts/Exceptions/MobizonApiException.cs` add `using System.Net;` and replace the constructor with:

```csharp
        /// <summary>
        /// Initializes a new instance of <see cref="MobizonApiException"/> with the API error code and message.
        /// </summary>
        /// <param name="rawCode">The raw integer response code returned by the API.</param>
        /// <param name="apiMessage">The human-readable error message returned by the API.</param>
        /// <param name="statusCode">HTTP status of the response (usually 200 — Mobizon reports errors inside the JSON envelope).</param>
        public MobizonApiException(int rawCode, string apiMessage, HttpStatusCode? statusCode = null)
            : base($"Mobizon API error {rawCode}: {apiMessage}", statusCode)
        {
            RawCode = rawCode;
            Code = (MobizonResponseCode)rawCode;
            ApiMessage = apiMessage;
        }
```

- [ ] **Step 4: Rewrite `SendCoreAsync`** in `MobizonApiClient.cs` (replace the whole method) and add the helper:

```csharp
        private async Task<MobizonResponse<T>> SendCoreAsync<T>(
            HttpRequestMessage request,
            CancellationToken cancellationToken,
            int[]? extraSuccessCodes = null)
        {
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw; // the caller asked for it
            }
            catch (OperationCanceledException ex)
            {
                // HttpClient.Timeout surfaces as TaskCanceledException while the caller's token is untouched.
                throw new MobizonException(
                    $"Request to Mobizon API timed out after {_httpClient.Timeout}.", statusCode: null, ex);
            }
            catch (Exception ex)
            {
                throw new MobizonException($"Failed to send request to Mobizon API: {ex.Message}", statusCode: null, ex);
            }

            using (response)
            {
                string json;
                try
                {
                    json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    throw new MobizonException("Failed to read Mobizon API response", response.StatusCode, ex);
                }

                MobizonResponse<T>? result;
                try
                {
                    result = JsonSerializer.Deserialize<MobizonResponse<T>>(json, JsonOptions);
                }
                catch (JsonException ex)
                {
                    throw new MobizonException(DescribeUnexpectedBody(response, json), response.StatusCode, ex);
                }

                if (result == null)
                    throw new MobizonException(DescribeUnexpectedBody(response, json), response.StatusCode);

                if (result.Code != MobizonResponseCode.Success &&
                    result.Code != MobizonResponseCode.BackgroundTask &&
                    (extraSuccessCodes == null || Array.IndexOf(extraSuccessCodes, result.RawCode) < 0))
                {
                    throw new MobizonApiException(result.RawCode, result.Message, response.StatusCode);
                }

                return result;
            }
        }

        private static string DescribeUnexpectedBody(HttpResponseMessage response, string body)
        {
            var status = (int)response.StatusCode;
            var snippet = body.Length <= 200 ? body : body.Substring(0, 200) + "…";
            return response.IsSuccessStatusCode
                ? $"Failed to deserialize Mobizon API response (HTTP {status}): {snippet}"
                : $"Mobizon API returned HTTP {status} ({response.StatusCode}) with a non-JSON body: {snippet}";
        }
```

- [ ] **Step 5: Build + full suite — expect PASS.**

Run: `dotnet build -c Release; dotnet test --no-build -c Release`
Expected: 0 warnings; all green, including the five new api-client tests and two exception tests. `SendAsync_NetworkError_ThrowsMobizonException` still passes (the `HttpRequestException` path is unchanged).

- [ ] **Step 6: Commit.**

```pwsh
git add -A
git commit -m "fix: expose HTTP status on MobizonException; wrap HttpClient timeouts; describe non-JSON bodies"
```

---

## Task 7: `ConfigureAwait(false)` in `ContactCards`

**Files:**
- Modify: `src/Mobizon.Net/ContactCards/ContactCardSet.cs:60,75,116`
- Modify: `src/Mobizon.Net/ContactCards/ContactCardQuery.cs:107,116,129,139,150,161,174`

**Interfaces:** none.

- [ ] **Step 1: Apply mechanically.**

```pwsh
foreach ($f in 'src/Mobizon.Net/ContactCards/ContactCardSet.cs','src/Mobizon.Net/ContactCards/ContactCardQuery.cs') {
  $c = Get-Content $f -Raw
  $n = $c -replace '(await [^;]+?\))\s*;', '$1.ConfigureAwait(false);'
  Set-Content $f $n -NoNewline
}
git grep -n "await " -- src/Mobizon.Net/ContactCards | Select-String -NotMatch "ConfigureAwait"
```
Expected: the final command prints nothing. `git diff` shows exactly 10 changed lines, each now ending `.ConfigureAwait(false);`, and no doubled `.ConfigureAwait(false).ConfigureAwait(false)`.

- [ ] **Step 2: Build + test + commit.**

Run: `dotnet build -c Release; dotnet test --no-build -c Release` → green.

```pwsh
git add -A
git commit -m "fix: ConfigureAwait(false) throughout ContactCardSet/ContactCardQuery"
```

---

## Task 8: Polly — retry only idempotent requests by default (spec D6)

**Files:**
- Modify: `src/Mobizon.Net.Extensions.Polly/MobizonResilienceOptions.cs`
- Modify: `src/Mobizon.Net.Extensions.Polly/MobizonHttpClientBuilderExtensions.cs`
- Test: `tests/Mobizon.Net.Tests/Extensions/IntegrationTests.cs` (append), `tests/Mobizon.Net.Tests/Extensions/MobizonResilienceTests.cs` (append)

**Interfaces:**
- Consumes: `RequestMarkers.IsIdempotent(HttpRequestMessage)` (Task 5).
- Produces: `MobizonResilienceOptions.RetryNonIdempotentRequests : bool` (default `false`).

- [ ] **Step 1: Failing tests.** Append to `tests/Mobizon.Net.Tests/Extensions/IntegrationTests.cs` (it already wires `AddMobizon(...).AddMobizonResilience()` + `ConfigurePrimaryHttpMessageHandler(() => mockHttp)`; make sure `using System.Net;` and `using Mobizon.Contracts.Exceptions;` are present):

```csharp
        [Fact]
        public async Task AddMobizonResilience_DoesNotRetry_SendSmsMessage_ByDefault()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/Message/SendSmsMessage")
                .Respond(HttpStatusCode.ServiceUnavailable, "text/plain", "down");

            var services = new ServiceCollection();
            services.AddMobizon(o => { o.ApiKey = "test-key"; o.ApiUrl = "https://api.mobizon.kz"; })
                .AddMobizonResilience()
                .ConfigurePrimaryHttpMessageHandler(() => mockHttp);
            var client = services.BuildServiceProvider().GetRequiredService<IMobizonClient>();

            var ex = await Assert.ThrowsAsync<MobizonException>(() => client.Messages.QuickSendAsync("77001234567", "hi"));

            Assert.Equal(HttpStatusCode.ServiceUnavailable, ex.StatusCode);
            mockHttp.VerifyNoOutstandingExpectation(); // exactly one attempt
        }

        [Fact]
        public async Task AddMobizonResilience_RetriesSendSmsMessage_WhenOptedIn()
        {
            const string ok = @"{""code"":0,""data"":{""campaignId"":""1"",""messageId"":""2"",""status"":1},""message"":""""}";
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/Message/SendSmsMessage")
                .Respond(HttpStatusCode.ServiceUnavailable);
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/Message/SendSmsMessage")
                .Respond("application/json", ok);

            var services = new ServiceCollection();
            services.AddMobizon(o => { o.ApiKey = "test-key"; o.ApiUrl = "https://api.mobizon.kz"; })
                .AddMobizonResilience(r => { r.RetryNonIdempotentRequests = true; r.RetryBaseDelay = TimeSpan.FromMilliseconds(1); })
                .ConfigurePrimaryHttpMessageHandler(() => mockHttp);
            var client = services.BuildServiceProvider().GetRequiredService<IMobizonClient>();

            var result = await client.Messages.QuickSendAsync("77001234567", "hi");

            Assert.Equal(2, result.MessageId);
            mockHttp.VerifyNoOutstandingExpectation();
        }
```

Append to `MobizonResilienceTests`:

```csharp
        [Fact]
        public void MobizonResilienceOptions_RetryNonIdempotentRequests_DefaultsToFalse()
        {
            Assert.False(new MobizonResilienceOptions().RetryNonIdempotentRequests);
        }
```

- [ ] **Step 2: Run — expect FAIL** (compile error: `RetryNonIdempotentRequests` missing).

- [ ] **Step 3: Add the option** to `MobizonResilienceOptions.cs`:

```csharp
        /// <summary>
        /// When <see langword="false"/> (default) the retry policy applies only to read-only API calls
        /// (<c>get*</c> / <c>list</c>). Write calls such as <c>message/sendSmsMessage</c> or <c>campaign/send</c>
        /// are not retried, because a retry after a lost response can duplicate an SMS.
        /// Set to <see langword="true"/> to retry every call.
        /// </summary>
        public bool RetryNonIdempotentRequests { get; set; }
```

- [ ] **Step 4: Select the retry policy per request.** In `MobizonHttpClientBuilderExtensions.cs` add `using Mobizon.Net.Internal;` and replace the two public methods (keep their XML docs, update the summaries to mention "retry for read-only calls, see `RetryNonIdempotentRequests`"; keep the two private `Get*Policy` helpers):

```csharp
        public static IHttpClientBuilder AddMobizonResilience(this IHttpClientBuilder builder)
            => builder.AddMobizonResilience(_ => { });

        public static IHttpClientBuilder AddMobizonResilience(
            this IHttpClientBuilder builder,
            Action<MobizonResilienceOptions> configure)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            var options = new MobizonResilienceOptions();
            configure(options);

            var retry = GetRetryPolicy(options.RetryCount, options.RetryBaseDelay);
            var noRetry = Policy.NoOpAsync<HttpResponseMessage>();
            var retryAll = options.RetryNonIdempotentRequests;

            return builder
                .AddPolicyHandler(request => retryAll || RequestMarkers.IsIdempotent(request) ? retry : noRetry)
                .AddPolicyHandler(GetCircuitBreakerPolicy(
                    options.CircuitBreakerFailureThreshold,
                    options.CircuitBreakerDuration));
        }
```

- [ ] **Step 5: Build + full suite — expect PASS.**

Run: `dotnet build -c Release; dotnet test --no-build -c Release`
Expected: green; the pre-existing `AddMobizonResilience_RetryPolicy_RetriesOn503AndSucceeds` (getownbalance = idempotent) still retries.

- [ ] **Step 6: Commit.**

```pwsh
git add -A
git commit -m "fix(polly): retry only idempotent (read) requests by default; add RetryNonIdempotentRequests"
```

---

# Phase 2 — public surface (breaking, pre-release)

## Task 9: Namespace collapse (spec D8)

**Files:**
- Modify: every `*.cs` under `src/`, `tests/`, `samples/Mobizon.Net.ConsoleSample/`, `tools/` (scripted rewrite of `namespace` / `using` / fully-qualified names)
- Modify by hand: `src/Mobizon.Contracts/Services/IWebhookParser.cs`, `IWebhookProcessor.cs`, `IWebhookSignatureVerifier.cs`, `src/Mobizon.Contracts/Exceptions/WebhookParseException.cs` (→ `Mobizon.Contracts.Webhooks`), `src/Mobizon.Net.Webhooks.AspNetCore/WebhookServiceCollectionExtensions.cs` (missing using)

**Interfaces:**
- Produces: all Contracts types live in `Mobizon.Contracts` (webhook types in `Mobizon.Contracts.Webhooks`); `ContactCardSet`/`ContactCardQuery` live in `Mobizon.Net`. Every later task uses these namespaces.

- [ ] **Step 1: Run the rewrite script from the repo root.**

```pwsh
$files = Get-ChildItem src,tests,samples,tools -Recurse -Include *.cs |
  Where-Object { $_.FullName -notmatch '\\(bin|obj|Mobizon\.Net\.Playground)\\' }

foreach ($f in $files) {
  $c = Get-Content $f.FullName -Raw
  $n = $c `
    -replace 'Mobizon\.Contracts\.Models\.Webhooks', 'Mobizon.Contracts.Webhooks' `
    -replace 'Mobizon\.Contracts\.Models\.(Alphanames|Campaigns|Common|ContactCards|ContactGroups|Links|Messages|StopLists|TaskQueues|Users)', 'Mobizon.Contracts' `
    -replace 'Mobizon\.Contracts\.Services', 'Mobizon.Contracts' `
    -replace 'Mobizon\.Contracts\.Exceptions', 'Mobizon.Contracts' `
    -replace 'Mobizon\.Net\.ContactCards', 'Mobizon.Net' `
    -replace 'cref="Exceptions\.', 'cref="'

  # keep only the first occurrence of each `using X;` line (the collapse creates duplicates → CS0105 → error)
  $seen = @{}
  $lines = foreach ($line in ($n -split "`r?`n")) {
    if ($line -match '^\s*using\s+[\w\.]+;\s*$') {
      $k = $line.Trim()
      if ($seen.ContainsKey($k)) { continue }
      $seen[$k] = $true
    }
    $line
  }
  $n = $lines -join "`n"
  if ($n -ne $c) { Set-Content $f.FullName $n -NoNewline }
}
git grep -c "Mobizon.Contracts.Models" -- '*.cs' ; git grep -c "Mobizon.Net.ContactCards" -- '*.cs'
```
Expected: both greps print nothing (no matches).

- [ ] **Step 2: Move the webhook contracts into `Mobizon.Contracts.Webhooks`.** In these four files change `namespace Mobizon.Contracts` to `namespace Mobizon.Contracts.Webhooks`:
`src/Mobizon.Contracts/Services/IWebhookParser.cs`, `src/Mobizon.Contracts/Services/IWebhookProcessor.cs`, `src/Mobizon.Contracts/Services/IWebhookSignatureVerifier.cs`, `src/Mobizon.Contracts/Exceptions/WebhookParseException.cs`.

Then add `using Mobizon.Contracts.Webhooks;` to `src/Mobizon.Net.Webhooks.AspNetCore/WebhookServiceCollectionExtensions.cs` (it only imported the former `Services` namespace).

- [ ] **Step 3: Build; fix stragglers.**

Run: `dotnet build -c Release`
Expected: 0 errors. If the compiler reports CS0246 for a webhook type, add `using Mobizon.Contracts.Webhooks;` to that file. If it reports CS0101 (duplicate type name in `Mobizon.Contracts`), stop and report — none is expected (`CampaignStatus` and `CampaignCommonStatus` are distinct names).

- [ ] **Step 4: Full suite; commit.**

Run: `dotnet test --no-build -c Release` → green (behaviour unchanged).

```pwsh
git add -A
git commit -m "refactor!: collapse namespaces to Mobizon.Contracts (+ .Webhooks) and Mobizon.Net"
```

---

## Task 10: `BackgroundTaskStatus`, drop phantom `TaskQueueStatus.Id` (spec D9)

**Files:**
- Rename: `src/Mobizon.Contracts/Models/TaskQueues/TaskStatus.cs` → `BackgroundTaskStatus.cs`
- Modify: `src/Mobizon.Contracts/Models/TaskQueues/TaskQueueStatus.cs`
- Modify: `tests/Mobizon.Net.Tests/Services/TaskQueueServiceTests.cs`
- Modify: `samples/Mobizon.Net.ConsoleSample/Samples/TaskQueueSamples.cs` (if it prints `Id`)

**Interfaces:**
- Produces: `public enum BackgroundTaskStatus { Pending = 0, InProgress = 1, Completed = 2, Rejected = 3 }`; `TaskQueueStatus { BackgroundTaskStatus Status; int Progress; }`.

- [ ] **Step 1: Update the test first.** In `TaskQueueServiceTests.GetStatusAsync_SendsIdParameter` change the response payload to `@"{""code"":0,""data"":{""status"":2,""progress"":100},""message"":""""}"`, delete `Assert.Equal(42, result.Id);`, and replace the status assertion with `Assert.Equal(BackgroundTaskStatus.Completed, result.Status);` (add `using Mobizon.Contracts;` if absent).

- [ ] **Step 2: Run — expect compile error** (`BackgroundTaskStatus` unknown).

- [ ] **Step 3: Rename the enum and trim the DTO.**

```pwsh
git mv src/Mobizon.Contracts/Models/TaskQueues/TaskStatus.cs src/Mobizon.Contracts/Models/TaskQueues/BackgroundTaskStatus.cs
```
In `BackgroundTaskStatus.cs` change `public enum TaskStatus` → `public enum BackgroundTaskStatus` and set the summary to `/// <summary>Status of a background task in the Mobizon task queue (<c>taskqueue/getStatus</c>).</summary>`.

Replace the body of `TaskQueueStatus.cs` with:

```csharp
namespace Mobizon.Contracts
{
    /// <summary>Progress of a background task, as returned by <c>taskqueue/getStatus</c>.</summary>
    public class TaskQueueStatus
    {
        /// <summary>Current task status.</summary>
        public BackgroundTaskStatus Status { get; set; }

        /// <summary>Completion percentage, 0–100.</summary>
        public int Progress { get; set; }
    }
}
```

- [ ] **Step 4: Fix remaining references.**

```pwsh
git grep -n "TaskStatus\b" -- src tests samples/Mobizon.Net.ConsoleSample README.md | Select-String -NotMatch "BackgroundTaskStatus|TaskQueueStatus"
git grep -n "taskStatus.Id\|\.Id}" -- samples/Mobizon.Net.ConsoleSample/Samples/TaskQueueSamples.cs
```
Fix every hit in `src`/`tests`/`samples` (README is handled in Task 17): `TaskStatus` → `BackgroundTaskStatus`; remove any `Id` output from `TaskQueueSamples.cs`.

- [ ] **Step 5: Build + test + commit.**

Run: `dotnet build -c Release; dotnet test --no-build -c Release` → green.

```pwsh
git add -A
git commit -m "refactor!: rename TaskStatus to BackgroundTaskStatus (BCL name clash); drop TaskQueueStatus.Id"
```

---

## Task 11: Shared `DeleteResult`; `Links.DeleteAsync` reports partial failure (spec D9)

**Files:**
- Create: `src/Mobizon.Contracts/Models/Common/DeleteResult.cs`
- Delete: `src/Mobizon.Contracts/Models/ContactGroups/DeleteContactGroupResult.cs`
- Modify: `src/Mobizon.Contracts/Services/IContactGroupService.cs`, `src/Mobizon.Net/Services/ContactGroupService.cs`
- Modify: `src/Mobizon.Contracts/Services/ILinkService.cs:28-38`, `src/Mobizon.Net/Services/LinkService.cs:44-55`
- Test: `tests/Mobizon.Net.Tests/Services/LinkServiceTests.cs` (`DeleteAsync_SendsIdsArray`), `tests/Mobizon.Net.Tests/Services/ContactGroupServiceTests.cs`

**Interfaces:**
- Produces: `public class DeleteResult { IReadOnlyList<long> Processed; IReadOnlyList<long> NotProcessed; bool AllProcessed { get; } }` in `Mobizon.Contracts`; `Task<DeleteResult> ILinkService.DeleteAsync(long[] ids, ct)`; `Task<DeleteResult> IContactGroupService.DeleteAsync(long id, ct)`.

- [ ] **Step 1: Failing test.** Replace `LinkServiceTests.DeleteAsync_SendsIdsArray` with:

```csharp
        [Fact]
        public async Task DeleteAsync_SendsIdsArray_AndReturnsProcessedLists()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/link/delete")
                .WithFormData("ids[0]", "10")
                .WithFormData("ids[1]", "20")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""processed"":[""10""],""notProcessed"":[""20""]},""message"":""""}");

            var result = await CreateService(mockHttp).DeleteAsync(new[] { 10L, 20L });

            Assert.Equal(new[] { 10L }, result.Processed);
            Assert.Equal(new[] { 20L }, result.NotProcessed);
            Assert.False(result.AllProcessed);
            mockHttp.VerifyNoOutstandingExpectation();
        }
```
In `ContactGroupServiceTests` add after `DeleteAsync_SendsFormData_ReturnsProcessedIds`'s existing asserts: `Assert.True(result.AllProcessed);`.

- [ ] **Step 2: Run — expect compile errors** (`DeleteAsync` returns `Task`; `AllProcessed` missing).

- [ ] **Step 3: Create `src/Mobizon.Contracts/Models/Common/DeleteResult.cs`.**

```csharp
using System;
using System.Collections.Generic;

namespace Mobizon.Contracts
{
    /// <summary>
    /// Outcome of a delete call: the IDs the API removed and the IDs it refused
    /// (not found, not owned, or in a state that forbids deletion).
    /// </summary>
    public class DeleteResult
    {
        /// <summary>IDs that were deleted.</summary>
        public IReadOnlyList<long> Processed { get; set; } = Array.Empty<long>();

        /// <summary>IDs that were not deleted.</summary>
        public IReadOnlyList<long> NotProcessed { get; set; } = Array.Empty<long>();

        /// <summary><see langword="true"/> when every requested ID was deleted.</summary>
        public bool AllProcessed => NotProcessed.Count == 0;
    }
}
```

- [ ] **Step 4: Replace `DeleteContactGroupResult` everywhere.**

```pwsh
git rm src/Mobizon.Contracts/Models/ContactGroups/DeleteContactGroupResult.cs
foreach ($f in (git grep -l "DeleteContactGroupResult" -- src tests samples/Mobizon.Net.ConsoleSample)) {
  (Get-Content $f -Raw) -replace 'DeleteContactGroupResult', 'DeleteResult' | Set-Content $f -NoNewline
}
```

- [ ] **Step 5: `Links.DeleteAsync` returns the result.** In `ILinkService.cs` change the signature to `Task<DeleteResult> DeleteAsync(long[] ids, CancellationToken cancellationToken = default);` and the `<returns>` doc to `/// <returns>A <see cref="DeleteResult"/> listing the deleted and the refused link IDs.</returns>`. In `LinkService.cs` replace the body's last statement:

```csharp
            return (await _apiClient.SendAsync<DeleteResult>(
                ModuleName, "delete", parameters, cancellationToken).ConfigureAwait(false)).Data;
```
and change the method's return type to `Task<DeleteResult>`.

- [ ] **Step 6: Build + test + commit.**

Run: `dotnet build -c Release; dotnet test --no-build -c Release` → green.

```pwsh
git add -A
git commit -m "refactor!: shared DeleteResult; Links.DeleteAsync returns processed/notProcessed ids"
```

---

## Task 12: Consistent `long` IDs — contact groups, recipient groups, campaign group criteria (spec D9)

**Files:**
- Modify: `src/Mobizon.Contracts/Services/IContactCardSet.cs:62`, `src/Mobizon.Net/ContactCards/ContactCardSet.cs:105-109`, `src/Mobizon.Net/Services/ContactCardService.cs:102-117`
- Modify: `src/Mobizon.Contracts/Models/ContactCards/ContactGroupRef.cs`
- Modify: `src/Mobizon.Contracts/Models/Campaigns/AddRecipientsRequest.cs` (`RecipientGroups`), `src/Mobizon.Net/Services/CampaignService.cs:334-336`
- Modify: `src/Mobizon.Contracts/Models/Campaigns/CampaignListRequest.cs` (`CampaignCriteria.Groups`), `src/Mobizon.Net/Services/CampaignService.cs:150-152`
- Test: `tests/Mobizon.Net.Tests/Services/ContactCardServiceTests.cs`, `CampaignServiceTests.cs`, `ContactCardQueryTests.cs` (wherever the changed members appear)

**Interfaces:**
- Produces: `Task IContactCardSet.SetGroupsAsync(long id, IReadOnlyList<long> groupIds, ct)`; `ContactGroupRef.Id : long?`; `AddRecipientsRequest.RecipientGroups : IReadOnlyList<long>?`; `CampaignCriteria.Groups : IReadOnlyList<long>?`. `AddRecipientsRequest.RecipientContacts` stays `IReadOnlyList<string>?` (the API accepts `{cardId}:{fieldKey}`).

- [ ] **Step 1: Find every affected test/sample line.**

```pwsh
git grep -n "SetGroupsAsync(\|RecipientGroups\s*=\|Groups\s*=\s*new\|\.Id)" -- tests samples/Mobizon.Net.ConsoleSample | Select-String "ContactGroupRef|SetGroups|RecipientGroups|Groups ="
```
For each hit convert string literals to `long` literals, e.g. `new[] { "100604" }` → `new[] { 100604L }`, `SetGroupsAsync(1, new[] { "5" })` → `SetGroupsAsync(1, new[] { 5L })`, and `Assert.Equal("100604", card.Groups[0].Id)` → `Assert.Equal(100604L, card.Groups[0].Id)`. Do not change `RecipientContacts` usages.

- [ ] **Step 2: Run — expect compile errors** in the edited tests.

- [ ] **Step 3: Change the contracts.**

`ContactGroupRef.cs`: `public string? Id { get; set; }` → `public long? Id { get; set; }` (the globally registered `StringToLongConverter` already turns `"100604"` into `100604`).

`IContactCardSet.cs`: `Task SetGroupsAsync(long id, IReadOnlyList<long> groupIds, CancellationToken cancellationToken = default);`

`AddRecipientsRequest.cs`: `public IReadOnlyList<long>? RecipientGroups { get; set; }`

`CampaignListRequest.cs` (`CampaignCriteria`): `public IReadOnlyList<long>? Groups { get; set; }`

- [ ] **Step 4: Change the implementations.**

`ContactCardSet.SetGroupsAsync`: parameter `IReadOnlyList<long> groupIds`, pass through unchanged.

`ContactCardService.SetGroupsAsync(string id, IReadOnlyList<long> groupIds, …)`: the loop becomes `parameters[$"groupIds[{i}]"] = ApiFormat.Int(groupIds[i]);`.

`CampaignService.SendAddRecipientsAsync`: `parameters[$"recipientGroups[{i}]"] = ApiFormat.Int(request.RecipientGroups[i]);`

`CampaignService.ListAsync`: `parameters[$"criteria[groups][{i}]"] = ApiFormat.Int(c.Groups[i]);`

- [ ] **Step 5: Build + test + commit.**

Run: `dotnet build -c Release; dotnet test --no-build -c Release` → green.

```pwsh
git add -A
git commit -m "refactor!: long ids for contact-group references, recipientGroups and campaign group criteria"
```

---

## Task 13: Flags and enums instead of `int?` (spec D9)

**Files:**
- Modify: `src/Mobizon.Contracts/Models/Messages/MessageListRequest.cs` (`WithNumberInfo`), `src/Mobizon.Net/Services/MessageService.cs:153-154`
- Modify: `src/Mobizon.Contracts/Services/ICampaignService.cs:49-64`, `src/Mobizon.Net/Services/CampaignService.cs:91-104`
- Modify: `src/Mobizon.Contracts/Models/Campaigns/CampaignListRequest.cs` (`CampaignCriteria.Type`), `src/Mobizon.Net/Services/CampaignService.cs:147-148`
- Test: `tests/Mobizon.Net.Tests/Services/MessageServiceTests.cs`, `tests/Mobizon.Net.Tests/Services/CampaignServiceTests.cs`

**Interfaces:**
- Produces: `MessageListRequest.WithNumberInfo : bool?`; `Task<CampaignInfo> ICampaignService.GetInfoAsync(long id, bool? fillTemplateText = null, CancellationToken cancellationToken = default)`; `CampaignCriteria.Type : CampaignType?`.

- [ ] **Step 1: Failing tests.** Append to `MessageServiceTests`:

```csharp
        [Fact]
        public async Task ListAsync_WithNumberInfo_SendsFlagAsOneOrZero()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/Message/List")
                .WithFormData("withNumberInfo", "1")
                .Respond("application/json", @"{""code"":0,""data"":{""items"":[],""totalItemCount"":""0""},""message"":""""}");

            await CreateService(mockHttp).ListAsync(new MessageListRequest { WithNumberInfo = true });

            mockHttp.VerifyNoOutstandingExpectation();
        }
```
Append to `CampaignServiceTests`:

```csharp
        [Fact]
        public async Task GetInfoAsync_FillTemplateText_SendsFlag()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/Campaign/GetInfo")
                .WithFormData("id", "123")
                .WithFormData("getFilledTplCampaignText", "0")
                .Respond("application/json", Fixtures.Load("campaign.getInfo.json"));

            await CreateService(mockHttp).GetInfoAsync(123, fillTemplateText: false);

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task ListAsync_TypeCriteria_SendsNumericCampaignType()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/Campaign/List")
                .WithFormData("criteria[type]", "3")
                .Respond("application/json", @"{""code"":0,""data"":{""items"":[],""totalItemCount"":""0""},""message"":""""}");

            await CreateService(mockHttp).ListAsync(new CampaignListRequest
            {
                Criteria = new CampaignCriteria { Type = CampaignType.Template }
            });

            mockHttp.VerifyNoOutstandingExpectation();
        }
```
(URL casing matters to MockHttp: `CampaignService` sends `Campaign/GetInfo` and `Campaign/List`, `MessageService` sends `Message/List` and `Message/SendSmsMessage`; the other services use lower-case module/method names.)

- [ ] **Step 2: Run — expect compile errors.**

- [ ] **Step 3: Change contracts and services.**

`MessageListRequest.cs`: `public bool? WithNumberInfo { get; set; }` with doc `/// <summary>Include recipient number info (<c>countryA2</c>, <c>operatorName</c>) in each item.</summary>`.
`MessageService.ListAsync`: `if (request.WithNumberInfo.HasValue) parameters["withNumberInfo"] = ApiFormat.Bool(request.WithNumberInfo.Value);`

`ICampaignService.GetInfoAsync`: signature `Task<CampaignInfo> GetInfoAsync(long id, bool? fillTemplateText = null, CancellationToken cancellationToken = default);` and the `<param name="fillTemplateText">` doc: `For template campaigns: <see langword="true"/> (API default) returns the text filled with real recipient data; <see langword="false"/> returns the raw text with placeholders.`
`CampaignService.GetInfoAsync`: same signature; `if (fillTemplateText.HasValue) parameters["getFilledTplCampaignText"] = ApiFormat.Bool(fillTemplateText.Value);`

`CampaignCriteria.Type`: `public CampaignType? Type { get; set; }`; `CampaignService.ListAsync`: `parameters["criteria[type]"] = ApiFormat.Int((int)c.Type.Value);`

- [ ] **Step 4: Fix other call sites.**

```pwsh
git grep -n "GetInfoAsync(\|WithNumberInfo\|Type = [0-9]" -- src tests samples/Mobizon.Net.ConsoleSample
```
Update any positional `GetInfoAsync(id, 1)` → `GetInfoAsync(id, fillTemplateText: true)`, `WithNumberInfo = 1` → `= true`, `Type = 2` → `Type = CampaignType.Bulk`.

- [ ] **Step 5: Build + test + commit.**

Run: `dotnet build -c Release; dotnet test --no-build -c Release` → green.

```pwsh
git add -A
git commit -m "refactor!: bool?/enum instead of int? for WithNumberInfo, GetInfoAsync(fillTemplateText), CampaignCriteria.Type"
```

---

## Task 14: Typed values — `Balance` decimal, `Gender` enum, link `DateTime`s, `GetStats` id limit (spec D9)

**Files:**
- Modify: `src/Mobizon.Contracts/Models/Users/BalanceResult.cs`
- Create: `src/Mobizon.Net/Internal/Converters/TolerantStringEnumConverter.cs`; Delete: `src/Mobizon.Net/Internal/Converters/ContactTypeConverter.cs`
- Modify: `src/Mobizon.Net/Internal/MobizonApiClient.cs` (converter registrations), `src/Mobizon.Net/Internal/ApiFormat.cs` (`Gender`)
- Modify: `src/Mobizon.Contracts/Models/ContactCards/ContactCard.cs`, `ContactCardFields.cs`, `CreateContactCardRequest.cs`, `UpdateContactCardRequest.cs` (`Gender?`), `src/Mobizon.Net/Services/ContactCardService.cs` (`BuildCardFields`)
- Modify: `src/Mobizon.Contracts/Models/Links/LinkData.cs`, `CreateLinkRequest.cs`, `UpdateLinkRequest.cs`, `GetLinkStatsRequest.cs`; `src/Mobizon.Net/Services/LinkService.cs`
- Test: `UserServiceTests.cs`, `Extensions/IntegrationTests.cs`, `ContactCardServiceTests.cs`, `LinkServiceTests.cs`

**Interfaces:**
- Produces: `BalanceResult.Balance : decimal`; `ContactCard.Gender`/`ContactCardFields.Gender`/`CreateContactCardRequest.Gender`/`UpdateContactCardRequest.Gender : Gender?`; `ApiFormat.Gender(Gender?) → "male" | "female" | ""`; `LinkData.ExpirationDate`, `LinkData.RealExpirationDate`, `CreateLinkRequest.ExpirationDate`, `UpdateLinkRequest.ExpirationDate`, `GetLinkStatsRequest.DateFrom`, `GetLinkStatsRequest.DateTo : DateTime?`; `internal class TolerantStringEnumConverter<TEnum> : JsonConverter<TEnum?>`.

- [ ] **Step 1: Failing tests.**

`UserServiceTests`: `Assert.Equal("4043.0656", result.Balance)` → `Assert.Equal(4043.0656m, result.Balance)`. `Extensions/IntegrationTests.cs`: `Assert.Equal("100.50", result.Balance)` → `Assert.Equal(100.50m, result.Balance)`.

`ContactCardServiceTests`: `Assert.Equal("male", f.Gender)` → `Assert.Equal(Gender.Male, f.Gender)`; every `Gender = "male"`/`"female"` in requests → `Gender = Gender.Male`/`Gender.Female`. Append:

```csharp
        [Fact]
        public async Task CreateAsync_Gender_IsSentLowercase()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/contactcard/create")
                .With(req =>
                {
                    // same style as the existing multipart assertions in this class
                    var content = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                    return content.Contains("data[gender]") && content.Contains("female") && !content.Contains("Female");
                })
                .Respond("application/json", @"{""code"":0,""data"":""777"",""message"":""""}");

            await CreateService(mockHttp).CreateAsync(new CreateContactCardRequest { Name = "A", Gender = Gender.Female });

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Theory]
        [InlineData("\"male\"", true)]
        [InlineData("\"MALE\"", true)]
        [InlineData("\"\"", false)]
        [InlineData("\"other\"", false)]
        [InlineData("[]", false)]
        public async Task GetAsync_Gender_IsParsedTolerantly(string genderJson, bool expectMale)
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, "https://api.mobizon.kz/service/contactcard/get")
                .Respond("application/json",
                    "{\"code\":0,\"data\":{\"id\":\"1\",\"fields\":{\"name\":\"A\",\"gender\":" + genderJson + "}},\"message\":\"\"}");

            var card = await CreateService(mockHttp).GetAsync("1");

            Assert.Equal(expectMale ? Gender.Male : (Gender?)null, card.Fields!.Gender);
        }
```
(`CreateService` is the existing helper in that class; it returns the internal `ContactCardService`.)

`LinkServiceTests.CreateAsync_WithOptionalParams_SendsAll`: `ExpirationDate = "2025-12-31"` → `ExpirationDate = new DateTime(2025, 12, 31)`, and add `Assert.Equal(new DateTime(2025, 12, 31), result.ExpirationDate);`. Append:

```csharp
        [Fact]
        public async Task GetStatsAsync_DateRange_IsSentAsDateTime()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/link/getstats")
                .WithFormData("criteria[dateFrom]", "2026-01-01 00:00:00")
                .WithFormData("criteria[dateTo]", "2026-01-31 23:59:59")
                .Respond("application/json", Fixtures.Load("link.getStats.json"));

            await CreateService(mockHttp).GetStatsAsync(new GetLinkStatsRequest
            {
                Ids = new[] { 1L },
                Type = LinkStatsType.Daily,
                DateFrom = new DateTime(2026, 1, 1),
                DateTo = new DateTime(2026, 1, 31, 23, 59, 59)
            });

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        public async Task GetStatsAsync_RejectsInvalidIdCount(int count)
        {
            var ids = new long[count];
            for (var i = 0; i < count; i++) ids[i] = i + 1;

            await Assert.ThrowsAsync<ArgumentException>(() =>
                CreateService(new MockHttpMessageHandler()).GetStatsAsync(new GetLinkStatsRequest { Ids = ids, Type = LinkStatsType.Daily }));
        }
```
Check the fixture-driven `GetStatsAsync` tests: if any passes `DateFrom = "…"` strings, convert to `DateTime`.

- [ ] **Step 2: Run — expect compile errors.**

- [ ] **Step 3: `Balance` → decimal.** `BalanceResult.cs`: `public decimal Balance { get; set; }` (doc: "Current balance with 4 decimal places, in <see cref="Currency"/>."). The registered `StringToDecimalConverter` parses `"4043.0656"`.

- [ ] **Step 4: Generalise the tolerant enum converter.** Create `src/Mobizon.Net/Internal/Converters/TolerantStringEnumConverter.cs`:

```csharp
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mobizon.Net.Internal.Converters
{
    /// <summary>
    /// Reads a string-valued enum case-insensitively. Anything that is not a recognised string
    /// (empty string, `[]`/`{}` from the PHP API for an unset field, an unknown future value)
    /// becomes <see langword="null"/> instead of failing the whole response.
    /// </summary>
    internal class TolerantStringEnumConverter<TEnum> : JsonConverter<TEnum?>
        where TEnum : struct, Enum
    {
        public override TEnum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.String)
            {
                reader.Skip();
                return null;
            }

            var s = reader.GetString();
            if (string.IsNullOrWhiteSpace(s))
                return null;

            return Enum.TryParse<TEnum>(s, ignoreCase: true, out var result) ? result : (TEnum?)null;
        }

        public override void Write(Utf8JsonWriter writer, TEnum? value, JsonSerializerOptions options)
        {
            if (value is null)
                writer.WriteNullValue();
            else
                writer.WriteStringValue(value.Value.ToString().ToUpperInvariant());
        }
    }
}
```
Delete `ContactTypeConverter.cs` (`git rm`). In `MobizonApiClient.JsonOptions` replace `new ContactTypeConverter(),` with:

```csharp
                new TolerantStringEnumConverter<ContactType>(),
                new TolerantStringEnumConverter<Gender>(),
```

- [ ] **Step 5: `Gender?` on the four DTOs + write mapping.** Change `public string? Gender { get; set; }` to `public Gender? Gender { get; set; }` in `ContactCard.cs`, `ContactCardFields.cs`, `CreateContactCardRequest.cs`, `UpdateContactCardRequest.cs`. Add to `ApiFormat`:

```csharp
        /// <summary>Wire form of a contact's gender. Empty string clears the field.</summary>
        public static string Gender(Gender? value)
        {
            switch (value)
            {
                case Contracts.Gender.Male:   return "male";
                case Contracts.Gender.Female: return "female";
                default:                      return string.Empty;
            }
        }
```
(add `using Mobizon.Contracts;` to `ApiFormat.cs`). In `ContactCardService.BuildCardFields` change the parameter to `Gender? gender` and the field line to `["data[gender]"] = ApiFormat.Gender(gender),`. `ContactCardMapper` compiles unchanged (it passes the value through).

- [ ] **Step 6: Link dates.** `LinkData.cs`: `ExpirationDate` and `RealExpirationDate` become `DateTime?` (keep `[JsonPropertyName]`s). `CreateLinkRequest.cs`, `UpdateLinkRequest.cs`: `public DateTime? ExpirationDate { get; set; }` (doc: "Last day the link is valid, in the account's time zone. `null` = never expires."). `GetLinkStatsRequest.cs`: `public DateTime? DateFrom { get; set; }`, `public DateTime? DateTo { get; set; }`. In `LinkService.cs`:

```csharp
            if (request.ExpirationDate.HasValue)
                parameters["data[expirationDate]"] = ApiFormat.Date(request.ExpirationDate.Value);
```
(in both `CreateAsync` and `UpdateAsync`), and in `GetStatsAsync`:

```csharp
            if (request.Ids == null || request.Ids.Length == 0 || request.Ids.Length > 5)
                throw new ArgumentException("link/getStats accepts 1 to 5 link ids per request.", nameof(request));
            ...
            if (request.DateFrom.HasValue)
                parameters["criteria[dateFrom]"] = ApiFormat.DateTime(request.DateFrom.Value);
            if (request.DateTo.HasValue)
                parameters["criteria[dateTo]"] = ApiFormat.DateTime(request.DateTo.Value);
```
(add `using System;` to `LinkService.cs`).

- [ ] **Step 7: Fix remaining call sites.**

```pwsh
git grep -n "ExpirationDate = \"\|DateFrom = \"\|DateTo = \"\|Gender = \"\|\.Balance" -- src tests samples/Mobizon.Net.ConsoleSample
```
Convert string literals to `new DateTime(...)` / `Gender.X`; `UserSamples` prints `{result.Balance:0.0000}`.

- [ ] **Step 8: Build + test + commit.**

Run: `dotnet build -c Release; dotnet test --no-build -c Release` → green.

```pwsh
git add -A
git commit -m "refactor!: decimal Balance, Gender enum (tolerant converter), DateTime link fields, GetStats id-count validation"
```

---

## Task 15: `ShortenLinks` for single SMS, `IMobizonClient` without `IDisposable`, `Campaigns.GetLinksAsync` (spec D9)

**Files:**
- Modify: `src/Mobizon.Contracts/Models/Messages/SmsMessageParameters.cs`, `src/Mobizon.Net/Services/MessageService.cs:49-64`
- Modify: `src/Mobizon.Contracts/Services/IMobizonClient.cs:8`, `src/Mobizon.Net/MobizonClient.cs:21`
- Modify: `src/Mobizon.Contracts/Services/ICampaignService.cs`, `src/Mobizon.Net/Services/CampaignService.cs`
- Test: `MessageServiceTests.cs`, `CampaignServiceTests.cs`, `MobizonClientTests.cs`

**Interfaces:**
- Produces: `SmsMessageParameters.ShortenLinks : bool?`; `public interface IMobizonClient` (no base); `public class MobizonClient : IMobizonClient, IDisposable`; `Task<IReadOnlyList<LinkData>> ICampaignService.GetLinksAsync(long campaignId, CancellationToken cancellationToken = default)`.

- [ ] **Step 1: Failing tests.** `MessageServiceTests`:

```csharp
        [Fact]
        public async Task SendSmsMessageAsync_ShortenLinks_SendsParam()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/Message/SendSmsMessage")
                .WithFormData("params[shortenLinks]", "1")
                .Respond("application/json", Fixtures.Load("message.sendSmsMessage.json"));

            await CreateService(mockHttp).SendSmsMessageAsync(new SendSmsMessageRequest
            {
                Recipient = "77001234567",
                Text = "https://example.com/very/long",
                Parameters = new SmsMessageParameters { ShortenLinks = true }
            });

            mockHttp.VerifyNoOutstandingExpectation();
        }
```
`CampaignServiceTests`:

```csharp
        [Fact]
        public async Task GetLinksAsync_CallsLinkModule()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/link/getlinks")
                .WithFormData("campaignId", "42")
                .Respond("application/json", @"{""code"":0,""data"":[{""id"":""7"",""code"":""abc"",""fullLink"":""https://e.com"",""shortLink"":""https://mbzn.co/abc"",""clickCnt"":""3"",""redirectCnt"":""1""}],""message"":""""}");

            var links = await CreateService(mockHttp).GetLinksAsync(42);

            Assert.Single(links);
            Assert.Equal(7, links[0].Id);
            mockHttp.VerifyNoOutstandingExpectation();
        }
```
`MobizonClientTests` — add:

```csharp
        [Fact]
        public void IMobizonClient_IsNotDisposable_ButMobizonClientIs()
        {
            Assert.False(typeof(System.IDisposable).IsAssignableFrom(typeof(IMobizonClient)));
            Assert.True(typeof(System.IDisposable).IsAssignableFrom(typeof(MobizonClient)));
        }
```

- [ ] **Step 2: Run — expect compile errors / failures.**

- [ ] **Step 3: Implement.**

`SmsMessageParameters.cs` — add:

```csharp
        /// <summary>Shorten every URL in <see cref="SendSmsMessageRequest.Text"/> with the Mobizon link shortener (<c>params[shortenLinks]</c>).</summary>
        public bool? ShortenLinks { get; set; }
```
`MessageService.SendSmsMessageAsync` — inside the `if (request.Parameters != null)` block add:

```csharp
                if (p.ShortenLinks.HasValue)
                    parameters["params[shortenLinks]"] = ApiFormat.Bool(p.ShortenLinks.Value);
```

`IMobizonClient.cs`: `public interface IMobizonClient : IDisposable` → `public interface IMobizonClient`; drop the now-unused `using System;`. Add to its summary: `Implementations created via DI are owned by the container; when you construct <c>MobizonClient</c> yourself, dispose that concrete instance.`
`MobizonClient.cs`: `public class MobizonClient : IMobizonClient` → `public class MobizonClient : IMobizonClient, IDisposable`.

`ICampaignService.cs` — add:

```csharp
        /// <summary>
        /// Returns the short links used by a campaign, with their click counters.
        /// Same endpoint as <see cref="ILinkService.GetLinksAsync"/> (<c>link/getLinks</c>), exposed here for discoverability.
        /// </summary>
        /// <param name="campaignId">The campaign ID.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>The campaign's <see cref="LinkData"/> items (bare array — not a paged envelope).</returns>
        /// <exception cref="MobizonApiException">Thrown when the API returns a non-success response code.</exception>
        Task<IReadOnlyList<LinkData>> GetLinksAsync(long campaignId, CancellationToken cancellationToken = default);
```
(add `using System.Collections.Generic;`). `CampaignService.cs` — add:

```csharp
        public async Task<IReadOnlyList<LinkData>> GetLinksAsync(
            long campaignId, CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string> { ["campaignId"] = ApiFormat.Int(campaignId) };
            return (await _apiClient.SendAsync<IReadOnlyList<LinkData>>(
                "link", "getlinks", parameters, cancellationToken).ConfigureAwait(false)).Data;
        }
```

- [ ] **Step 4: Fix consumers of `IMobizonClient` disposal.**

```pwsh
git grep -n "using var client = provider\|using (var client = provider\|IMobizonClient.*Dispose" -- tests samples/Mobizon.Net.ConsoleSample
```
Remove `using` from any DI-resolved `IMobizonClient` (the container owns it).

- [ ] **Step 5: Build + test + commit.**

Run: `dotnet build -c Release; dotnet test --no-build -c Release` → green.

```pwsh
git add -A
git commit -m "feat: SmsMessageParameters.ShortenLinks; Campaigns.GetLinksAsync; IMobizonClient no longer IDisposable"
```

---

## Task 16: `SortDirection.Ascending/Descending`, `PageSize` 25, immutable `ContactCardQuery` with `Page()` (spec D9)

**Files:**
- Modify: `src/Mobizon.Contracts/Models/Common/SortDirection.cs`, `PaginationRequest.cs`; `src/Mobizon.Net/Internal/ApiFormat.cs` (`Sort`)
- Modify: every service that renders `sort[...]` (`MessageService`, `CampaignService`, `LinkService`, `ContactCardService`, `ContactGroupService`, `NumberStopListService`) — scripted
- Modify: `src/Mobizon.Contracts/Services/IContactCardQuery.cs`, `IContactCardSet.cs`; `src/Mobizon.Net/ContactCards/ContactCardQuery.cs` (full rewrite), `ContactCardSet.cs`
- Test: `tests/Mobizon.Net.Tests/Services/ContactCardQueryTests.cs` and any test using `SortDirection.ASC/DESC` or the `20` default page size

**Interfaces:**
- Produces: `enum SortDirection { Ascending, Descending }`; `ApiFormat.Sort(SortDirection) → "ASC"/"DESC"`; `PaginationRequest.PageSize` default `25`; `IContactCardQuery.Page(int pageIndex)` (replaces `Skip`); `IContactCardSet.Page(int pageIndex)` (replaces `Skip`); every `IContactCardQuery` builder call returns a **new** instance.

- [ ] **Step 1: Failing tests.** In `ContactCardQueryTests`: `Take(25).Skip(50)` → `Take(25).Page(2)`; `Take(25).Skip(25)` → `Take(25).Page(1)`; `.Skip(0)` → `.Page(0)`; rename the affected test methods `Skip_…` → `Page_…`. Append:

```csharp
        [Fact]
        public async Task Builder_IsImmutable_BaseQueryCanBeReused()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, ListUrl)
                .WithFormData("pagination[currentPage]", "0")
                .WithFormData("pagination[pageSize]", "10")
                .Respond("application/json", EmptyListJson);
            mockHttp.Expect(HttpMethod.Post, ListUrl)
                .WithFormData("pagination[currentPage]", "1")
                .WithFormData("pagination[pageSize]", "10")
                .Respond("application/json", EmptyListJson);

            var baseQuery = CreateSet(mockHttp).Take(10);
            var first = baseQuery.Page(0);
            var second = baseQuery.Page(1);

            Assert.NotSame(baseQuery, first);
            Assert.NotSame(first, second);
            await first.ToListAsync();
            await second.ToListAsync();

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public void Take_NonPositive_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateSet(new MockHttpMessageHandler()).Take(0));
        }

        [Fact]
        public void Page_Negative_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateSet(new MockHttpMessageHandler()).Page(-1));
        }
```
Rename enum members in tests/samples now so the build breaks in one place:

```pwsh
foreach ($f in (git grep -l "SortDirection\." -- src tests samples/Mobizon.Net.ConsoleSample)) {
  (Get-Content $f -Raw) -replace 'SortDirection\.ASC\b', 'SortDirection.Ascending' -replace 'SortDirection\.DESC\b', 'SortDirection.Descending' | Set-Content $f -NoNewline
}
git grep -n 'pageSize\]", "20"' -- tests
```
If the last grep finds tests that rely on the old default page size (no explicit `PageSize`), change the expected value to `"25"`.

- [ ] **Step 2: Run — expect compile errors.**

- [ ] **Step 3: Enum, default, formatter.**

`SortDirection.cs`:

```csharp
namespace Mobizon.Contracts
{
    /// <summary>Sort order for list requests.</summary>
    public enum SortDirection
    {
        Ascending,
        Descending
    }
}
```
`SortRequest.cs`: default `Direction = SortDirection.Ascending`. `PaginationRequest.cs`: `public int PageSize { get; set; } = 25;` (doc: "Items per page. API default is 25; maximum 100.").
`ApiFormat.cs` — add: `public static string Sort(SortDirection direction) => direction == SortDirection.Descending ? "DESC" : "ASC";`

Route every service through it:

```pwsh
foreach ($f in (Get-ChildItem src/Mobizon.Net/Services -Filter *.cs | ForEach-Object FullName)) {
  $c = Get-Content $f -Raw
  $n = $c -replace '(\w+)\.Sort\.Direction\.ToString\(\)', 'ApiFormat.Sort($1.Sort.Direction)'
  if ($n -ne $c) { Set-Content $f $n -NoNewline; Write-Host "rewrote $f" }
}
git grep -n "Direction.ToString" -- src
```
Expected: 6 files rewritten; final grep empty. Services that lack `using Mobizon.Net.Internal;` already have it (they use `MobizonApiClient`).

- [ ] **Step 4: Interfaces — `Skip` → `Page`.** In `IContactCardQuery.cs` and `IContactCardSet.cs` replace the `Skip` member with:

```csharp
        /// <summary>
        /// Selects the zero-based page to return. Pair with <see cref="Take"/> to set the page size
        /// (default 25). Unlike LINQ's <c>Skip</c>, this maps 1:1 onto the API's <c>pagination[currentPage]</c>.
        /// </summary>
        IContactCardQuery Page(int pageIndex);
```
and change the `Take` summary to `/// <summary>Sets the page size (items per request). Must be positive; default 25.</summary>`. Add to `IContactCardQuery`'s type summary: `Every builder method returns a new query and leaves the receiver untouched.`

- [ ] **Step 5: Rewrite `src/Mobizon.Net/ContactCards/ContactCardQuery.cs` entirely:**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts;
using Mobizon.Net.Internal;
using Mobizon.Net.Services;

namespace Mobizon.Net
{
    /// <summary>
    /// An immutable, composable query over <c>contactcard/list</c>. Every builder call returns a new
    /// query and never modifies the receiver, so a base query can be reused:
    /// <code>
    /// var kz = client.ContactCards.Where(x => x.Address.CountryA2 == "KZ").Take(50);
    /// var page0 = await kz.Page(0).ToListAsync();
    /// var page1 = await kz.Page(1).ToListAsync();
    /// </code>
    /// </summary>
    public sealed class ContactCardQuery : IContactCardQuery
    {
        private const int DefaultPageSize = 25;

        private readonly ContactCardService _service;
        private readonly Expression<Func<ContactCardFilterSpec, bool>>? _predicate;
        private readonly int? _take;
        private readonly int? _page;
        private readonly string? _sortField;
        private readonly SortDirection _sortDirection;

        internal ContactCardQuery(ContactCardService service)
            : this(service, null, null, null, null, SortDirection.Ascending)
        {
        }

        private ContactCardQuery(
            ContactCardService service,
            Expression<Func<ContactCardFilterSpec, bool>>? predicate,
            int? take,
            int? page,
            string? sortField,
            SortDirection sortDirection)
        {
            _service = service;
            _predicate = predicate;
            _take = take;
            _page = page;
            _sortField = sortField;
            _sortDirection = sortDirection;
        }

        /// <inheritdoc />
        public IContactCardQuery Where(Expression<Func<ContactCardFilterSpec, bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            var combined = _predicate == null ? predicate : CombineAnd(_predicate, predicate);
            return new ContactCardQuery(_service, combined, _take, _page, _sortField, _sortDirection);
        }

        /// <inheritdoc />
        public IContactCardQuery Take(int count)
        {
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Page size must be positive.");
            return new ContactCardQuery(_service, _predicate, count, _page, _sortField, _sortDirection);
        }

        /// <inheritdoc />
        public IContactCardQuery Page(int pageIndex)
        {
            if (pageIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(pageIndex), "Page index is zero-based and cannot be negative.");
            return new ContactCardQuery(_service, _predicate, _take, pageIndex, _sortField, _sortDirection);
        }

        /// <inheritdoc />
        public IContactCardQuery OrderBy<TKey>(Expression<Func<ContactCardFilterSpec, TKey>> keySelector)
            => new ContactCardQuery(_service, _predicate, _take, _page, ExtractFieldName(keySelector), SortDirection.Ascending);

        /// <inheritdoc />
        public IContactCardQuery OrderByDescending<TKey>(Expression<Func<ContactCardFilterSpec, TKey>> keySelector)
            => new ContactCardQuery(_service, _predicate, _take, _page, ExtractFieldName(keySelector), SortDirection.Descending);

        // ── Terminal operations ───────────────────────────────────────────────

        /// <inheritdoc />
        public async Task<IReadOnlyList<ContactCard>> ToListAsync(CancellationToken ct = default)
        {
            var response = await _service.ListAsync(BuildRequest(), ct).ConfigureAwait(false);
            return Map(ItemsOf(response));
        }

        /// <inheritdoc />
        public async Task<PaginatedResponse<ContactCard>> ToPageAsync(CancellationToken ct = default)
        {
            var request = BuildRequest();
            var response = await _service.ListAsync(request, ct).ConfigureAwait(false);
            return new PaginatedResponse<ContactCard>
            {
                Items       = Map(ItemsOf(response)),
                TotalCount  = response?.TotalItemCount ?? 0,
                CurrentPage = request.Pagination?.CurrentPage ?? 0,
                PageSize    = request.Pagination?.PageSize    ?? DefaultPageSize
            };
        }

        /// <inheritdoc />
        public async Task<int> CountAsync(CancellationToken ct = default)
        {
            var response = await _service.ListAsync(BuildRequest(takeOverride: 1), ct).ConfigureAwait(false);
            return response?.TotalItemCount ?? 0;
        }

        /// <inheritdoc />
        public async Task<ContactCard?> FirstOrDefaultAsync(CancellationToken ct = default)
        {
            var response = await _service.ListAsync(BuildRequest(takeOverride: 1), ct).ConfigureAwait(false);
            var items = ItemsOf(response);
            return items.Count > 0 ? ContactCardMapper.ToEntity(items[0]) : null;
        }

        /// <inheritdoc />
        public async Task<ContactCard> FirstAsync(CancellationToken ct = default)
        {
            var result = await FirstOrDefaultAsync(ct).ConfigureAwait(false);
            return result ?? throw new InvalidOperationException("Sequence contains no elements.");
        }

        /// <inheritdoc />
        public async Task<ContactCard?> SingleOrDefaultAsync(CancellationToken ct = default)
        {
            var response = await _service.ListAsync(BuildRequest(takeOverride: 2), ct).ConfigureAwait(false);
            var items = ItemsOf(response);
            if (items.Count > 1)
                throw new InvalidOperationException("Sequence contains more than one element.");
            return items.Count == 1 ? ContactCardMapper.ToEntity(items[0]) : null;
        }

        /// <inheritdoc />
        public async Task<ContactCard> SingleAsync(CancellationToken ct = default)
        {
            var result = await SingleOrDefaultAsync(ct).ConfigureAwait(false);
            return result ?? throw new InvalidOperationException("Sequence contains no elements.");
        }

        // ── Internal helpers ──────────────────────────────────────────────────

        private ContactCardListRequest BuildRequest(int? takeOverride = null)
        {
            PaginationRequest? pagination = null;
            var pageSize = takeOverride ?? _take;
            if (pageSize.HasValue || _page.HasValue)
                pagination = new PaginationRequest { CurrentPage = _page ?? 0, PageSize = pageSize ?? DefaultPageSize };

            return new ContactCardListRequest
            {
                Criteria   = _predicate != null ? ContactCardExpressionParser.Parse(_predicate) : null,
                Pagination = pagination,
                Sort       = _sortField != null
                                 ? new SortRequest { Field = _sortField, Direction = _sortDirection }
                                 : null
            };
        }

        private static Expression<Func<ContactCardFilterSpec, bool>> CombineAnd(
            Expression<Func<ContactCardFilterSpec, bool>> left,
            Expression<Func<ContactCardFilterSpec, bool>> right)
        {
            var param = Expression.Parameter(typeof(ContactCardFilterSpec), "x");
            var body = Expression.AndAlso(
                new ReplaceParam(left.Parameters[0], param).Visit(left.Body),
                new ReplaceParam(right.Parameters[0], param).Visit(right.Body));
            return Expression.Lambda<Func<ContactCardFilterSpec, bool>>(body, param);
        }

        private sealed class ReplaceParam : ExpressionVisitor
        {
            private readonly ParameterExpression _from, _to;
            public ReplaceParam(ParameterExpression from, ParameterExpression to) { _from = from; _to = to; }
            protected override Expression VisitParameter(ParameterExpression node) => node == _from ? _to : base.VisitParameter(node);
        }

        private static IReadOnlyList<ContactCard> Map(IReadOnlyList<ContactCardData> items)
            => items.Select(ContactCardMapper.ToEntity).ToArray();

        // contactcard/list may return success with a null data payload; treat it as an empty page.
        private static IReadOnlyList<ContactCardData> ItemsOf(ContactCardListResult response)
            => response?.Items ?? Array.Empty<ContactCardData>();

        private static string ExtractFieldName<TKey>(Expression<Func<ContactCardFilterSpec, TKey>> expr)
        {
            Expression body = expr.Body is UnaryExpression { NodeType: ExpressionType.Convert } u
                ? u.Operand : expr.Body;

            if (body is MemberExpression)
            {
                var path = new List<string>();
                Expression current = body;
                while (current is MemberExpression m)
                {
                    path.Insert(0, m.Member.Name);
                    current = m.Expression!;
                }
                return ContactCardExpressionParser.GetApiFieldName(path.ToArray());
            }

            throw new ArgumentException(
                "Selector must be a property access, e.g. x => x.Surname or x => x.Mobile.Value.",
                nameof(expr));
        }
    }
}
```

In `ContactCardSet.cs` replace the `Skip` entry point with:

```csharp
        /// <summary>Begins a query positioned on the given zero-based page.</summary>
        public IContactCardQuery Page(int pageIndex)
            => new ContactCardQuery(_service).Page(pageIndex);
```

- [ ] **Step 6: Build + test + commit.**

Run: `dotnet build -c Release; dotnet test --no-build -c Release` → green (`git grep -n "\.Skip(" -- src tests` → empty; LINQ `Skip` in the sample's `Program.cs` is unrelated).

```pwsh
git add -A
git commit -m "refactor!: SortDirection.Ascending/Descending, PageSize default 25, immutable ContactCardQuery with Page()"
```

---

# Phase 3 — docs, sample, release

## Task 17: XML docs, README, CHANGELOG (spec D9 docs rows, D10)

**Files:**
- Modify: `src/Mobizon.Contracts/Services/IContactGroupService.cs:45-52`, `src/Mobizon.Contracts/Services/IContactCardSet.cs:53-54`
- Modify: `README.md`
- Replace: `CHANGELOG.md`

**Interfaces:** none (docs only). Every README snippet must compile against the surface produced by Tasks 9–16.

- [ ] **Step 1: XML docs.**

`IContactGroupService.GetCardsCountAsync` summary → 
```csharp
        /// <summary>
        /// Returns the number of contact cards in the specified group.
        /// Pass <see langword="null"/> to count cards that belong to no group.
        /// </summary>
```
`IContactCardSet.UpdateAsync` summary →
```csharp
        /// <summary>
        /// Updates an existing contact card. <see cref="ContactCard.Id"/> must be set.
        /// <para>
        /// This is a full replace: every editable field of <paramref name="entity"/> is sent, and a
        /// <see langword="null"/> field clears the corresponding value on the server. Load the card with
        /// <see cref="FindAsync"/>, change what you need, then call this method. The only exception is
        /// <see cref="ContactCard.Address"/>, which is left untouched on the server when it is <see langword="null"/>.
        /// </para>
        /// </summary>
```

- [ ] **Step 2: README — apply these edits in order** (line numbers refer to the current file; search by text).

1. Intro (line 3): `A .NET SDK for the [Mobizon](https://mobizon.kz) SMS gateway REST API (v1).` → `An **unofficial** .NET SDK for the [Mobizon](https://mobizon.kz) SMS gateway REST API (v1). Not affiliated with or endorsed by Mobizon.`
2. Features list: replace the `netstandard2.0` bullet with `- **\`netstandard2.0\` + \`net8.0\` targets** — .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+ (ASP.NET Core webhooks: \`net8.0\` / \`net10.0\`)`; add bullets `- **API key never in the URL** — sent in the POST body, so it stays out of proxy and \`HttpClient\` logs` and `- **Actionable errors** — \`MobizonApiException\` (API code) vs \`MobizonException\` (transport/timeout/non-JSON) with the HTTP \`StatusCode\` attached`.
3. Quick Start block (lines 85–105) → 
```csharp
using Mobizon.Contracts;
using Mobizon.Net;

using var client = new MobizonClient(new MobizonClientOptions
{
    ApiKey = "your-api-key-here",
    ApiUrl = "https://api.mobizon.kz"
});

var result = await client.Messages.QuickSendAsync("77001234567", "Hello from Mobizon.Net!");
Console.WriteLine($"Message ID: {result.MessageId}");
```
   and the sentence above it → `Send an SMS in three lines (excluding configuration):`.
4. Messages → "Send an SMS with optional parameters": add `ShortenLinks = true,   // shorten URLs in Text via Mobizon's link shortener` inside `SmsMessageParameters`. "List messages": `Direction = SortDirection.DESC` → `Direction = SortDirection.Descending`.
5. Campaigns: after `var info = await client.Campaigns.GetInfoAsync(campaignId);` add
```csharp
// 6. Short links used by the campaign (same as client.Links.GetLinksAsync)
var links = await client.Campaigns.GetLinksAsync(campaignId);
```
   and `Other available methods: \`GetAsync\`, \`ListAsync\`, \`DeleteAsync\`.` → `Other available methods: \`GetAsync\`, \`ListAsync\`, \`DeleteAsync\`, \`GetLinksAsync\`.`
6. Links: `ExpirationDate = "2026-12-31"` → `ExpirationDate = new DateTime(2026, 12, 31)`; `DateFrom = "2026-01-01", DateTo = "2026-01-31"` → `DateFrom = new DateTime(2026, 1, 1), DateTo = new DateTime(2026, 1, 31, 23, 59, 59)`; add after the `Type = LinkStatsType.Daily` line the comment `// up to 5 ids per request`. "Delete links" block →
```csharp
var deleted = await client.Links.DeleteAsync(new[] { link.Id });
if (!deleted.AllProcessed)
    Console.WriteLine($"Not deleted: {string.Join(", ", deleted.NotProcessed)}");
```
7. User: `Console.WriteLine($"{balance.Balance} {balance.Currency}");` → `Console.WriteLine($"{balance.Balance:0.0000} {balance.Currency}"); // decimal`; delete the line `` `GetOwnBalanceAsync` is the only SDK method that uses HTTP GET. All other methods use POST. `` and put instead: `All SDK calls are HTTP POST; the API key travels in the request body, never in the URL.`
8. TaskQueue: `Console.WriteLine($"Task {taskStatus.Id}: {taskStatus.Progress}% complete");` → `Console.WriteLine($"{taskStatus.Status}: {taskStatus.Progress}% complete");` and `` `TaskQueueStatus` fields: `Id`, `Status`, `Progress` (0–100). `` → `` `TaskQueueStatus` fields: `Status` (`BackgroundTaskStatus`: Pending / InProgress / Completed / Rejected) and `Progress` (0–100). ``
9. Insert a new section before `### Alphanames`:
````markdown
### Contact cards & groups

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
int total      = await vip.CountAsync();

// UpdateAsync is a full replace: load, modify, save. Null fields are cleared on the server.
var existing = await client.ContactCards.FindAsync(card.Id.Value);
existing!.Info = "Preferred customer";
await client.ContactCards.UpdateAsync(existing);
```

Supported filter operators: `==` (including `== null` for "empty"), `!=`, `>=`, `<=`, `.Contains()`, combined with `&&`.
````
10. Webhooks: after `Webhooks are created and configured in the Mobizon **control panel** (not via the API);` insert `choose the **JSON** data format when creating the webhook (this SDK does not parse the \`raw\`/\`xml\` formats);`. After the "Signature" paragraph add:
   `**Webhooks without a secret key**: Mobizon allows creating a webhook without a secret; such requests carry no signature and \`WebhookProcessor\` (and \`MapMobizonWebhook\`) always rejects them with \`SignatureMismatch\` — it fails closed. If you deliberately run unsigned, parse with \`new WebhookParser().Parse(body)\` and protect the endpoint by other means (IP allow-list, private URL).`
11. DI: `` `IMobizonClient` is registered as a **transient** service backed by `IHttpClientFactory`. `` → `` `IMobizonClient` is registered as a **transient** service backed by `IHttpClientFactory`. It is not `IDisposable` — never wrap an injected client in `using`; the factory owns the `HttpClient`. ``
12. Polly: replace the paragraph starting `Chain \`AddMobizonResilience()\`` with:
   `Chain \`AddMobizonResilience()\` after \`AddMobizon()\`. By default the **retry** policy applies only to read-only calls (\`get*\` / \`list\`): retrying \`message/sendSmsMessage\` or \`campaign/send\` after a lost response could send an SMS twice. Set \`RetryNonIdempotentRequests = true\` to retry everything. The **circuit breaker** applies to every call. There is no separate timeout policy — use \`MobizonClientOptions.Timeout\`.`
   In the "Customise" snippet add `resilience.RetryNonIdempotentRequests = false; // default` as the first line inside the lambda.
13. Error Handling table: `MobizonException` row → `` Transport-level failure: network error, `HttpClient.Timeout` expiry, non-JSON response (proxy/5xx HTML page), or a JSON parse problem. `StatusCode` (`HttpStatusCode?`) carries the HTTP status when a response was received; `null` when it was not (network error, timeout). `MobizonApiException` derives from this type. ``. In the snippet's second `catch` replace the `Console.WriteLine` with `Console.WriteLine($"SDK Error [{ex.StatusCode?.ToString() ?? "no response"}]: {ex.Message}");`. Add the sentence: `Cancellation via your own \`CancellationToken\` still surfaces as \`OperationCanceledException\`; only the client's own timeout is wrapped.`
14. Final check: every `using Mobizon.Contracts.Models.*` / `.Services` / `.Exceptions` in README → replaced by `using Mobizon.Contracts;` (`using Mobizon.Contracts.Models.Webhooks;` → `using Mobizon.Contracts.Webhooks;`).

```pwsh
git grep -n "Mobizon.Contracts.Models\|Mobizon.Contracts.Services\|Mobizon.Contracts.Exceptions\|SortDirection.DESC\|SortDirection.ASC\|taskStatus.Id\|TaskStatus\b" -- README.md
```
Expected: no output.

- [ ] **Step 3: Replace `CHANGELOG.md` with:**

```markdown
# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - UNRELEASED

First public release.

### Packages

- `Mobizon.Net` — client for the Mobizon SMS gateway REST API v1 (`netstandard2.0`, `net8.0`)
- `Mobizon.Contracts` — interfaces, DTOs, enums, exceptions
- `Mobizon.Net.Extensions.DependencyInjection` — `AddMobizon()` on `IServiceCollection` (`IHttpClientFactory`)
- `Mobizon.Net.Extensions.Polly` — `AddMobizonResilience()`: retry for read-only calls, circuit breaker for all
- `Mobizon.Net.Webhooks` — signature verification and typed parsing of inbound webhooks
- `Mobizon.Net.Webhooks.AspNetCore` — `AddMobizonWebhooks()` / `MapMobizonWebhook()` (`net8.0`, `net10.0`)

### API coverage

- Message: SendSmsMessage (incl. `shortenLinks`, `validity`, `deferredToTs`, `mclass`), GetSMSStatus, List
- Campaign: Create, AddRecipients (auto-batched by 500, file upload), Send, Get, GetInfo, GetLinks, List, Delete
- Link: Create, Get (by id / code / short URL), GetLinks, GetStats (period-major grid transposed per link), List, Update, Delete
- User: GetOwnBalance · Taskqueue: GetStatus · Alphaname: List
- ContactCard: LINQ-style `Where/OrderBy/Take/Page` query, Find, Add, Update, Remove, SetGroups, GetGroups
- ContactGroup: List, Create, Update, Delete, GetCardsCount · NumberStopList: List, AddNumber, AddNumberRange, Delete
- Webhook events: `sms-delivery-report`, `form-submission`, `form-contact-confirmation`, `form-contact-unsubscribe` (+ forward-compatible `UnknownWebhookEvent`)

### Behaviour worth knowing

- The API key is sent in the POST body, never in the URL; every request is a POST and carries `User-Agent: Mobizon.Net/<version>`.
- All wire formatting is culture-invariant.
- `MobizonApiException` for API error codes; `MobizonException` (with `StatusCode`) for transport errors, `HttpClient` timeouts and non-JSON responses. Caller cancellation is never wrapped.
- Codes 98/99/100 are folded into `AddRecipientsResult.Outcome` and `CampaignSendResult.IsQueued` instead of throwing.
- Polly retries only `get*`/`list` calls unless `RetryNonIdempotentRequests = true`.
- Webhook verification fails closed: no secret / no signature → `SignatureMismatch`.
```

- [ ] **Step 4: Build docs check + commit.**

Run: `dotnet build -c Release` (XML doc changes compile; cref errors would fail the build).

```pwsh
git add -A
git commit -m "docs: README/CHANGELOG/XML docs for 0.1.0 surface"
```

---

## Task 18: Console sample — command-driven, with a DI example (spec D10)

**Files:**
- Replace: `samples/Mobizon.Net.ConsoleSample/Program.cs`
- Verify compile: `samples/Mobizon.Net.ConsoleSample/Samples/*.cs` (already fixed by earlier tasks' grep steps)

**Interfaces:**
- Consumes: the existing static `*Samples` methods, verified against the files on 2026-08-30 — use exactly these names:
  - `UserSamples.GetBalanceAsync(MobizonClient)`
  - `MessageSamples.QuickSendAsync(MobizonClient, string recipient, string text)`, `SendSmsMessageAsync(MobizonClient, string, string)`, `GetStatusAsync(MobizonClient)`, `ListAsync(MobizonClient)`
  - `CampaignSamples.ListAsync/GetAsync/GetInfoAsync/CreateSendDeleteAsync/AddRecipientsAsync(MobizonClient)`
  - `LinkSamples.ListAsync/CreateGetUpdateDeleteAsync/GetStatsAsync(MobizonClient)`
  - `ContactGroupSamples.ListAsync/CreateUpdateDeleteAsync/GetCardsCountAsync(MobizonClient)`
  - `ContactCardSamples.ListAsync/ListByGroupAsync/GetAsync/FirstAndSingleAsync/AddAndUpdateAsync/GroupsAsync/RemoveAsync/ToPageAsync(MobizonClient)` — note: there is **no** `CreateAndSetGroupAsync`, `UpdateAsync` or `GetGroupsAsync`; the current `Program.cs` comments name methods that do not exist
  - `NumberStopListSamples.ListAsync/AddNumberAsync/AddNumberRangeAsync/DeleteAsync(MobizonClient)`
  - `TaskQueueSamples.GetStatusAsync(MobizonClient, long taskId)`
  - `WebhookSamples.ProcessDeliveryReport(string body, string secret)` (returns `void`)

- [ ] **Step 1: Replace `Program.cs` with:**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mobizon.Contracts;
using Mobizon.Net;
using Mobizon.Net.ConsoleSample.Samples;
using Mobizon.Net.Extensions.DependencyInjection;
using Mobizon.Net.Extensions.Polly;

namespace Mobizon.Net.ConsoleSample
{
    /// <summary>
    /// Usage: dotnet run -- &lt;command&gt; [args]
    /// Configure Mobizon:ApiKey / Mobizon:ApiUrl in appsettings.Development.json or env vars Mobizon__ApiKey / Mobizon__ApiUrl.
    /// </summary>
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                              ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                              ?? "Development";

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var apiKey        = configuration["Mobizon:ApiKey"];
            var apiUrl        = configuration["Mobizon:ApiUrl"] ?? "https://api.mobizon.kz";
            var testRecipient = configuration["Mobizon:TestRecipient"] ?? "";
            var testMessage   = configuration["Mobizon:TestMessage"]   ?? "Hello from Mobizon.Net SDK!";

            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("API key is not configured. Set Mobizon:ApiKey in appsettings.Development.json or Mobizon__ApiKey env var.");
                return 2;
            }

            using var client = new MobizonClient(new MobizonClientOptions { ApiKey = apiKey, ApiUrl = apiUrl });

            var commands = new Dictionary<string, Func<string[], Task>>(StringComparer.OrdinalIgnoreCase)
            {
                ["balance"]            = _ => UserSamples.GetBalanceAsync(client),
                ["send"]               = a => MessageSamples.QuickSendAsync(client, a.ElementAtOrDefault(0) ?? testRecipient, a.ElementAtOrDefault(1) ?? testMessage),
                ["send-full"]          = a => MessageSamples.SendSmsMessageAsync(client, a.ElementAtOrDefault(0) ?? testRecipient, a.ElementAtOrDefault(1) ?? testMessage),
                ["sms-status"]         = _ => MessageSamples.GetStatusAsync(client),
                ["sms-list"]           = _ => MessageSamples.ListAsync(client),
                ["campaign-list"]      = _ => CampaignSamples.ListAsync(client),
                ["campaign-get"]       = _ => CampaignSamples.GetAsync(client),
                ["campaign-info"]      = _ => CampaignSamples.GetInfoAsync(client),
                ["campaign-flow"]      = _ => CampaignSamples.CreateSendDeleteAsync(client),
                ["campaign-recipients"]= _ => CampaignSamples.AddRecipientsAsync(client),
                ["link-list"]          = _ => LinkSamples.ListAsync(client),
                ["link-flow"]          = _ => LinkSamples.CreateGetUpdateDeleteAsync(client),
                ["link-stats"]         = _ => LinkSamples.GetStatsAsync(client),
                ["group-list"]         = _ => ContactGroupSamples.ListAsync(client),
                ["group-flow"]         = _ => ContactGroupSamples.CreateUpdateDeleteAsync(client),
                ["group-count"]        = _ => ContactGroupSamples.GetCardsCountAsync(client),
                ["card-list"]          = _ => ContactCardSamples.ListAsync(client),
                ["card-by-group"]      = _ => ContactCardSamples.ListByGroupAsync(client),
                ["card-get"]           = _ => ContactCardSamples.GetAsync(client),
                ["card-first"]         = _ => ContactCardSamples.FirstAndSingleAsync(client),
                ["card-page"]          = _ => ContactCardSamples.ToPageAsync(client),
                ["card-add-update"]    = _ => ContactCardSamples.AddAndUpdateAsync(client),
                ["card-groups"]        = _ => ContactCardSamples.GroupsAsync(client),
                ["card-remove"]        = _ => ContactCardSamples.RemoveAsync(client),
                ["stoplist"]           = _ => NumberStopListSamples.ListAsync(client),
                ["stoplist-add"]       = _ => NumberStopListSamples.AddNumberAsync(client),
                ["stoplist-add-range"] = _ => NumberStopListSamples.AddNumberRangeAsync(client),
                ["stoplist-delete"]    = _ => NumberStopListSamples.DeleteAsync(client),
                ["task"]               = a => TaskQueueSamples.GetStatusAsync(client, long.Parse(a.ElementAtOrDefault(0) ?? "0")),
                ["webhook"]            = a => { WebhookSamples.ProcessDeliveryReport(a.ElementAtOrDefault(0) ?? SampleWebhookBody, a.ElementAtOrDefault(1) ?? "your-webhook-secret"); return Task.CompletedTask; },
                ["di-balance"]         = _ => DiBalanceAsync(apiKey, apiUrl),
            };

            if (args.Length == 0 || !commands.TryGetValue(args[0], out var command))
            {
                Console.WriteLine("Usage: dotnet run -- <command> [args]");
                Console.WriteLine("Commands: " + string.Join(", ", commands.Keys.OrderBy(k => k)));
                return 1;
            }

            try
            {
                await command(args.Skip(1).ToArray());
                return 0;
            }
            catch (MobizonApiException ex)
            {
                Console.WriteLine($"[API Error {ex.RawCode}] {ex.ApiMessage}");
                return 3;
            }
            catch (MobizonException ex)
            {
                Console.WriteLine($"[Error{(ex.StatusCode.HasValue ? " HTTP " + (int)ex.StatusCode.Value : "")}] {ex.Message}");
                return 4;
            }
        }

        /// <summary>The DI route: AddMobizon + AddMobizonResilience, then resolve IMobizonClient (not disposable — the container owns it).</summary>
        private static async Task DiBalanceAsync(string apiKey, string apiUrl)
        {
            var services = new ServiceCollection();
            services.AddMobizon(o => { o.ApiKey = apiKey; o.ApiUrl = apiUrl; })
                    .AddMobizonResilience();

            await using var provider = services.BuildServiceProvider();
            var client = provider.GetRequiredService<IMobizonClient>();

            var balance = await client.User.GetOwnBalanceAsync();
            Console.WriteLine($"=== DI: User.GetOwnBalance ===");
            Console.WriteLine($"Balance : {balance.Balance:0.0000} {balance.Currency}");
        }

        private const string SampleWebhookBody =
            "{\"eventId\":1,\"eventType\":\"sms-delivery-report\",\"eventCreateTs\":\"2026-01-15 11:42:28\",\"webhookId\":1,\"attempt\":1," +
            "\"data\":{\"campaignId\":1,\"messageId\":2,\"segNum\":1,\"status\":\"DELIVRD\",\"to\":\"77001234567\"},\"sign\":\"...\"}";
    }
}
```

- [ ] **Step 2: Build the sample and run the no-arg path.**

```pwsh
dotnet build samples/Mobizon.Net.ConsoleSample -c Release
dotnet run --project samples/Mobizon.Net.ConsoleSample -c Release --no-build
```
Expected: 0 warnings; the second command prints the usage line with the command list (exit code 1) — or the "API key is not configured" message (exit 2) when no key is configured locally. Both are fine.

- [ ] **Step 3: Commit.**

```pwsh
git add -A
git commit -m "docs(sample): command-driven console sample with DI + resilience example"
```

---

## Task 19: Release verification and hand-off

**Files:** none new.

- [ ] **Step 1: Clean full build, tests, pack.**

```pwsh
git status --short            # must be empty
dotnet clean -c Release | Out-Null
dotnet build -c Release
dotnet test --no-build -c Release
dotnet pack --no-build -c Release -o artifacts/pack
Get-ChildItem artifacts/pack | Select-Object Name
```
Expected: 0 warnings; every test green; 6 `.nupkg` + 6 `.snupkg`.

- [ ] **Step 2: Sanity greps — all must print nothing.**

```pwsh
git grep -n "Official"                    -- src README.md
git grep -n "Mobizon.Contracts.Models"    -- src tests samples/Mobizon.Net.ConsoleSample README.md
git grep -n "HttpMethod.Get"              -- src
git grep -n 'ToString("yyyy'              -- src
git grep -n "mobizon/mobizon-dotnet"      -- .
git grep -n "\.Skip("                     -- src tests README.md
git grep -n "DeleteContactGroupResult\|TaskStatus\b" -- src tests samples/Mobizon.Net.ConsoleSample README.md | Select-String -NotMatch "BackgroundTaskStatus|TaskQueueStatus"
```

- [ ] **Step 3: Inspect one package.**

```pwsh
Expand-Archive artifacts/pack/Mobizon.Net.0*.nupkg -DestinationPath artifacts/pack/inspect -Force
Get-Content artifacts/pack/inspect/Mobizon.Net.nuspec
Get-ChildItem artifacts/pack/inspect/lib -Recurse | Select-Object FullName
Remove-Item -Recurse -Force artifacts/pack
```
Expected: `lib/netstandard2.0/` and `lib/net8.0/` each with `Mobizon.Net.dll` + `Mobizon.Net.xml`; nuspec URLs point at `github.com/gberikov/Mobizon.Net`; description starts with `Unofficial`.

- [ ] **Step 4: Open the PR** `feature/release-readiness` → `develop` with the spec and this plan linked. Merge `develop` → `master` after review.

- [ ] **Step 5: Tag and publish — ONLY after the user explicitly confirms.** Prerequisites: `NUGET_API_KEY` secret exists in the GitHub repo; `CHANGELOG.md` `[0.1.0] - UNRELEASED` changed to the release date and committed.

```pwsh
git checkout master; git pull
git tag -a v0.1.0 -m "Mobizon.Net 0.1.0 — first public release"
git push origin v0.1.0
```
CI builds `0.1.0` and the publish job pushes all six packages (+ symbols) to nuget.org.

---

## Post-release follow-ups (not in this plan)

- Verify `ApiFormat.Gender` casing (`male`/`female`) against the live API with the Playground; flip to upper-case if `contactcard/create` rejects it.
- Consider `IContactCardSet.UpdateAsync` partial-update semantics once the API's handling of omitted fields is confirmed.

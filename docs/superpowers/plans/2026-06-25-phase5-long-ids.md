# Mobizon.Net Phase 5 — Widen entity IDs to `long`

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development. Steps use checkbox (`- [ ]`) syntax.

**Goal:** Fix the final-review Critical: Mobizon entity IDs are 64-bit (the webhook models already use `long`), but the client types them as `int` — `StringToIntConverter` throws on IDs > `int.MaxValue`. Widen all entity IDs to `long`, add a `StringToLongConverter`, and cover large IDs with tests.

**Tech Stack:** C# 8 / `netstandard2.0`; xunit + MockHttp; System.Text.Json.

**Source:** final whole-branch review (opus), 2026-06-25 — C1 (int overflow) + C2 (no large-id test) + doc fixes (M1/M2/I1).

## Global Constraints

- Core targets `netstandard2.0`, C# 8, `Nullable enable`, warning-clean under `TreatWarningsAsErrors`; no third-party deps in core.
- Pre-release: breaking type changes (`int`→`long`) are allowed.
- Widening is purely a type change (`int`→`long`, `int?`→`long?`, `MobizonResponse<int>`→`MobizonResponse<long>`, `int[]`→`long[]`); existing test ID literals will need an `L` suffix where the compiler complains — fix them as build errors surface.
- `StringToLongConverter` must accept BOTH JSON string and number tokens (mirror `StringToIntConverter`).
- Branch `feature/api-contract-refactor`. Commit trailers:
  `Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>` + the `Claude-Session:` trailer.

---

### Task 5.1: Add `StringToLongConverter` and register it

**Files:**
- Create: `src/Mobizon.Net/Internal/Converters/StringToLongConverter.cs`
- Modify: `src/Mobizon.Net/Internal/MobizonApiClient.cs:25-40` (register)
- Test: `tests/Mobizon.Net.Tests/Internal/MobizonApiClientTests.cs` (or a small converter test class)

**Interfaces:** Produces an `internal JsonConverter<long>` registered globally so `long`/`long?` properties parse string-encoded numbers.

- [ ] **Step 1: Write the failing test** (deserialize a `long` from a quoted string bigger than int.MaxValue). Add to a new `tests/Mobizon.Net.Tests/Internal/StringToLongConverterTests.cs`:

```csharp
using System.Text.Json;
using Mobizon.Net.Internal.Converters;
using Xunit;

namespace Mobizon.Net.Tests.Internal
{
    public class StringToLongConverterTests
    {
        private static JsonSerializerOptions Opts()
        {
            var o = new JsonSerializerOptions();
            o.Converters.Add(new StringToLongConverter());
            return o;
        }

        [Fact]
        public void Reads_String_Above_Int32_Max()
            => Assert.Equal(70000000001L, JsonSerializer.Deserialize<long>("\"70000000001\"", Opts()));

        [Fact]
        public void Reads_Number_Token()
            => Assert.Equal(42L, JsonSerializer.Deserialize<long>("42", Opts()));
    }
}
```

(`StringToLongConverter` must be accessible to the test. The existing `StringToIntConverter` is `internal`; if the test project lacks `InternalsVisibleTo`, add `[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Mobizon.Net.Tests")]` in a new `src/Mobizon.Net/Properties/AssemblyInfo.cs`. Check whether `InternalsVisibleTo` already exists first — `BracketNotationSerializerTests` previously used `Mobizon.Net.Internal` types, so it likely does.)

- [ ] **Step 2: Run, verify FAIL.** `dotnet test tests/Mobizon.Net.Tests --filter StringToLongConverter`.

- [ ] **Step 3: Implement** `StringToLongConverter.cs` (mirror `StringToIntConverter` with `long`):

```csharp
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mobizon.Net.Internal.Converters
{
    /// <summary>Handles Mobizon API responses where 64-bit numeric fields are returned as JSON strings (e.g. "70000000001").</summary>
    internal class StringToLongConverter : JsonConverter<long>
    {
        public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    var s = reader.GetString();
                    return long.TryParse(s, out var result)
                        ? result
                        : throw new JsonException($"Cannot convert string \"{s}\" to long.");
                case JsonTokenType.Number:
                    return reader.GetInt64();
                default:
                    throw new JsonException($"Unexpected token type {reader.TokenType} when parsing long.");
            }
        }

        public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
            => writer.WriteNumberValue(value);
    }
}
```

- [ ] **Step 4: Register** in `MobizonApiClient.JsonOptions.Converters` — add `new StringToLongConverter()` right after `new StringToIntConverter()`.
- [ ] **Step 5: Run, verify PASS** + full suite.
- [ ] **Step 6: Commit.** `git add -A && git commit -m "feat: StringToLongConverter for 64-bit Mobizon ids"`

---

### Task 5.2: Widen Message / Campaign / TaskQueue IDs to `long`

**Files (models):** `Models/Messages/SendSmsResult.cs` (`CampaignId`,`MessageId`), `Models/Messages/SmsStatusResult.cs` (`Id`), `Models/Messages/MessageInfo.cs` (`Id`,`CampaignId`), `Models/Campaigns/CampaignData.cs` (`Id`), `Models/Campaigns/CampaignInfo.cs` (`CampaignCounters.CampaignId`), `Models/Campaigns/AddRecipientsResult.cs` (`TaskId` `int?`→`long?`; `AddRecipientEntry.MessageId` `int?`→`long?`, `Contact` `int?`→`long?`), `Models/Campaigns/AddRecipientsRequest.cs` (`CampaignId`), `Models/Campaigns/CampaignCriteria.cs` (`Id`/`Ids` if present), `Models/TaskQueues/TaskQueueStatus.cs` (`Id`).
**Files (interfaces/services):** `ICampaignService`/`CampaignService` — `CreateAsync`→`MobizonResponse<long>`, `SendAsync(long id)`→`MobizonResponse<long>`, `GetAsync(long id)`, `GetInfoAsync(long id, …)`, `DeleteAsync(long id)`; `IMessageService`/`MessageService` — `GetSmsStatusAsync(long id)` + `GetSmsStatusAsync(long[] ids)`; `ITaskQueueService`/`TaskQueueService` — `GetStatusAsync(long id)`.
**Files (converter):** `Internal/Converters/AddRecipientsResultConverter.cs` — the scalar task-id branch must use `reader.TryGetInt64(out var taskId)` and `long.TryParse(reader.GetString(), out …)` (was Int32) so a large task id binds.
**Tests:** update `MessageServiceTests`, `CampaignServiceTests`, `AddRecipientsResultConverterTests`, any `TaskQueue` test; fix ID literals (append `L`) where the compiler requires.

- [ ] **Step 1: Write failing large-id tests** (these fail today because `int` overflows). In `MessageServiceTests`:

```csharp
[Fact]
public async Task SendSmsMessageAsync_Parses_LargeIds()
{
    var mockHttp = new MockHttpMessageHandler();
    mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/Message/SendSmsMessage")
        .Respond("application/json",
            @"{""code"":0,""data"":{""campaignId"":""70000000001"",""messageId"":""70000000002"",""status"":0},""message"":""""}");
    var result = await CreateService(mockHttp).SendSmsMessageAsync(new SendSmsMessageRequest { Recipient = "7700", Text = "x" });
    Assert.Equal(70000000001L, result.Data.CampaignId);
    Assert.Equal(70000000002L, result.Data.MessageId);
}
```

In `CampaignServiceTests`:

```csharp
[Fact]
public async Task CreateAsync_Parses_LargeId()
{
    var mockHttp = new MockHttpMessageHandler();
    mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/Campaign/Create")
        .Respond("application/json", @"{""code"":0,""data"":""70000000003"",""message"":""""}");
    var result = await CreateService(mockHttp).CreateAsync(new CreateCampaignRequest { Type = CampaignType.Bulk, Text = "x" });
    Assert.Equal(70000000003L, result.Data);
}
```

In `AddRecipientsResultConverterTests` add an async-scalar large-task-id case (`"data":70000000004`) asserting `TaskId == 70000000004L`.

- [ ] **Step 2: Run, verify FAIL** (overflow `JsonException`/`MobizonException`, or compile error once you reference `long` members — that's expected; proceed).
- [ ] **Step 3: Widen** every member listed in **Files** above from `int`→`long` (`int?`→`long?`, `int[]`→`long[]`, `MobizonResponse<int>`→`MobizonResponse<long>`). Update the `AddRecipientsResultConverter` scalar branch to Int64. Read each file before editing; change ONLY the ID members (leave counts/flags/status enums alone).
- [ ] **Step 4: Fix compile errors in tests** — append `L` to ID literals the compiler flags (e.g. `Assert.Equal(42, result.Data.MessageId)` → `Assert.Equal(42L, …)`; `GetSmsStatusAsync(new[] { 100, 200 })` → `new[] { 100L, 200L }`).
- [ ] **Step 5: Run, verify PASS** (`dotnet test tests/Mobizon.Net.Tests`) + warning-clean build. Fix any sample call sites that break.
- [ ] **Step 6: Commit.** `git add -A && git commit -m "fix!: widen Message/Campaign/TaskQueue ids to long"`

---

### Task 5.3: Widen Link / ContactCard / ContactGroup / NumberStopList IDs to `long`

**Files (models):** `Models/Links/LinkData.cs` (`Id`), `Models/ContactCards/ContactCard.cs` + `ContactCardData.cs` (`Id` `int?`→`long?`), `Models/ContactGroups/ContactGroupData.cs` (`Id`), `Models/StopLists/StopListEntry.cs` (`Id`).
**Files (interfaces/services):** `ILinkService`/`LinkService` — `GetByIdAsync(long id)`, `DeleteAsync(long[] ids)`, `GetLinksAsync(long campaignId)`; `Models/Links/GetLinkStatsRequest.cs` `Ids` `int[]`→`long[]`; `IContactCardSet`/`ContactCardSet`/`IContactCardQuery` + `ContactCardService` — `FindAsync(long)`, `RemoveAsync(long)`, `SetGroupsAsync(long)`, `GetGroupsAsync(long)` (the internal service already uses `.ToString()` on the id, so `long` flows through unchanged); `IContactGroupService`/`ContactGroupService` — `CreateAsync`→`MobizonResponse<long>`, `UpdateAsync(long id)`, `DeleteAsync(long id)`, `GetCardsCountAsync(long? id)`; `INumberStopListService`/`NumberStopListService` — `AddNumberAsync`→`MobizonResponse<long>`, `DeleteAsync(long id)`.
**Tests:** update `LinkServiceTests`, `ContactCardServiceTests`, `ContactCardQueryTests`, `ContactGroupServiceTests`, `NumberStopListServiceTests`; fix ID literals (`L`).

- [ ] **Step 1: Write a failing large-id test** in `LinkServiceTests` (link IDs are smaller today but the contract must support 64-bit):

```csharp
[Fact]
public async Task GetByIdAsync_Parses_LargeId()
{
    var mockHttp = new MockHttpMessageHandler();
    mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/link/get")
        .WithFormData("id", "70000000005")
        .Respond("application/json", @"{""code"":0,""data"":{""id"":""70000000005"",""code"":""x"",""fullLink"":""https://e.com"",""status"":""1"",""clickCnt"":""0""},""message"":""""}");
    var result = await CreateService(mockHttp).GetByIdAsync(70000000005L);
    Assert.Equal(70000000005L, result.Data.Id);
}
```

- [ ] **Step 2: Run, verify FAIL.**
- [ ] **Step 3: Widen** every member in **Files** above `int`→`long`. Read each file first; change only ID members. The `ContactCardSet` wrappers call `_service.X(id.ToString(), …)` — `long.ToString()` works unchanged.
- [ ] **Step 4: Fix test/sample compile errors** (append `L` to ID literals; `DeleteAsync(new[] { 10, 20 })` → `new[] { 10L, 20L }`; `GetLinkStatsRequest { Ids = new[] { 1, 2 } }` → `new[] { 1L, 2L }`).
- [ ] **Step 5: Run, verify PASS** + warning-clean build (`dotnet build Mobizon.Net.sln -c Debug`). Fix any sample call sites.
- [ ] **Step 6: Commit.** `git add -A && git commit -m "fix!: widen Link/Contact/Group/StopList ids to long"`

---

### Task 5.4: Doc fixes from the final review (CHANGELOG + XML-doc)

**Files:** `CHANGELOG.md`, `src/Mobizon.Contracts/Services/ILinkService.cs` (`GetLinksAsync`), `src/Mobizon.Contracts/Services/IMessageService.cs` (`GetSmsStatusAsync`).

- [ ] **Step 1: Fix CHANGELOG.** (M1) Correct the line that references a non-existent `MobizonResponse<MobizonCreateResponse>` — the old types were `CreateCampaignResult`/`CampaignSendResult`. (M2) Add the missing breaking changes: `ContactCardListResponse`→`ContactCardListResult` rename; new `IMobizonClient.Alphanames` member (breaks hand-rolled implementers); and the new **`int`→`long` id widening** (this phase). Keep the file's existing format.
- [ ] **Step 2: XML-doc the intentional bare-array returns.** (I1) On `ILinkService.GetLinksAsync` and `IMessageService.GetSmsStatusAsync`, add a sentence: the API returns a bare array here (not a paged `MobizonListResult<T>` envelope), so the return type is intentionally `IReadOnlyList<T>`.
- [ ] **Step 3: Build (docs/XML only).** `dotnet build Mobizon.Net.sln -c Debug` — 0 warnings.
- [ ] **Step 4: Commit.** `git add -A && git commit -m "docs: CHANGELOG accuracy + document intentional bare-array returns"`

---

## Self-Review

**Coverage of final-review findings:** C1 (int overflow) → 5.1 (converter) + 5.2/5.3 (widen all ids); C2 (no large-id test) → large-id tests in 5.1/5.2/5.3; M1/M2 (CHANGELOG) → 5.4; I1 (bare-array docs) → 5.4. I2 (module casing) is a documented conscious non-goal (not reopened). M3/M4 were informational (no action).

**Placeholder scan:** converter + tests have complete code; the widening tasks list exact members and the change is a mechanical `int`→`long` — actionable, not a TODO.

**Type consistency:** `StringToLongConverter` registered alongside `StringToIntConverter`; `MobizonListResult<T>.TotalItemCount` stays `int` (small counts, no overflow). `AddRecipientsResultConverter` updated to Int64 to match the widened `TaskId`. All ID params/returns/fields consistently `long` after 5.2/5.3.

**Risk:** mechanical but broad; the existing exact-form-data + new large-id tests are the safety net. Build errors guide the test-literal fixes.

---

## Execution Handoff

Execute via subagent-driven development, Tasks 5.1→5.4. After 5.4, re-run the final whole-branch review (focused on the widening) and then superpowers:finishing-a-development-branch for the merge decision.

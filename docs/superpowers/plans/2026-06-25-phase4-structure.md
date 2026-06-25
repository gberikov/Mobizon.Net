# Mobizon.Net Phase 4 — Structure / Naming Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development. Steps use checkbox (`- [ ]`) syntax.

**Goal:** Final low-risk cleanup: remove dead code, centralize the enum→API-code mapping, fix the `ContactCardQuery.Where` footgun, document the `MobizonResponse.Data` contract, and add a CHANGELOG for the 1.0 breaking changes.

**Tech Stack:** C# 8 / `netstandard2.0`; xunit + MockHttp.

**Spec:** `docs/superpowers/specs/2026-06-24-mobizon-net-refactor-design.md` (§7 Phase 4). **Decision:** `BracketNotationSerializer` is to be **deleted** (user decision 2026-06-25 — adoption value turned out modest; it only flattens a pre-built dict).

## Global Constraints

- Core targets `netstandard2.0`, C# 8, `Nullable enable`, warning-clean under `TreatWarningsAsErrors`; no third-party deps in core.
- Pure cleanup phase: NO wire-format changes; existing form-data tests must stay green unchanged.
- Branch `feature/api-contract-refactor`. Commit trailers:
  `Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>` + the `Claude-Session:` trailer.

---

## File Structure (Phase 4)

- Delete: `src/Mobizon.Net/Internal/BracketNotationSerializer.cs`, `tests/Mobizon.Net.Tests/Internal/BracketNotationSerializerTests.cs`.
- Create: `src/Mobizon.Net/Internal/Converters/ApiStatusCodes.cs` (centralized enum→code).
- Modify: `src/Mobizon.Net/Services/MessageService.cs` (use `ApiStatusCodes`), `src/Mobizon.Net/ContactCards/ContactCardQuery.cs` (combine `Where`), `src/Mobizon.Contracts/Models/Common/MobizonResponse.cs` (doc), `CHANGELOG.md`.
- Tests: `tests/Mobizon.Net.Tests/Services/ContactCardQueryTests.cs`.

---

### Task 4.1: Delete the dead `BracketNotationSerializer`

**Files:**
- Delete: `src/Mobizon.Net/Internal/BracketNotationSerializer.cs`, `tests/Mobizon.Net.Tests/Internal/BracketNotationSerializerTests.cs`

**Interfaces:** none (removal of an unused internal type).

- [ ] **Step 1: Confirm it is unused** in production code. Run: `rg -n "BracketNotationSerializer" src` — Expected: only the definition file (no usages). If any `src` usage exists, STOP and report BLOCKED (the delete decision assumed it is dead).
- [ ] **Step 2: Delete both files.** `git rm src/Mobizon.Net/Internal/BracketNotationSerializer.cs tests/Mobizon.Net.Tests/Internal/BracketNotationSerializerTests.cs`
- [ ] **Step 3: Build + test.** `dotnet build Mobizon.Net.sln -c Debug` then `dotnet test tests/Mobizon.Net.Tests`. Expected: 0 warnings; suite green (minus the ~11 removed serializer tests).
- [ ] **Step 4: Commit.** `git commit -m "refactor: remove unused BracketNotationSerializer and its tests"`

---

### Task 4.2: Centralize the enum→API-code mapping

**Files:**
- Create: `src/Mobizon.Net/Internal/Converters/ApiStatusCodes.cs`
- Modify: `src/Mobizon.Net/Services/MessageService.cs:169-198` (remove the two private methods; call the shared mapper)
- Test: existing `MessageServiceTests.ListAsync_*` already assert the produced codes (e.g. `criteria[status]=DELIVRD`) — they guard this move; no wire change.

**Interfaces:**
- Produces: `internal static class ApiStatusCodes { public static string ToApiCode(SmsStatus status); public static string ToApiCode(CampaignCommonStatus status); }` — byte-identical to the current `MessageService` switch outputs.

- [ ] **Step 1: Create `ApiStatusCodes.cs`** by moving the exact switch bodies from `MessageService.SmsStatusToApiCode` and `CampaignCommonStatusToApiCode` (keep the EXACT string codes incl. `ENQUEUD`/`ACCEPTD`/`DELIVRD` etc. — they are the real Mobizon DLR codes, not typos):

```csharp
using System;
using Mobizon.Contracts.Models.Campaigns;
using Mobizon.Contracts.Models.Messages;

namespace Mobizon.Net.Internal.Converters
{
    /// <summary>Maps strongly-typed status enums to the wire codes the Mobizon API expects on input.</summary>
    internal static class ApiStatusCodes
    {
        public static string ToApiCode(SmsStatus status)
        {
            switch (status)
            {
                case SmsStatus.New:         return "NEW";
                case SmsStatus.Enqueued:    return "ENQUEUD";
                case SmsStatus.Accepted:    return "ACCEPTD";
                case SmsStatus.Delivered:   return "DELIVRD";
                case SmsStatus.Undelivered: return "UNDELIV";
                case SmsStatus.Rejected:    return "REJECTD";
                case SmsStatus.Expired:     return "EXPIRD";
                case SmsStatus.Deleted:     return "DELETED";
                default: throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }

        public static string ToApiCode(CampaignCommonStatus status)
        {
            switch (status)
            {
                case CampaignCommonStatus.Moderation:       return "MODERATION";
                case CampaignCommonStatus.Declined:         return "DECLINED";
                case CampaignCommonStatus.ReadyForSend:     return "READY_FOR_SEND";
                case CampaignCommonStatus.AutoReadyForSend: return "AUTO_READY_FOR_SEND";
                case CampaignCommonStatus.Running:          return "RUNNING";
                case CampaignCommonStatus.Sent:             return "SENT";
                case CampaignCommonStatus.Done:             return "DONE";
                default: throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }
    }
}
```

- [ ] **Step 2: Update `MessageService`.** Delete the two private static methods; replace the two call sites (`SmsStatusToApiCode(c.Status.Value)` → `ApiStatusCodes.ToApiCode(c.Status.Value)`; `CampaignCommonStatusToApiCode(c.CampaignStatus.Value)` → `ApiStatusCodes.ToApiCode(c.CampaignStatus.Value)`). Add `using Mobizon.Net.Internal.Converters;` if needed.
- [ ] **Step 3: Build + test.** `dotnet test tests/Mobizon.Net.Tests --filter MessageServiceTests` then the full suite. Expected: green (the existing criteria tests prove the codes are unchanged).
- [ ] **Step 4: Commit.** `git commit -am "refactor: centralize enum->API-code mapping in ApiStatusCodes"`

---

### Task 4.3: Fix `ContactCardQuery.Where` (combine predicates) + document `Skip` & `Data`

**Files:**
- Modify: `src/Mobizon.Net/ContactCards/ContactCardQuery.cs:35-39` (combine), `src/Mobizon.Contracts/Models/Common/MobizonResponse.cs` (doc)
- Test: `tests/Mobizon.Net.Tests/Services/ContactCardQueryTests.cs`

**Interfaces:**
- Produces: chained `Where(a).Where(b)` filters by `a AND b` (instead of the second overwriting the first).

- [ ] **Step 1: Verify the parser supports `&&`.** Read `src/Mobizon.Net/Internal/ContactCardExpressionParser.cs`. Confirm a single predicate using `&&` (e.g. `x => x.GroupId == 100 && x.Email == "a@b.c"`) already parses into multiple criteria (look for `ExpressionType.AndAlso` handling). 
  - If AndAlso IS supported → proceed to Step 2 (combine).
  - If AndAlso is NOT supported → do NOT attempt the combine (out of scope risk). Instead, document on `IContactCardSet.Where`/`ContactCardQuery.Where` that only a single `Where` is supported (a second call replaces the first) and STOP after updating that doc + Step 4/5 (Skip/Data docs) + commit. Report which path you took.

- [ ] **Step 2 (if AndAlso supported): Write the failing test** in `ContactCardQueryTests.cs` (follow the file's existing setup for building a query + capturing the outgoing request):

```csharp
[Fact]
public async Task Where_CalledTwice_CombinesWithAnd()
{
    // Arrange a query with two Where calls; assert BOTH criteria are sent.
    // (Use the test file's existing MockHttp/service harness pattern.)
    // e.g. client.ContactCards.Where(x => x.GroupId == 100).Where(x => x.Surname == "Doe")
    // should encode criteria for BOTH GroupId and Surname.
}
```

Fill the body using the harness already in `ContactCardQueryTests.cs` (read it first); assert the request contains the criteria from BOTH predicates.

- [ ] **Step 3 (if AndAlso supported): Implement the combine** in `ContactCardQuery.Where`:

```csharp
public IContactCardQuery Where(Expression<Func<ContactCardFilterSpec, bool>> predicate)
{
    if (predicate == null) throw new ArgumentNullException(nameof(predicate));
    _predicate = _predicate == null ? predicate : CombineAnd(_predicate, predicate);
    return this;
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
```

(`using System.Linq.Expressions;` is already imported.)

- [ ] **Step 4: Document `Skip`.** Ensure the XML-doc on `ContactCardQuery.Skip` (and `IContactCardSet.Skip`) states it is page-based: the value is translated to `currentPage = Skip / pageSize`, so it is accurate only at page-size multiples and requires `Take` for predictable paging. (The existing comment on `ContactCardQuery.Skip` already says this — confirm/keep.)

- [ ] **Step 5: Document `MobizonResponse.Data`.** Update the XML-doc on `MobizonResponse<T>.Data` to state the contract clearly: for endpoints that return a payload, `Data` is populated on success (`Code == Success`/`BackgroundTask`); for value-type `T` or no-payload endpoints it may be the default. (Documentation only — do NOT add a runtime guard, since `null`/default is legitimately valid for some endpoints.)

- [ ] **Step 6: Build + test.** `dotnet test tests/Mobizon.Net.Tests`. Expected: green.
- [ ] **Step 7: Commit.** `git commit -am "fix: ContactCardQuery.Where combines predicates; document Skip and Data contracts"`

---

### Task 4.4: CHANGELOG for the 1.0 breaking changes

**Files:**
- Modify: `CHANGELOG.md`

**Interfaces:** none (docs).

- [ ] **Step 1: Read `CHANGELOG.md`** to match its existing format.
- [ ] **Step 2: Add an entry** (under an `Unreleased` or `1.0.0` heading, matching the file's style) summarizing the API-contract refactor. Group as **Breaking**, **Added**, **Fixed**:
  - **Breaking:** list endpoints now return `MobizonResponse<MobizonListResult<T>>` (Campaign/Link/Message/ContactCard list); `Campaign.CreateAsync`/`SendAsync` return `MobizonResponse<int>`; `Link.GetAsync(code)` replaced by `GetByIdAsync`/`GetByCodeAsync`/`GetByShortLinkAsync`; `UpdateLinkRequest` keyed by `Id` (no `Code`/`FullLink`); `LinkData.Clicks` → `ClickCnt`; `Link.GetStatsAsync` returns `LinkStatsResult { Items, Totals }` (old single-point type renamed `LinkStatPoint`); `MessageInfo.SegUserBuy` is now `decimal`; `IMobizonClient` registered transient (was singleton).
  - **Added:** `alphaname` module (`client.Alphanames`); `AddRecipients` file upload + single-source validation; `Campaign/Create` `shortenLinks`; `Link/List` criteria; `LinkStatsType` `Hourly`/`Minute`; webhook endpoint body-size cap + JSON problem responses; tag-driven versioning (MinVer).
  - **Fixed:** `campaign/addRecipients` array|scalar deserialization; `LinkData.clickCnt` mapping; injected `HttpClient.Timeout` no longer mutated; README corrected; CI triggers on `master`/`develop`.
- [ ] **Step 3: Commit.** `git commit -am "docs: add CHANGELOG entry for the API-contract & DX refactor"`

---

## Self-Review

**Spec coverage (§7 Phase 4):** BracketNotationSerializer → **deleted** (4.1, per user decision); enum↔code mapping consolidation → 4.2; `ContactCardQuery.Where` → 4.3; `Skip`/`Data` documentation → 4.3; CHANGELOG → 4.4. **Module-name casing** (`"link"` vs `"Link"`) is **intentionally skipped**: the Mobizon API is case-insensitive on module/method, so normalizing is cosmetic and would only churn the URL assertions in `LinkServiceTests` for no behavioral gain — recorded here as a conscious non-goal.

**Placeholder scan:** 4.1/4.2/4.4 have complete content. 4.3 has a verified decision branch (parser supports AndAlso or not) with concrete code for the supported path and a concrete doc-only fallback — not an open TODO.

**Type consistency:** `ApiStatusCodes.ToApiCode` overloads match the deleted private methods' signatures/outputs (4.2). `CombineAnd`/`ReplaceParam` are self-contained (4.3).

**Risk:** lowest of all phases — no wire-format changes; existing form-data tests guard 4.2; 4.1 only removes unused code; 4.3 adds behavior (combine) guarded by a new test or falls back to docs.

---

## Execution Handoff

Execute via subagent-driven development, Tasks 4.1→4.4. After 4.4, run the **final whole-branch review** (superpowers:requesting-code-review) over `master..HEAD`, then superpowers:finishing-a-development-branch to decide merge/PR.

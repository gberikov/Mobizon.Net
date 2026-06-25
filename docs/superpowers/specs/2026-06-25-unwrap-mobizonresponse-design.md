# Design: Unwrap `MobizonResponse<T>` from the public service surface

**Date:** 2026-06-25
**Status:** Approved (pending spec review)
**Type:** Breaking API redesign (pre-1.0, no external consumers — breaking allowed)

## Problem

Every service method returns `Task<MobizonResponse<T>>`. But `MobizonApiClient.SendCoreAsync`
already throws `MobizonApiException` on any non-success response code (see
`src/Mobizon.Net/Internal/MobizonApiClient.cs:173-178`). By the time the wrapper reaches the
caller:

- `RawCode` is always `Success (0)`, `BackgroundTask (100)`, or an explicit extra-success code.
- `Message` is always empty (the error text already went into the exception).
- `Data` is the only field carrying real information.

So on the happy path the wrapper is pure noise — the developer writes `.Data` everywhere for no
gain. The only places the response code carries meaning beyond "success" are **two** campaign
methods, and even there the meaning is derivable from / foldable into the payload:

- `CampaignService.SendAsync` — `Code == BackgroundTask (100)` means the `long` is a queued task id
  rather than a synchronous result.
- `CampaignService.AddRecipientsAsync` — the only use of `extraSuccessCodes` in the codebase
  (`CampaignService.cs:331`), codes `98 PartiallyAdded` / `99 NoneAdded`. These are derivable from
  the per-recipient `AddRecipientEntry.Code` values already in the payload.

There is already a precedent for unwrapping in the codebase: the fluent contact-card API
(`ContactCardSet` / `ContactCardQuery`) returns domain types directly (e.g.
`ToListAsync → IReadOnlyList<ContactCard>`), unwrapping the response internally. This design
finishes that direction across the whole SDK.

## Goal

Remove `MobizonResponse<T>` from the public service surface. Services return domain types directly.
Errors continue to surface as `MobizonApiException`. The two code-aware campaign methods fold their
meaningful code into domain types.

## Architecture

`MobizonResponse<T>` moves to `Mobizon.Net/Internal` and becomes `internal`. It remains the
deserialization target and the place where `MobizonApiClient` decides whether to throw or return the
payload. It is no longer referenced by any public signature.

`MobizonResponseCode` **stays public** — it is exposed by `MobizonApiException.Code` and is the
correct place for a caller to inspect the failure reason inside a `catch`.

Error semantics are unchanged: a non-success code → `MobizonApiException`; `BackgroundTask (100)` and
AddRecipients `98/99` remain non-errors. The only change is that those codes are now read from domain
fields (`CampaignSendResult.IsQueued`, `AddRecipientsResult.Outcome`) instead of the wrapper.

## Method categories

### Category A — unwrap to `T` (data-bearing methods)

`Task<MobizonResponse<T>>` → `Task<T>`, same `T`:

| Service | Methods |
|---|---|
| Link | `CreateAsync→LinkData`, `GetByIdAsync`/`GetByCodeAsync`/`GetByShortLinkAsync→LinkData`, `GetLinksAsync→IReadOnlyList<LinkData>`, `GetStatsAsync→LinkStatsResult`, `ListAsync→MobizonListResult<LinkData>` |
| Campaign | `CreateAsync→long`, `GetAsync→CampaignData`, `GetInfoAsync→CampaignInfo`, `ListAsync→MobizonListResult<CampaignData>` |
| Alphaname | `ListAsync→MobizonListResult<AlphanameData>` |
| NumberStopList | `ListAsync→StopListListResponse`, `AddNumberAsync→long` |
| TaskQueue | `GetStatusAsync→TaskQueueStatus` |
| ContactGroup | `ListAsync→ContactGroupListResponse`, `CreateAsync→long`, `DeleteAsync→DeleteContactGroupResult`, `GetCardsCountAsync→long` |
| User | `GetOwnBalanceAsync→BalanceResult` |
| Message | `QuickSendAsync→SendSmsResult`, `SendSmsMessageAsync→SendSmsResult`, `GetSmsStatusAsync→IReadOnlyList<SmsStatusResult>` (both overloads), `ListAsync→MobizonListResult<MessageInfo>` |
| ContactCard (service) | `ListAsync→ContactCardListResult`, `GetAsync→ContactCardData`, `CreateAsync→string`, `GetGroupsAsync→IReadOnlyList<ContactGroupRef>` |

`DeleteContactGroupResult` carries `Processed` / `NotProcessed` ID lists — it is meaningful and stays
`Task<DeleteContactGroupResult>` (NOT void).

### Category B — unwrap to `Task` (void; success = no exception)

Methods whose `Data` is `object` or an always-`true` `bool`:

- `Link.DeleteAsync`, `Link.UpdateAsync`
- `Campaign.DeleteAsync`
- `NumberStopList.AddNumberRangeAsync`, `NumberStopList.DeleteAsync`
- `ContactGroup.UpdateAsync`
- `ContactCard.UpdateAsync`, `ContactCard.SetGroupsAsync`, `ContactCard.RemoveAsync`

### Category C — fold code into a domain type (2 campaign methods)

**`Campaign.SendAsync`** → `Task<CampaignSendResult>`:

```csharp
public class CampaignSendResult
{
    /// <summary>True when the API queued the send as a background task (response code 100).</summary>
    public bool IsQueued { get; set; }

    /// <summary>The background task id when <see cref="IsQueued"/>; otherwise the synchronous send result.</summary>
    public long Id { get; set; }
}
```

`IsQueued` is set from whether the response code was `BackgroundTask (100)`.

**`Campaign.AddRecipientsAsync`** → stays `Task<AddRecipientsResult>`, with a new property:

```csharp
public AddRecipientsOutcome Outcome { get; set; }  // AllAdded | PartiallyAdded | NoneAdded
```

`Outcome` is populated from the top-level response code (`0` / `98` / `99`). This moves
`AddRecipientsResponseCode` into the payload and makes the `extraSuccessCodes` mechanism a purely
internal detail of `MobizonApiClient` — no public method exposes it. The batch-aggregation logic in
`CampaignService` (worst-case code across batches, `CampaignService.cs:289-291`) maps onto `Outcome`.

## Components touched

- **`src/Mobizon.Contracts/Services/*.cs`** — all 9 service interfaces: change return types.
- **`src/Mobizon.Net/Services/*.cs`** + **`ContactCardService.cs`** — implementations unwrap `.Data`
  (or compute `IsQueued` / `Outcome`) before returning.
- **`MobizonResponse<T>`** — move from `src/Mobizon.Contracts/Models/Common/` to
  `src/Mobizon.Net/Internal/` as `internal`. Update `MobizonApiClient` and
  `ContactCards/ContactCardQuery.cs` (the only internal consumers).
- **New types** in `src/Mobizon.Contracts/Models/Campaigns/`: `CampaignSendResult`,
  `AddRecipientsOutcome` enum. Add `Outcome` to `AddRecipientsResult`.
- **Tests** — remove ~50 `Assert.Equal(MobizonResponseCode.Success, result.Code)` assertions (success
  is now expressed by absence of an exception); rewrite `result.Data.X` → `result.X`; add tests for
  `IsQueued` and `Outcome`.
- **Samples** — `CampaignSamples` branch on `sendResult.Code == BackgroundTask` → `sendResult.IsQueued`.
- **README** — keep the response-code table (still relevant for `MobizonApiException.Code`); drop
  `.Data` from examples.

## Out of scope

- `MobizonResponseCode`, `MobizonApiException`, `MobizonListResult<T>`, `PaginatedResponse<T>`, JSON
  converters, webhooks.
- The fluent contact-card API (`ContactCardSet` / `ContactCardQuery`) — already unwrapped; only
  verified for consistency.

## Testing strategy

- Service-level: each method asserts the returned domain object directly; error path asserts
  `MobizonApiException` with the expected `Code`.
- `CampaignSendResult`: one test for the sync path (`IsQueued == false`) and one for the queued path
  (`IsQueued == true`, `Id` = task id) using `link.getStats`-style payload fixtures.
- `AddRecipientsResult.Outcome`: tests for code `0` (AllAdded), `98` (PartiallyAdded), `99` (NoneAdded),
  including the multi-batch worst-case aggregation.
- `MobizonResponse<T>` internal deserialization tests remain (now testing the internal type).

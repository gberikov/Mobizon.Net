# Design: Naming & Types Refactor

**Date:** 2026-06-25
**Status:** Approved (pending spec review)
**Type:** Breaking refactor of public model surface (pre-1.0, breaking allowed)
**Branch:** `feature/naming-and-types-refactor` (off `develop`)

## Problem

Several public entity properties carry awkward API-transliterated names and weak types:
- Timestamps arrive as `string` (`CreateTs`, `UpdateTs`) instead of `DateTime?`.
- Abbreviations (`ClickCnt`, `RedirectCnt`, `SegUserBuy`, `SegNum`) read poorly in .NET.
- Fixed-domain `int`/`string` fields (link status, placeholder mode, boolean flags) would be friendlier as enums/bools.
- `AddRecipientsResponseCode` and `AddRecipientsOutcome` are literal duplicates (both `AllAdded=0, PartiallyAdded=98, NoneAdded=99`) — the latter was introduced during the unwrap refactor without noticing the former.

The converter infrastructure already exists and is registered globally in `MobizonApiClient.JsonOptions`: `MobizonDateTimeConverter` (`DateTime?` ↔ `yyyy-MM-dd HH:mm:ss`), `StringToNumericEnumConverter<T>`, `StringToBoolConverter`, and per-status write mapping via `ApiStatusCodes`. Most of this refactor leverages that infrastructure rather than adding new machinery.

## Goal

Rename API-dictated properties to idiomatic .NET names, strengthen weak types (string→DateTime, int→enum, int→bool) where the value domain is known, and remove the duplicate enum — without changing wire behavior.

## API value domains (confirmed from Mobizon API docs)

- Link `status`: `0` = inactive, `1` = active.
- Link `moderatorStatus`: `0` = blocked by admin, `1` = approved by admin.
- AddRecipients `params[placeholdersFlag]`: `1` = keep placeholders as-is (default), `2` = remove, `3` = reject message.
- AddRecipients `params[replace]`: `0` = append (default), `1` = replace.
- AddRecipients `params[recipientsFileSkipHeader]`: `0`/`1`.
- Alphaname `globalStatus`/`partnerStatus`/`type`: **value domain not documented/accessible** → left as `int`.
- Contact-card `gender`: **representation not confirmed** (no doc page, no fixture) → left as `string?`.

## Block 1 — Timestamps: string/`...Ts` → `DateTime?` + meaningful names

### Read-side entities
The global `MobizonDateTimeConverter` already handles any `DateTime?` property. Change the property type and name; keep the existing `[JsonPropertyName]` so the wire mapping is unchanged.

| File | Was | Becomes | JsonPropertyName |
|---|---|---|---|
| `src/Mobizon.Contracts/Models/Links/LinkData.cs` | `string? CreateTs` | `DateTime? Created` | `createTs` |
| `src/Mobizon.Contracts/Models/Links/LinkData.cs` | `string? UpdateTs` | `DateTime? Updated` | `updateTs` |
| `src/Mobizon.Contracts/Models/Alphanames/AlphanameData.cs` | `string? CreateTs` | `DateTime? Created` | `createTs` |
| `src/Mobizon.Contracts/Models/Alphanames/AlphanameInfo.cs` | `string? CreateTs` | `DateTime? Created` | `createTs` |
| `src/Mobizon.Contracts/Models/Campaigns/CampaignInfo.cs` | `DateTime? UpdateTs` | `DateTime? Updated` (rename only) | (unchanged) |

### Write-side filters
Change type to `DateTime?`; the owning service formats the value as `yyyy-MM-dd HH:mm:ss` when building the request parameter dictionary (these are form-encoded params, not JSON, so the converter does not apply — the formatting is explicit in service code).

| File | Was | Becomes |
|---|---|---|
| `CampaignListRequest`/`CampaignCriteria` | `string? CreateTsFrom`, `CreateTsTo` | `DateTime? CreatedFrom`, `CreatedTo` |
| `CampaignListRequest`/`CampaignCriteria` | `string? SentTsFrom`, `SentTsTo` | `DateTime? SentFrom`, `SentTo` |
| `src/Mobizon.Contracts/Models/Links/LinkListCriteria.cs` | `string? CreateTsFrom`, `CreateTsTo` | `DateTime? CreatedFrom`, `CreatedTo` |

`CampaignService.ListAsync` and `LinkService.ListAsync` format these with `value?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)`.

## Block 2 — Cryptic names → idiomatic (keep `[JsonPropertyName]`)

| File | Was | Becomes | JsonPropertyName |
|---|---|---|---|
| `LinkData.cs` | `int ClickCnt` | `int Clicks` | `clickCnt` |
| `LinkData.cs` | `int RedirectCnt` | `int Redirects` | `redirectCnt` |
| `LinkListCriteria.cs` | `int? ClickCntFrom`, `ClickCntTo` | `int? ClicksFrom`, `ClicksTo` | (request params) |
| `src/Mobizon.Contracts/Models/Messages/MessageInfo.cs` | `decimal SegUserBuy` | `decimal SegmentCost` | `segUserBuy` |
| `src/Mobizon.Contracts/Models/Webhooks/SmsDeliveryReport.cs` | `int SegNum` | `int Segments` | **add `[JsonPropertyName("segNum")]`** — this property currently has no explicit mapping, so its C# name IS the wire key; renaming without the attribute would break webhook deserialization. Verify the webhook serializer options before/after and keep an existing webhook test covering this field. |

## Block 3 — New enums (values confirmed)

New enum files under `src/Mobizon.Contracts/Models/Links/` and `.../Campaigns/`:

```csharp
public enum LinkStatus { Inactive = 0, Active = 1 }
public enum LinkModeratorStatus { Blocked = 0, Approved = 1 }
public enum PlaceholderMissingMode { KeepAsIs = 1, Remove = 2, Reject = 3 }
```

Wiring:
- `LinkData.Status` (`int` → `LinkStatus`), `LinkData.ModeratorStatus` (`int` → `LinkModeratorStatus`) — read side. Register `StringToNumericEnumConverter<LinkStatus>` and `<LinkModeratorStatus>` globally in `MobizonApiClient.JsonOptions` (the API may send these as `int` or numeric string; that converter handles both).
- `CreateLinkRequest.Status`, `UpdateLinkRequest.Status`, `LinkListCriteria.Status` (`int?` → `LinkStatus?`) — write side; service writes `((int)value).ToString()`.
- `LinkListCriteria.ModeratorStatus` (`int?` → `LinkModeratorStatus?`) — write side.
- `AddRecipientsParameters.PlaceholdersFlag` (`int?` → `PlaceholderMissingMode?`) — write side; `CampaignService` writes `((int)value).ToString()`.

## Block 4 — Wire an existing enum

- `CampaignListRequest`/`CampaignCriteria.Status`: `string?` → `CampaignCommonStatus?`. `CampaignService.ListAsync` maps it with the existing `ApiStatusCodes.ToApiCode(CampaignCommonStatus)`.
- **Caveat:** the API filter also accepts the search-only pseudo-status `NOT_YET_SENT`, which `CampaignCommonStatus` does not model. That filter option is dropped. Accepted as a known limitation.

## Block 5 — int → bool

- `AlphanameData.IsDefault` (`int` → `bool`) — read side; the global `StringToBoolConverter` already coerces `0`/`1`. Type change only.
- `AddRecipientsParameters.Replace` (`int?` → `bool?`) — write side; `CampaignService` writes `value == true ? "1" : "0"`.
- `AddRecipientsParameters.RecipientsFileSkipHeader` (`int?` → `bool?`) — write side; same `"1"/"0"` formatting.

## Block 6 — Enum dedup

- Delete `src/Mobizon.Contracts/Models/Campaigns/AddRecipientsResponseCode.cs`.
- Keep `AddRecipientsOutcome`.
- The only consumer, `CampaignService.SendAddRecipientsAsync` (`extraSuccessCodes: new[] { (int)AddRecipientsResponseCode.PartiallyAdded, (int)AddRecipientsResponseCode.NoneAdded }`), switches to `(int)AddRecipientsOutcome.PartiallyAdded` / `.NoneAdded` (same values 98/99).

## Block 7 — Explicitly out of scope

- `AlphanameData.GlobalStatus`, `AlphanameData.PartnerStatus`, `AlphanameInfo.Type` — stay `int` (value domain not found).
- `Gender` — `ContactCard.Gender`, `ContactCardFields.Gender`, `CreateContactCardRequest.Gender`, `UpdateContactCardRequest.Gender` stay `string?` (API representation unconfirmed). The dead `ContactCardFilterSpec.Gender => null` stub and the `Gender` enum are left untouched this pass.
- `LinkData.ExpirationDate` — date-only (`YYYY-MM-DD`), name already idiomatic → stays `string?` (the datetime converter expects a time component and would fail on a date-only value).

## Components touched

- **Models** (`src/Mobizon.Contracts/Models/**`): property renames/retypes per Blocks 1–6; new enum files (Block 3); deleted enum (Block 6).
- **Converters** (`src/Mobizon.Net/Internal/Converters/`): register `StringToNumericEnumConverter<LinkStatus>` and `<LinkModeratorStatus>` in `MobizonApiClient.JsonOptions`. No new converter classes are required (Gender deferred).
- **Services** (`src/Mobizon.Net/Services/CampaignService.cs`, `LinkService.cs`): update request-parameter building for the renamed/retyped write-side filters and params — datetime formatting, enum→int/string, bool→`"1"/"0"`.
- **Tests**: update existing fixture/unit tests for new names/types; add tests for the new read converters (`LinkStatus`/`LinkModeratorStatus`) and for write-side formatting (datetime filters, enum/bool params).
- **Samples / README / CHANGELOG**: update any references to the renamed members; add a Breaking entry.

## Error handling / wire behavior

No wire behavior changes. `[JsonPropertyName]` attributes preserve the JSON field mapping for every renamed property. Read-side type changes are absorbed by already-registered global converters. Write-side type changes are formatted explicitly in service code to emit the exact same strings the API received before. Non-success response codes still throw `MobizonApiException`.

## Testing strategy

- **Read side**: deserialize a fixture/JSON payload and assert the new property name + type (e.g. `LinkData.Created` is a `DateTime`, `LinkData.Status == LinkStatus.Active`, `LinkData.Clicks`, `AlphanameData.IsDefault` is `bool`).
- **Write side**: invoke the service with the new typed filter/param and assert (via `MockHttp` `WithFormData`) that the emitted form field matches the prior wire string (`criteria[createTsFrom]` = `2026-01-02 03:04:05`; `criteria[status]` = `READY_FOR_SEND`; `params[placeholdersFlag]` = `2`; `params[replace]` = `1`).
- **Dedup**: confirm `AddRecipientsResponseCode` is gone and the AddRecipients partial/none-added paths still resolve to `AddRecipientsOutcome.PartiallyAdded`/`.NoneAdded`.
- Full suite stays green; build stays at 0 warnings.

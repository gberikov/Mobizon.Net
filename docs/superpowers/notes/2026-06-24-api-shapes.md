# Mobizon API — captured response shapes (2026-06-24)

Source: live capture via `tools/Mobizon.Net.ApiCapture` against `api.mobizon.kz`,
sanitized fixtures in `tests/Mobizon.Net.Tests/Payloads/`. This file is the ground
truth for Phases 1–4.

**Conventions**
- Envelope is always `{ "code": int, "data": <varies>, "message": string }`.
- Numbers are frequently returned as JSON **strings** (`"0"`, `"400145"`, `"16.2000"`)
  → rely on the registered `StringToInt/Float/Decimal` converters; `MobizonListResult.TotalItemCount`
  must convert from string.
- `7000000XXXX` / `0.0000` in fixtures are sanitizer placeholders (real digits/balance).

## Per-endpoint shapes & SDK deltas

| Endpoint | `data` shape (real) | SDK delta |
|----------|---------------------|-----------|
| `user/getOwnBalance` | `{ balance:str, currency:str }` | OK (`BalanceResult`) |
| `message/list` | `{ items:[], totalItemCount:str }` | already `MessageListResponse`; move to `MobizonListResult<MessageInfo>`; `TotalItemCount` from string |
| `message/sendSmsMessage` | `{ campaignId:str, messageId:str, status:int }` | **OK** — `SendSmsResult` fields match (str→int via converter; `status` int→`CampaignStatus`) |
| `message/getSMSStatus` | `[ { id:str, status:"ACCEPTD"/"DELIVRD", segNum:str, startSendTs, statusUpdateTs } ]` (bare array) | **OK** — `SmsStatusResult` matches (`segNum`→`Segments`, DLR string→`SmsStatus`) |
| `campaign/list` | `{ items:[], totalItemCount:str }` | **FIX**: bare-array → `MobizonListResult<CampaignData>` |
| `campaign/create` | **bare string id** `"123456"` | **BUG**: `CreateCampaignResult` (object) can't bind → return `MobizonResponse<int>` |
| `campaign/send` | **bare int** `2` (sync); task id when `code==100` | **BUG**: `CampaignSendResult` (object) can't bind → return `MobizonResponse<int>` |
| `campaign/addRecipients` | sync: **bare array** `[ { recipient, code:int, messageId, type:"number", number } ]`; async: scalar task id (`code==100`) | **BUG**: `AddRecipientsResult` (object `{TaskId,Entries}`) binds neither → custom converter (array→`Entries`, scalar→`TaskId`); add `Type` to entry |
| `campaign/getInfo` | rich object: `id,userId,type,msgType,commonStatus,moderationStatus,partnerModStatus,globalModStatus,sendStatus,creationWay,createTs,startSendTs,endSendTs,expirationTs,name,from,text, extra{validity,mclass,isTestAlphanameUsed,coding,charset,trackShortLinkRecipients}, groups, counters{...} }` | `CampaignInfo`/`CampaignCounters` model is **solid**; audit deltas: add `Counters.UserCurrency`, `Counters.CampaignId`; verify `extra{}` and the status-enum fields map. (README's flat `Sent/Delivered/Failed` is fiction.) |
| `campaign/delete` | `bool true` | OK as `object`; optionally type as `bool` |
| `link/list` | `{ items:[ <link> ], totalItemCount:str }` | **FIX**: bare-array → `MobizonListResult<LinkData>` |
| `link/get` | `<link>` (full object) | OK shape; expand `LinkData` (below) |
| `link/create` | `<link>` (full object) | OK |
| `link/update` | `bool true` | OK as `object`; optionally `bool` |
| `link/delete` | `{ processed:[str], notProcessed:[str] }` | currently untyped `object`; optionally model `LinkDeleteResult` |
| `link/getStats` | **NOT CAPTURED** — tool sent invalid `type=day` → code 12. Docs: `{ items:[...], totals:str }`. Valid `type`: `monthly\|daily\|hourly\|minute`; `ids` max **5** | **FIX**: wrapper `{ Items, Totals }`; `LinkStatsType` add `Hourly`,`Minute`; per-item shape **still to capture** (re-run with `type=daily`) |
| `taskqueue/getStatus` | **NOT CAPTURED** — sync send produced no task; passing send-result `2` as id → code 11. Docs: `{ progress:int, status:int }` | rely on docs (`TaskQueueStatus`); optional re-capture via a large `--send` |
| `alphaname/list` | `{ items:[ { alphanameId:str, globalStatus:str, partnerStatus:str, isDefault:str, statusUpdateTs, createTs, id:str, details:{description}, globalComment, partnerComment, alphaname:{ id, name, createTs, type }, userProfile:{...mostly null PII}, alphanameDocs:[{userDocId,...}] } ], totalItemCount:str }` | new `AlphanameData`: expose `AlphanameId`, `Name` (`alphaname.name`), `GlobalStatus`, `PartnerStatus`, `IsDefault`, `Description` (`details.description`); ignore `userProfile` (PII, all null) |

### `LinkData` real fields (from `link/list`,`link/get`,`link/create`)
`shortLink, domain, id:str, partnerId, userId, status:str, moderatorStatus:str, isTmp, isCustom, domainId, clickCnt:str, redirectCnt:str, deletedTs, createTs, updateTs, expirationDate, realExpirationDate, code, fullLink, comment, moderatorComment, analyticsKey`
→ Map `clickCnt`→`ClickCnt` (currently `Clicks`, unmapped, always 0); add `ShortLink, RedirectCnt, ModeratorStatus, CreateTs, UpdateTs, RealExpirationDate, ModeratorComment` (+ optionally domain/isTmp/isCustom). All numerics arrive as strings.

## Remaining gaps (to close during Phase 1)
1. `link/getStats` success per-item shape — re-capture read-only with corrected `type=daily` (tool now fixed).
2. `taskqueue/getStatus` success shape — relying on docs `{progress,status}`; optional large-send re-capture.

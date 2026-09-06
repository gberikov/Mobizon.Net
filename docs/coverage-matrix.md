# API coverage matrix

What the SDK covers, and — more importantly — how each claim was verified. Having a DTO does not mean every
field on it has been seen on the wire.

**Status legend**

| Status | Meaning |
|---|---|
| **docs** | Confirmed against the official page (`mobizon.kz/help/api-docs/*`), last reviewed 2026-09-06. |
| **capture** | Confirmed by a sanitised live response in `tests/Mobizon.Net.Tests/Payloads/` (captured 2026-06-24 with `tools/Mobizon.Net.ApiCapture`). |
| **unverified** | Implemented from an earlier plan, community references or inference. Works in unit tests against fixtures written by hand; the real contract has not been confirmed. |

Response codes 0 / error codes: **docs** ([sms-api](https://mobizon.kz/help/api-docs/sms-api)). Envelope
`{code, data, message}`: **docs + capture**. Validation-error `data` shape (`{field: [messages]}`): **docs (prose
only)** — parsed tolerantly into `MobizonApiException.FieldErrors`; other shapes leave it empty.

## Modules

| Module | Endpoint | SDK method | Input parameters | Response fields | Errors / async forms | Source | Fixtures / tests | Gaps / intentional exclusions |
|---|---|---|---|---|---|---|---|---|
| Message | `message/sendSmsMessage` | `Messages.SendSmsMessageAsync`, `QuickSendAsync` | recipient, text, from, params[name, deferredToTs, mclass, validity, shortenLinks] | `SendSmsResult` {campaignId, messageId, status} | code 1/12 validation | docs 2026-09-06; capture | `message.sendSmsMessage.json`; `MessageServiceTests` | — |
| Message | `message/getSMSStatus` | `Messages.GetSmsStatusAsync(long)` / `(long[])` | ids[0..99] (≤100, SDK-enforced) | `SmsStatusResult[]` {id, status, segNum, startSendTs, statusUpdateTs} (bare array) | unknown `status` string → `MobizonException` (no lenient enum) | docs; capture | `message.getSMSStatus.poll1-3.json`; `MessageServiceTests`, `QueryWindowAndGuardTests` | 100-id limit stated in docs; automatic chunking not provided |
| Message | `message/list` | `Messages.ListAsync` | criteria[campaignIds, from, to, text, status, groups, campaignStatus, campaign*Ts*, startSendTs*, statusUpdateTs*], withNumberInfo, pagination, sort | `MobizonListResult<MessageInfo>` | — | docs; capture | `message.list.json`; `MessageServiceTests` | `MessageInfo` fields beyond the fixture: unverified |
| Campaign | `campaign/create` | `Campaigns.CreateAsync` | data[type, text, name, from, rateLimit, ratePeriod, deferredToTs, mclass, validity, trackShortLinkRecipients, shortenLinks] | scalar id (string) | zero/missing id → `MobizonException` | docs; capture | `campaign.create.json`; `CampaignServiceTests`, `EnvelopeHandlingTests` | — |
| Campaign | `campaign/addRecipients` | `Campaigns.AddRecipientsAsync` | id; recipients[i][recipient + placeholders] / recipientContacts[i] / recipientGroups[i] / multipart recipientsFile; params[replace, placeholdersFlag, recipientsFile*] | sync: `AddRecipientEntry[]` {recipient, code, messageId, type, number, contact}; async: task id | sync codes 0/98/99 → `Outcome`; async code 100 → `TaskId`/`IsQueued`; ≤500 sync recipients per request (SDK batches); partial failure → `AddRecipientsProgress` | docs; capture (sync array, code 0) | `campaign.addRecipients.json`; `AddRecipientsBatchingTests`, `AddRecipientsResultConverterTests` | 98/99 payload shape and file-upload response captured only in hand-written fixtures (**unverified**); `contact` entry field unverified |
| Campaign | `campaign/send` | `Campaigns.SendAsync` | id | scalar (sync result) or task id with code 100 → `CampaignSendResult.IsQueued` | code 100 accepted | docs; capture (sync) | `campaign.send.json`; `CampaignServiceTests` | queued form (code 100) **unverified by capture** |
| Campaign | `campaign/get` | `Campaigns.GetAsync` | id | `CampaignData` | — | docs | `CampaignServiceTests` (hand-written JSON) | no capture; field list **unverified** |
| Campaign | `campaign/getInfo` | `Campaigns.GetInfoAsync` | id, getFilledTplCampaignText | `CampaignInfo` {…, extra{}, counters{}} | — | docs; capture | `campaign.getInfo.json`; `CampaignServiceTests` | — |
| Campaign | `campaign/list` | `Campaigns.ListAsync` | criteria[id, ids, recipient, from, text, status, create/sentTs*, type, groups], pagination, sort | `MobizonListResult<CampaignData>` | — | docs; capture | `campaign.list.json`; `CampaignServiceTests` | — |
| Campaign | `campaign/delete` | `Campaigns.DeleteAsync` | id | `true` (ignored) | — | docs; capture | `campaign.delete.json`; `EnvelopeHandlingTests` | — |
| Link | `link/getLinks` | `Campaigns.GetLinksAsync`, `Links.GetLinksAsync` | campaignId | `LinkData[]` (bare array) | — | docs | `CampaignServiceTests`, `LinkServiceTests` | no capture; **unverified** |
| Link | `link/create` | `Links.CreateAsync` | data[fullLink, status, expirationDate, comment] | `LinkData` | — | docs; capture | `link.create.json`; `LinkServiceTests` | — |
| Link | `link/get` | `Links.GetByIdAsync` / `GetByCodeAsync` / `GetByShortLinkAsync` | id / code / shortLink | `LinkData` | — | docs; capture (by id) | `link.get.json`; `LinkServiceTests` | code / shortLink selectors: docs only |
| Link | `link/getStats` | `Links.GetStatsAsync` | ids[0..4] (≤5, SDK-enforced), type (monthly/daily/hourly/minute), criteria[dateFrom, dateTo] | period-major grid `{items:[{param, clicksN, redirectsN}], totals:{totalClicksN, totalRedirectsN}}` → `LinkStatsResult.Links[]` | invalid `type` → code 12 (captured); corrupt counters → `MobizonException`; `[]` → empty | docs; capture (success + error) | `link.getStats.json`, `link.getStats.new.json`; `LinkServiceTests`, `QueryWindowAndGuardTests` | — |
| Link | `link/list` | `Links.ListAsync` | criteria[status, moderatorStatus, code, fullLink, comment, createTs*, clickCnt*], pagination, sort | `MobizonListResult<LinkData>` | — | docs; capture | `link.list.json`; `LinkServiceTests` | — |
| Link | `link/update` | `Links.UpdateAsync` | id, data[fullLink, status, expirationDate, comment] | `true` (ignored) | — | docs; capture | `link.update.json`; `EnvelopeHandlingTests` | — |
| Link | `link/delete` | `Links.DeleteAsync` | ids[] | `DeleteResult` {processed[], notProcessed[]} | — | docs; capture | `link.delete.json`; `LinkServiceTests` | — |
| User | `user/getOwnBalance` | `User.GetOwnBalanceAsync` | — | `BalanceResult` {balance, currency} | — | docs; capture | `user.getOwnBalance.json`; `UserServiceTests` | — |
| Taskqueue | `taskqueue/getStatus` | `TaskQueue.GetStatusAsync` | id | `TaskQueueStatus` {progress 0–100, status 0–3} | code 2 not found; poll ≤ 1/s (docs) | docs; capture (error code 11 only) | `taskqueue.getStatus.poll1-3.json` (all code 11 — captured with an invalid id); `TaskQueueServiceTests` (hand-written success JSON) | success payload **not captured**; shape from docs only |
| Alphaname | `alphaname/list` | `Alphanames.ListAsync` | pagination | `AlphanameData` {id, alphanameId, globalStatus, partnerStatus, isDefault, createTs, statusUpdateTs, globalComment, partnerComment, alphaname{id, name, type, createTs}, details{description}, alphanameDocs[{userDocId, userId, alphanameId}]} | — | capture only | `alphaname.list.json`; `AlphanameServiceTests`, `QueryWindowAndGuardTests` | **No public documentation found.** Not exposed: `userProfile` (PII, all null), `dateExpiredGlobal` (only seen null, format unknown), `isTemplate*Comment`, `*ModeratorUserId`, `partnerId`, `userId`, top-level `description` (only null). `globalStatus`/`partnerStatus`/`alphaname.type` kept as raw ints: value tables unknown |
| ContactGroup | `contactgroup/list`, `create`, `update`, `delete`, `getCardsCount` | `ContactGroups.*` | pagination, sort / data[name] / id | `ContactGroupListResponse`, scalar id, `true`, `DeleteResult`, scalar count | — | **unverified** (no public page found on 2026-09-06) | `ContactGroupServiceTests` (hand-written JSON) | whole module: no capture |
| ContactCard | `contactcard/list`, `get`, `create` (multipart), `update` (multipart), `delete`, `setGroups`, `getGroups` | `ContactCards.*` (LINQ-style query + CRUD) | criteria[i][field/operator/value] (operators equal/not_equal/contain/from/to/empty), pagination, sort; data[...] fields incl. address[...] and optional photo | `ContactCardData` (fields{}, groups[]), scalar id, `true`, `ContactGroupRef[]` | `list`/`get`/`getGroups` may answer `data:null` (observed) → empty/null | **unverified** (no public page found; earlier research notes in `docs/superpowers/notes`) | `ContactCardQueryTests`, `ContactCardServiceTests`, `QueryWindowAndGuardTests` (hand-written JSON) | whole module: no capture. `pageSize=1`/`2` used by `First`/`Count`/`Single` assumes any size 1–100 is accepted (unconfirmed). Allowed page sizes, max criteria count and photo constraints unknown |
| NumberStopList | `numberstoplist/list`, `create` (single / range), `delete` | `NumberStopList.*` | pagination, sort / number, numberFrom, numberTo, comment / id | `StopListListResponse`, scalar id / `true`, `true` | — | **unverified** (no public page found) | `NumberStopListServiceTests` (hand-written JSON) | whole module: no capture; range-create response shape assumed `true` |

## Webhooks (inbound)

| Event type | SDK type | Fields | Source | Fixtures / tests | Gaps |
|---|---|---|---|---|---|
| `sms-delivery-report` | `SmsDeliveryReportEvent` | campaignId, messageId, segNum, statusUpdateTs, status (typed + raw), to | docs 2026-09-06 (payload example) | `SmsDeliveryReportParseTests` | full `status` vocabulary assumed equal to `message/getSMSStatus` |
| `form-submission` | `FormSubmissionEvent` | formId, submissionId, items[] (`WebhookFieldItem`) | docs (payload example) | `FormEventParseTests`, `WebhookFieldTypeTests` | `fieldType` documented as "TEXT_STRING, EMAIL, MOBILE and others" → `FieldTypeKind` null for others |
| `form-contact-confirmation` | `FormContactConfirmationEvent` | formId, submissionId, item | docs | `FormEventParseTests` | — |
| `form-contact-unsubscribe` | `FormContactUnsubscribeEvent` | formId, unsubscribeTs, items[] | docs | `FormEventParseTests` | — |
| any other | `UnknownWebhookEvent` | envelope + `RawData` | design decision | `ForwardCompatibilityTests` | intentional: only place a raw `JsonElement` is public |
| signature | `WebhookSignatureVerifier` | `SHA1(eventId\|attempt\|eventCreateTs\|secretKey)`, hex, constant-time compare | docs | `WebhookSignatureVerifierTests` | signature does not cover `data`/`eventType`/`webhookId` (protocol limitation); no TTL by design (retries keep the original timestamp) |

## Limits encoded in the SDK

| Limit | Value | Source | Enforced |
|---|---|---|---|
| `message/getSMSStatus` ids per call | 100 | docs | `ArgumentException` |
| `campaign/addRecipients` synchronous recipients per request | 500 | docs | automatic batching |
| `link/getStats` ids per call | 5 | docs | `ArgumentException` |
| `taskqueue/getStatus` polling rate | ≤ 1 request/second | docs | documented only |
| page size | default 25, max 100 | docs (list endpoints) | documented only |
| response body | 16 MB | SDK choice | `HttpClient.MaxResponseContentBufferSize` on SDK-owned clients |

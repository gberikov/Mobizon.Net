# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Breaking

- List endpoints now return `MobizonResponse<MobizonListResult<T>>` (Campaign, Link, Message, ContactCard list operations)
- `Campaign.CreateAsync` and `SendAsync` now return `MobizonResponse<long>` instead of `MobizonResponse<CreateCampaignResult>` and `MobizonResponse<CampaignSendResult>`
- `Link.GetAsync(code)` replaced by `Link.GetByIdAsync`, `Link.GetByCodeAsync`, and `Link.GetByShortLinkAsync`
- `UpdateLinkRequest` now keyed by `Id` (no longer accepts `Code`)
- `LinkData.Clicks` property renamed to `ClickCnt`
- `Link.GetStatsAsync` now returns `LinkStatsResult { Links }` — one `LinkStatSeries` per requested link (with `LinkId`, `TotalClicks`, `TotalRedirects`, and per-period `LinkStatPoint { Param, Clicks, Redirects }`); the SDK transposes the API's period-major `clicks{i}`/`redirects{i}` grid and resolves each series back to its link ID
- `MessageInfo.SegUserBuy` now typed as `decimal` instead of `int`
- `ContactCardListResponse` renamed to `ContactCardListResult`
- All ID types widened from `int` to `long` (campaign IDs, message IDs, link IDs, task IDs, etc.)
- `IMobizonClient.Alphanames` member added (breaks hand-rolled implementers)
- `IMobizonClient` registered as transient (previously singleton)

### Added

- Alphaname module (`client.Alphanames`) for alphanumeric sender management
- `AddRecipients` file upload support with single-source validation
- `Campaign.CreateAsync` accepts new `shortenLinks` parameter
- `Link.ListAsync` criteria support for filtering and pagination
- `UpdateLinkRequest.FullLink` to repoint an existing short link's destination URL via `Link.UpdateAsync`
- `LinkStatsType` enum extended with `Hourly` and `Minute` statistic types
- Webhook endpoint body-size cap enforcement and JSON problem responses
- Tag-driven versioning via MinVer

### Fixed

- `campaign/addRecipients` endpoint deserialization for both array and scalar (task ID) request formats
- `link/getStats` deserialization — the real API returns a period-major grid with a `totals` **object** (not the scalar the old model assumed), which threw "Failed to deserialize Mobizon API response"; now parsed by a dedicated converter
- `LinkData.clickCnt` property mapping from API responses
- Injected `HttpClient.Timeout` no longer mutated by SDK operations
- README documentation corrections
- CI triggers corrected to run on `master` and `develop` branches
- `Campaign.GetInfoAsync` no longer throws when the API returns `null` for `totalPartnerCost` (`CampaignCounters.TotalPartnerCost` is now `decimal?`)
- `Message.ListAsync` filtering by `SmsStatus.Scheduled` no longer throws (added the missing `SCHEDUL` request-code mapping)
- Webhook body-size cap is now enforced by a bounded read, so a chunked or absent `Content-Length` can no longer bypass `MaxRequestBodyBytes`
- Corrected `ApiUrl` examples in XML-doc and README (the base URL must not include the `/service/` segment, which the client appends automatically)
- `AddMobizon` (DI) now applies the configured `MobizonClientOptions.Timeout` to the named `HttpClient`; previously the timeout was silently ignored on the DI path (the externally-owned client kept the 100s `HttpClient` default)
- Contact-card reads no longer fail when the PHP API serialises an unset object field (mobile/email/viber/whatsapp/landline/skype/telegram/address) as `[]` or `""` — such values now deserialize to `null`
- Unknown/future contact `type` values no longer fail the entire contact-card read — an unrecognised type degrades to `null` while the field value is preserved
- `ContactCard.BirthDate` mapping now tolerates date+time forms (e.g. `1990-01-15 00:00:00`) instead of silently dropping the value
- `ContactCards` query terminal operations (`ToListAsync`/`ToPageAsync`/`CountAsync`/`FirstOrDefaultAsync`/`SingleOrDefaultAsync`) no longer throw `NullReferenceException` when the API returns a `null` `data` payload
- `ContactCardSet.GetGroupsAsync` returns an empty list instead of `null` when the API returns no groups
- Contact-card enum filter values are now upper-cased with invariant culture, so locale-specific casing (e.g. tr-TR) no longer corrupts the wire value

## [1.0.0] - 2026-02-24

### Added

- Core SDK (`Mobizon.Net`) with full Mobizon API v1 coverage
- Message module: SendSmsMessage, GetSmsStatus, List
- Campaign module: Create, Delete, Get, GetInfo, List, Send, AddRecipients
- Link module: Create, Delete, Get, GetLinks, GetStats, List, Update
- User module: GetOwnBalance
- TaskQueue module: GetStatus
- Contracts package (`Mobizon.Contracts`) with all DTOs, interfaces, and enums
- DI integration package (`Mobizon.Net.Extensions.DependencyInjection`) with AddMobizon() extension
- Polly resilience package (`Mobizon.Net.Extensions.Polly`) with retry, circuit breaker, and timeout policies
- Regional API URL support (mobizon.kz, mobizon.uz, mobizon.com)
- Typed exception hierarchy (MobizonException, MobizonApiException)
- Console sample application
- Comprehensive unit test suite (76+ tests)

# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Breaking

- List endpoints now return `MobizonResponse<MobizonListResult<T>>` (Campaign, Link, Message, ContactCard list operations)
- `Campaign.CreateAsync` and `SendAsync` now return `MobizonResponse<long>` instead of `MobizonResponse<CreateCampaignResult>` and `MobizonResponse<CampaignSendResult>`
- `Link.GetAsync(code)` replaced by `Link.GetByIdAsync`, `Link.GetByCodeAsync`, and `Link.GetByShortLinkAsync`
- `UpdateLinkRequest` now keyed by `Id` (no longer accepts `Code` or `FullLink`)
- `LinkData.Clicks` property renamed to `ClickCnt`
- `Link.GetStatsAsync` now returns `LinkStatsResult { Items, Totals }` with individual stat points typed as `LinkStatPoint`
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
- `LinkStatsType` enum extended with `Hourly` and `Minute` statistic types
- Webhook endpoint body-size cap enforcement and JSON problem responses
- Tag-driven versioning via MinVer

### Fixed

- `campaign/addRecipients` endpoint deserialization for both array and scalar (task ID) request formats
- `LinkData.clickCnt` property mapping from API responses
- Injected `HttpClient.Timeout` no longer mutated by SDK operations
- README documentation corrections
- CI triggers corrected to run on `master` and `develop` branches

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

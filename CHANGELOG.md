# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

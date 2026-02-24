# Research: Mobizon.Net SDK

## Phase 0 Research Findings

### R-001: Mobizon API Request Format

**Decision**: Use `application/x-www-form-urlencoded` POST with
bracket notation for nested parameters.

**Rationale**: The Mobizon API exclusively accepts form-encoded
POST bodies. Nested objects use PHP-style bracket notation:
`data[field]=value`, `criteria[from]=Alpha`,
`pagination[currentPage]=2`. This is a non-standard serialization
format that requires a custom `BracketNotationSerializer`.

**Alternatives considered**:
- JSON body — not supported by Mobizon API
- Multipart form data — overkill, API doesn't require it

### R-002: Response Deserialization Strategy

**Decision**: Use `System.Text.Json` with case-insensitive
property matching and snake_case-to-PascalCase conversion.

**Rationale**: Mobizon API returns JSON with lowercase keys
(`code`, `data`, `message`). System.Text.Json with
`JsonSerializerOptions { PropertyNameCaseInsensitive = true }`
handles this cleanly. For nested data objects, use
`[JsonPropertyName("...")]` attributes on DTOs.

**Alternatives considered**:
- Newtonsoft.Json — adds 700KB+ dependency, violates Principle I
- Manual JSON parsing — error-prone, no benefit

### R-003: HTTP GET vs POST

**Decision**: `MobizonApiClient` MUST support both GET and POST.
Only `User/GetOwnBalance` uses GET; all other endpoints use POST.

**Rationale**: The Mobizon API documentation specifies GET for
the balance endpoint. The `SendAsync<T>` method accepts an
`HttpMethod` parameter to handle this.

**Alternatives considered**:
- POST-only with empty body for GET endpoints — may not work,
  API might reject POST for balance endpoint

### R-004: netstandard2.0 System.Text.Json Compatibility

**Decision**: Add `System.Text.Json` NuGet package (≥6.0.0) as
an explicit dependency for netstandard2.0.

**Rationale**: `System.Text.Json` is not available in-box for
netstandard2.0. The NuGet package provides full functionality.
Version 6.0.0+ supports all needed features (source generation
not required for our use case).

**Alternatives considered**:
- Target net6.0+ only — excludes .NET Framework consumers
- Newtonsoft.Json — heavier dependency

### R-005: HttpClient Lifetime Management

**Decision**: SDK MUST NOT create or dispose `HttpClient` instances.
The consumer provides `HttpClient` (directly or via
`IHttpClientFactory`).

**Rationale**: `HttpClient` has well-known socket exhaustion
issues when created/disposed frequently. The SDK receives
`HttpClient` via constructor injection, delegating lifetime
management to the consumer or `IHttpClientFactory`.

**Alternatives considered**:
- Internal static `HttpClient` — prevents consumer configuration
- `IHttpClientFactory` as hard dependency — forces DI on all consumers

### R-006: Error Handling Strategy

**Decision**: Two-level exception hierarchy:
1. `MobizonException` (base) — transport errors, serialization
2. `MobizonApiException` (derived) — API error codes (code != 0 and != 100)

**Rationale**: Consumers can catch `MobizonApiException` for
business-logic errors (invalid recipient, insufficient balance)
and `MobizonException` for infrastructure errors (network down).
`BackgroundTask` (code=100) is NOT an error — it returns normally
with the task ID in `Data`.

**Alternatives considered**:
- Single exception type — loses the ability to distinguish
  API errors from transport errors
- Result pattern (no exceptions) — less idiomatic in .NET SDK space

### R-007: Pagination Model

**Decision**: Use `PaginatedResponse<T>` as return type for List
methods, containing `Items`, `TotalCount`, `CurrentPage`,
`PageSize`. Input uses `PaginationRequest` + `SortRequest`.

**Rationale**: Mobizon API uses offset-based pagination with
`currentPage` and `pageSize` parameters. Response includes
items plus metadata. This maps cleanly to request/response models.

**Alternatives considered**:
- `IAsyncEnumerable` auto-pagination — over-engineering for v1.0,
  can be added later as convenience method
- Raw list return without pagination metadata — loses information

### R-008: MockHttpMessageHandler Library

**Decision**: Use `RichardSzalay.MockHttp` NuGet package for
unit testing HTTP interactions.

**Rationale**: Industry-standard library for mocking
`HttpMessageHandler` in .NET. Allows matching requests by URL,
method, content, and returning configured responses. Works with
`HttpClient` constructor injection pattern.

**Alternatives considered**:
- Custom mock handler — reinventing the wheel
- WireMock.Net — heavier, designed for integration tests

# Error Handling

[← Back to the README](https://github.com/gberikov/Mobizon.Net/blob/master/README.md)

---

The SDK uses a two-level exception hierarchy:

| Exception | When thrown |
|-----------|-------------|
| `MobizonApiException` | The API returned a non-success response code (auth failure, invalid data, not found, etc.). Exposes `Code` (`MobizonResponseCode`), `RawCode`, `ApiMessage` and `FieldErrors` — per-field validation messages when the API sent them (`ex.FieldErrors["text"]`, nested paths flattened as `mobile.value`). Thrown regardless of the HTTP status as long as the body is a valid envelope. |
| `MobizonException` | Transport or protocol failure: network error, `HttpClient.Timeout` expiry, a non-JSON body (proxy/5xx HTML page), an envelope without a numeric `code`, a non-2xx status with a "success" envelope, a missing payload, a zero id from a create call, or a payload that does not fit the expected type. `StatusCode` (`HttpStatusCode?`) carries the HTTP status when a response was received; `null` when it was not. `MobizonApiException` derives from this type. |

```csharp
try
{
    var result = await client.Messages.SendSmsMessageAsync(request);
    Console.WriteLine($"Message ID: {result.MessageId}");
}
catch (MobizonApiException ex)
{
    // API-level error — invalid parameters, bad API key, quota exceeded, etc.
    Console.WriteLine($"API Error [{ex.Code}]: {ex.ApiMessage}");
}
catch (MobizonException ex)
{
    // Transport error — network failure, timeout, response parse error
    Console.WriteLine($"SDK Error [{ex.StatusCode?.ToString() ?? "no response"}]: {ex.Message}");
}
```

Cancellation via your own `CancellationToken` still surfaces as `OperationCanceledException`; only the
client's own timeout is wrapped.

**Safe diagnostics.** Exception messages name the operation (`Campaign/Create`), the HTTP status and the JSON path of
the offending field (`$.messageId`) but never quote the response body or a field value — even in `InnerException`
and `ToString()` — so SMS texts, phone numbers, one-time codes and the API key cannot leak through ordinary
exception logging. The one exception is `ApiMessage`, the API's own error text, which is needed to act on the error.
This guarantee does not extend to anything *you* log: an `HttpClient` logging handler or a proxy that dumps request
bodies will see the API key and message texts in every request. Do not log Mobizon request bodies.

## Response codes

| `MobizonResponseCode` | Value | Meaning |
|-----------------------|-------|---------|
| `Success` | 0 | Operation completed successfully |
| `ValidationError` | 1 | Transmitted data contains invalid values |
| `NotFound` | 2 | Record not found or access denied by ID |
| `UnknownError` | 3 | Unknown application error |
| `InvalidModule` | 4 | Invalid `module` parameter |
| `InvalidMethod` | 5 | Invalid `method` parameter |
| `InvalidFormat` | 6 | Invalid `format` parameter |
| `LoginError` | 8 | Incorrect credentials or expired session |
| `AccessDenied` | 9 | Access to this API method is denied |
| `SaveError` | 10 | Server data save error |
| `MissingParameters` | 11 | Required parameters missing from the request |
| `InvalidParameter` | 12 | An input parameter violates constraints |
| `WrongServer` | 13 | Wrong regional API server |
| `AccountBlocked` | 14 | User account is blocked or deleted |
| `OperationError` | 15 | Operation error unrelated to data update |
| `RateLimitExceeded` | 30 | Too many requests; decrease the request frequency |
| `BulkPartialSuccess` | 98 | Bulk operation partially completed |
| `BulkCompleteFailure` | 99 | Bulk operation completely failed |
| `BackgroundTask` | 100 | Operation queued as a background task |
| `ServiceError` | 999 | General service error |

Codes 98 (`BulkPartialSuccess`) and 99 (`BulkCompleteFailure`) become `AddRecipientsResult.Outcome`
(`PartiallyAdded` / `NoneAdded`) for synchronous recipient loads. Code 100 (`BackgroundTask`) is accepted
by campaign sending and group/file recipient loads: `CampaignSendResult.IsQueued` or
`AddRecipientsResult.IsQueued` identifies the queued result. Group/file loads require a positive `TaskId`;
an invalid task response throws `MobizonException`. Other non-zero codes, including 100 on synchronous
recipient batches, throw `MobizonApiException`. If a later batch fails, confirmed progress remains available
through `AddRecipientsProgress.FromException`.


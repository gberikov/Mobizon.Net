# Data Model: Mobizon.Net SDK

## Overview

The SDK is stateless — no persistent storage. All entities
represent API request/response structures serialized to/from
JSON (responses) and form-urlencoded (requests).

## Core Entities

### MobizonClientOptions

Configuration for the SDK client.

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| ApiKey | string | Yes | — | Mobizon API key |
| ApiUrl | string | Yes | — | Base URL (e.g., `https://api.mobizon.kz`) |
| ApiVersion | string | No | `"v1"` | API version identifier |
| Timeout | TimeSpan | No | 30s | HTTP request timeout |

**Validation**: `ApiKey` and `ApiUrl` MUST NOT be null or empty.

### MobizonResponse\<T\>

Universal API response wrapper.

| Field | Type | Description |
|-------|------|-------------|
| Code | MobizonResponseCode | Response code enum |
| Data | T | Typed payload (may be null on error) |
| Message | string | API message (error description or empty) |

**JSON mapping**: `{"code": 0, "data": {...}, "message": ""}`

### MobizonResponseCode

| Name | Value | Meaning |
|------|-------|---------|
| Success | 0 | Operation completed successfully |
| BackgroundTask | 100 | Task queued, data contains task ID |
| InvalidData | 1 | Invalid request parameters |
| AuthFailed | 2 | Authentication failed |
| NotFound | 3 | Resource not found |
| AccessDenied | 4 | Insufficient permissions |
| InternalError | 5 | Server-side error |

Note: Additional codes may exist. Unknown codes MUST be
preserved as integer values in exceptions.

## Message Module Entities

### SendSmsMessageRequest → SendSmsResult

| Request Field | Type | API Param | Required |
|---------------|------|-----------|----------|
| Recipient | string | `recipient` | Yes |
| Text | string | `text` | Yes |
| From | string? | `from` | No |
| Validity | int? | `params[validity]` | No |

| Result Field | Type | JSON Key |
|--------------|------|----------|
| CampaignId | int | `campaignId` |
| MessageId | int | `messageId` |
| Status | int | `status` |

### SmsStatusResult

| Field | Type | JSON Key |
|-------|------|----------|
| Id | int | `id` |
| Status | int | `status` |
| SegNum | int | `segNum` |
| StartSendTs | string | `startSendTs` |

### MessageInfo

| Field | Type | JSON Key |
|-------|------|----------|
| Id | int | `id` |
| CampaignId | int | `campaignId` |
| From | string | `from` |
| Status | int | `status` |
| Text | string | `text` |

## Campaign Module Entities

### CreateCampaignRequest → CreateCampaignResult

| Request Field | Type | API Param | Required |
|---------------|------|-----------|----------|
| Type | int | `data[type]` | Yes |
| From | string | `data[from]` | Yes |
| Text | string | `data[text]` | Yes |

| Result Field | Type |
|--------------|------|
| CampaignId | int |

### CampaignData

| Field | Type |
|-------|------|
| Id | int |
| Type | int |
| From | string |
| Text | string |
| Status | int |

### CampaignInfo

| Field | Type |
|-------|------|
| Id | int |
| TotalMessages | int |
| Sent | int |
| Delivered | int |
| Failed | int |

### CampaignSendResult

| Field | Type |
|-------|------|
| TaskId | int? |
| Status | int |

### AddRecipientsRequest

| Field | Type | API Param |
|-------|------|-----------|
| CampaignId | int | `campaignId` |
| Type | int | `type` |
| Data | IReadOnlyList\<string\> | `data[]` |

## Link Module Entities

### CreateLinkRequest / UpdateLinkRequest

| Field | Type | API Param | Required |
|-------|------|-----------|----------|
| FullLink | string | `data[fullLink]` | Create: Yes, Update: No |
| Status | int? | `data[status]` | No |
| ExpirationDate | string? | `data[expirationDate]` | No |
| Comment | string? | `data[comment]` | No |
| Code | string | `code` | Update only |

### LinkData

| Field | Type |
|-------|------|
| Id | int |
| Code | string |
| FullLink | string |
| Status | int |
| ExpirationDate | string? |
| Comment | string? |
| Clicks | int |

### GetLinkStatsRequest

| Field | Type | API Param |
|-------|------|-----------|
| Ids | int[] | `ids[]` |
| Type | LinkStatsType | `type` |
| DateFrom | string? | `criteria[dateFrom]` |
| DateTo | string? | `criteria[dateTo]` |

### LinkStatsResult

| Field | Type |
|-------|------|
| LinkId | int |
| Date | string |
| Clicks | int |

## User Module Entities

### BalanceResult

| Field | Type | JSON Key |
|-------|------|----------|
| Balance | decimal | `balance` (string in JSON, parsed to decimal) |
| Currency | string | `currency` |

## TaskQueue Module Entities

### TaskQueueStatus

| Field | Type |
|-------|------|
| Id | int |
| Status | int |
| Progress | int |

## Pagination Entities

### PaginationRequest (input)

| Field | Type | Default |
|-------|------|---------|
| CurrentPage | int | 0 |
| PageSize | int | 20 |

### SortRequest (input)

| Field | Type |
|-------|------|
| Field | string |
| Direction | SortDirection |

### SortDirection (enum)

`ASC`, `DESC`

### LinkStatsType (enum)

`Daily`, `Monthly`

### PaginatedResponse\<T\> (output)

| Field | Type |
|-------|------|
| Items | IReadOnlyList\<T\> |
| TotalCount | int |
| CurrentPage | int |
| PageSize | int |

## Entity Relationships

```
IMobizonClient
├── IMessageService
│   └── uses: SendSmsMessageRequest, SendSmsResult,
│             SmsStatusResult, MessageListRequest, MessageInfo
├── ICampaignService
│   └── uses: CreateCampaignRequest, CreateCampaignResult,
│             CampaignData, CampaignInfo, CampaignSendResult,
│             AddRecipientsRequest, CampaignListRequest
├── ILinkService
│   └── uses: CreateLinkRequest, UpdateLinkRequest, LinkData,
│             GetLinkStatsRequest, LinkStatsResult, LinkListRequest
├── IUserService
│   └── uses: BalanceResult
└── ITaskQueueService
    └── uses: TaskQueueStatus

All List methods use: PaginationRequest, SortRequest, PaginatedResponse<T>
All methods return: MobizonResponse<T>
Error path: MobizonApiException (code != 0, != 100)
```

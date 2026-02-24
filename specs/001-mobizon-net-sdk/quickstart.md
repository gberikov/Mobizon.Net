# Quickstart: Mobizon.Net SDK

## Installation

```bash
# Core SDK (minimal, no DI)
dotnet add package Mobizon.Net

# With DI integration
dotnet add package Mobizon.Net.Extensions.DependencyInjection

# With resilience (retry, circuit breaker)
dotnet add package Mobizon.Net.Extensions.Polly
```

## Basic Usage (without DI)

```csharp
using Mobizon.Contracts;
using Mobizon.Net;

var options = new MobizonClientOptions
{
    ApiKey = "your-api-key-here",
    ApiUrl = "https://api.mobizon.kz"
};

using var client = new MobizonClient(
    new HttpClient(), options);

// Send SMS
var result = await client.Messages.SendSmsMessageAsync(
    new SendSmsMessageRequest
    {
        Recipient = "77001234567",
        Text = "Hello from Mobizon.Net!"
    });

Console.WriteLine($"Message ID: {result.Data.MessageId}");

// Check delivery status
var status = await client.Messages.GetSmsStatusAsync(
    new[] { result.Data.MessageId });

Console.WriteLine($"Status: {status.Data[0].Status}");
```

## ASP.NET Core (with DI + Polly)

### appsettings.json

```json
{
  "Mobizon": {
    "ApiKey": "your-api-key-here",
    "ApiUrl": "https://api.mobizon.kz"
  }
}
```

### Startup / Program.cs

```csharp
using Mobizon.Net.Extensions.DependencyInjection;
using Mobizon.Net.Extensions.Polly;

// Register with options action
services.AddMobizon(options =>
{
    options.ApiKey = configuration["Mobizon:ApiKey"];
    options.ApiUrl = configuration["Mobizon:ApiUrl"];
})
.AddMobizonResilience(); // retry + circuit breaker + timeout

// Or bind from configuration section
services.AddMobizon(configuration.GetSection("Mobizon"))
    .AddMobizonResilience();
```

### Use in a service

```csharp
public class NotificationService
{
    private readonly IMobizonClient _mobizon;

    public NotificationService(IMobizonClient mobizon)
    {
        _mobizon = mobizon;
    }

    public async Task SendOtpAsync(
        string phone, string code, CancellationToken ct)
    {
        await _mobizon.Messages.SendSmsMessageAsync(
            new SendSmsMessageRequest
            {
                Recipient = phone,
                Text = $"Your verification code: {code}"
            }, ct);
    }
}
```

## Check Balance

```csharp
var balance = await client.User.GetOwnBalanceAsync();
Console.WriteLine($"{balance.Data.Balance} {balance.Data.Currency}");
// Output: 4043.0656 KZT
```

## Campaign Workflow

```csharp
// 1. Create campaign
var campaign = await client.Campaigns.CreateAsync(
    new CreateCampaignRequest
    {
        Type = 1,
        From = "MyBrand",
        Text = "Flash sale - 50% off today!"
    });

// 2. Add recipients
await client.Campaigns.AddRecipientsAsync(
    new AddRecipientsRequest
    {
        CampaignId = campaign.Data.CampaignId,
        Type = 1,
        Data = new[] { "77001111111", "77002222222" }
    });

// 3. Send campaign
var sendResult = await client.Campaigns.SendAsync(
    campaign.Data.CampaignId);

// 4. If background task, poll status
if (sendResult.Code == MobizonResponseCode.BackgroundTask)
{
    var taskStatus = await client.TaskQueue.GetStatusAsync(
        sendResult.Data.TaskId.Value);
    Console.WriteLine($"Progress: {taskStatus.Data.Progress}%");
}
```

## Regional Configuration

```csharp
// Kazakhstan
options.ApiUrl = "https://api.mobizon.kz";

// Uzbekistan
options.ApiUrl = "https://api.mobizon.uz";

// International
options.ApiUrl = "https://api.mobizon.com";
```

## Error Handling

```csharp
try
{
    var result = await client.Messages.SendSmsMessageAsync(request);
}
catch (MobizonApiException ex)
{
    // API-level error (auth failed, invalid data, etc.)
    Console.WriteLine($"API Error {ex.Code}: {ex.ApiMessage}");
}
catch (MobizonException ex)
{
    // Transport error (network, timeout, serialization)
    Console.WriteLine($"SDK Error: {ex.Message}");
}
```

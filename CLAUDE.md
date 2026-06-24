# mobizon Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-02-24

## Active Technologies
- C# 8.0+ — core packages target `netstandard2.0`; the ASP.NET Core integration package and the console sample target `net8.0` (current LTS). + `System.Text.Json` 8.0.5 (matches existing projects); BCL `System.Security.Cryptography` (`SHA1`) — no third-party dependencies in the core. The ASP.NET Core package uses the `Microsoft.AspNetCore.App` framework reference and references `Mobizon.Net.Webhooks`. (002-webhooks)
- N/A (stateless parsing/verification; idempotency persistence is the consumer's responsibility). (002-webhooks)

- C# 8.0+ / .NET Standard 2.0 + System.Text.Json (NuGet), System.Net.Http (001-mobizon-net-sdk)

## Project Structure

```text
src/
tests/
```

## Commands

# Add commands for C# 8.0+ / .NET Standard 2.0

## Code Style

C# 8.0+ / .NET Standard 2.0: Follow standard conventions

## Recent Changes
- 002-webhooks: Added C# 8.0+ — core packages target `netstandard2.0`; the ASP.NET Core integration package and the console sample target `net8.0` (current LTS). + `System.Text.Json` 8.0.5 (matches existing projects); BCL `System.Security.Cryptography` (`SHA1`) — no third-party dependencies in the core. The ASP.NET Core package uses the `Microsoft.AspNetCore.App` framework reference and references `Mobizon.Net.Webhooks`.

- 001-mobizon-net-sdk: Added C# 8.0+ / .NET Standard 2.0 + System.Text.Json (NuGet), System.Net.Http

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->

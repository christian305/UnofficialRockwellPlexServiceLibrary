# UnofficialRockwellPlexServiceLibrary

UnofficialRockwellPlexServiceLibrary is a C#/.NET Standard client library for working with Rockwell Plex/Plex service integrations. It includes helpers for executing Plex datasources and an early Connect API surface for inventory receiving workflows.

This project is unofficial and is not endorsed by, maintained by, or affiliated with Rockwell Automation or Plex.

## Overview

The library currently provides two main integration paths:

- Plex datasource execution through `DataSourceRequest`, `Connection`, and `ConnectionType`.
- Connect API inventory receiving calls through `InventoryClient` and `InventoryReceivingClient`.

The project targets `netstandard2.0`, so it can be referenced from compatible .NET Framework, .NET Core, and modern .NET applications.

## Installation

Until this library is published as a package, reference it directly from your solution or project.

```bash
dotnet add reference path/to/RockwellPlexServiceLibrary.csproj
```

Then import the namespaces needed for the API surface you are using.

```csharp
using RockwellPlexServiceLibrary.Utils;
using RockwellPlexServiceLibrary.Request.Datasource;
using RockwellPlexServiceLibrary.Connect.Inventory.InventoryReceiving;
```

## Authentication

Create an `Authenticator` with your Plex username and password.

```csharp
var username = "your-username";
var password = "your-password";

var authenticator = new Authenticator(username, password);
```

Keep credentials out of source control. Prefer environment variables, a secret manager, or your application's existing configuration system.

## Environments

Use `ConnectionType` to select the target Plex environment for datasource calls.

```csharp
ConnectionType.Production
ConnectionType.Test
```

`ConnectionType.Test` targets the Plex test cloud datasource host. `ConnectionType.Production` targets the production cloud datasource host.

## Execute A Datasource

Use `DataSourceRequest` when you already know the datasource ID and request body.

```csharp
using RockwellPlexServiceLibrary.Request.Datasource;
using RockwellPlexServiceLibrary.Utils;

var authenticator = new Authenticator(username, password);

using (var request = new DataSourceRequest(authenticator, ConnectionType.Test))
{
    var body = new
    {
        Date_From = "2026-04-01",
        Date_To = "2026-04-30",
        Workcenter = "WC-100",
        Status = "Active"
    };

    var response = await request.ExecuteAsync("11944", body);

    if (response.IsSuccessStatusCode)
    {
        Console.WriteLine(response.Content);
    }
    else
    {
        Console.WriteLine($"Datasource call failed: {response.StatusCode} {response.ReasonPhrase}");
    }
}
```

## Use A Predefined Datasource Helper

`DataSource` includes predefined datasource definitions that build the request body for supported datasource calls.

```csharp
using RockwellPlexServiceLibrary.Request.Datasource;
using RockwellPlexServiceLibrary.Utils;

var authenticator = new Authenticator(username, password);

using (var request = new DataSourceRequest(authenticator, ConnectionType.Test))
{
    var definition = DataSource.GetMonthlyScraps(
        dateFrom: "2026-04-01",
        dateTo: "2026-04-30",
        workcenter: "WC-100",
        status: "Active");

    var response = await request.ExecuteAsync(definition);

    Console.WriteLine(response.Content);
}
```

Date parameters named with `_From` and `_To` are normalized to UTC-style timestamps before the request body is sent.

## Inventory Receiving Connect API

Use `InventoryClient` for the inventory receiving Connect API surface.

```csharp
using RockwellPlexServiceLibrary.Connect.Inventory.InventoryReceiving;
using RockwellPlexServiceLibrary.Utils;

var authenticator = new Authenticator(username, password);

using (var inventory = new InventoryClient(authenticator, ConnectionType.Production))
{
    var response = await inventory.Receiving.ListReceiptsAsync();

    if (response.IsSuccessStatusCode)
    {
        Console.WriteLine(response.Content);
    }
    else
    {
        Console.WriteLine($"Inventory call failed: {response.StatusCode} {response.ReasonPhrase}");
    }
}
```

You can also retrieve or create receipts through the receiving client.

```csharp
var receipt = await inventory.Receiving.GetReceiptAsync("receipt-id");

var created = await inventory.Receiving.CreateReceiptAsync(new
{
    // Add the request fields expected by the Plex Connect API.
});
```

## Error Handling

Responses expose the HTTP status, reason phrase, success flag, and raw content.

```csharp
if (!response.IsSuccessStatusCode)
{
    Console.WriteLine(response.StatusCode);
    Console.WriteLine(response.ReasonPhrase);
    Console.WriteLine(response.Content);
}
```

The internal HTTP clients retry transient failures such as timeouts, rate limits, and server-side errors before returning or throwing.

## Notes

- This library is unofficial and should be validated against your Plex environment before production use.
- Do not commit usernames, passwords, datasource IDs, or environment-specific values that should remain private.
- The current datasource helpers are examples of the library's public API shape and may need to be adjusted for your specific Plex datasource configuration.

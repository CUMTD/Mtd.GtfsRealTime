# Copilot Instructions — MTD GTFS-Realtime API

## What this repository does

This is a .NET 10 ASP.NET Core API that exposes the three GTFS-Realtime feed endpoints for the Champaign-Urbana Mass Transit District (MTD):

- **`GET /trip-updates/`** — proxies a binary protobuf response from a configured upstream feed server
- **`GET /vehicle-positions/`** — proxies a binary protobuf response from a configured upstream feed server
- **`GET /service-alerts/`** — builds a `FeedMessage` locally from active `Reroute` entities in the Stopwatch Azure SQL database
- **`GET /metadata/`** — aggregates metadata from all three feeds; always returns JSON regardless of `AllowJson` config

There is no authentication or rate limiting. All four endpoints are public (unrestricted CORS).

---

## Solution layout

| Project | Purpose |
|---|---|
| `Mtd.GtfsRealTime.Protos` | Raw `.proto` files packaged as a NuGet content package |
| `Mtd.GtfsRealTime.Codegen` | Compiles the proto → C# via `Grpc.Tools`; generates classes in the `TransitRealtime` namespace |
| `Mtd.GtfsRealTime.Proto.Helpers` | Pure C# helpers: `RerouteConverter`, HTML stripping, enum/string mapping, `DateTimeHelpers` |
| `Mtd.GtfsRealTime.Api` | The web API (controllers, formatter, OpenAPI transformers, health checks, Swagger UI) |

The `Proto.Helpers` project references `Codegen` (for `TransitRealtime.*` types) and `Stopwatch.Core` (for `Reroute` entity and `IRerouteRepository`).

---

## Key architecture decisions

### Protobuf is the default response format
`ProtoOutputFormatter` is registered before `SystemTextJsonOutputFormatter`. When no `Accept` header is sent (or it is `*/*`), the API returns `application/protobuf`. JSON is opt-in via the `AllowJson` config key. In production `AllowJson` is `false`.

### Pass-through controllers vs. local build
- `TripUpdatesController` and `VehiclePositionsController` both inherit `ProtoController<FeedMessage>` and call `GetProtoResponseFromDownstreamServer(uri, ct)`. This method fetches bytes from the upstream server and either returns them as-is (protobuf) or parses + serializes to proto3 JSON.
- `ServiceAlertsController` inherits `ControllerBase` directly (not `ProtoController<FeedMessage>`) because it never calls a downstream feed. It queries the DB and calls `reroutes.ConvertToFeedMessage()`.
- `MetadataController` inherits `ProtoController<FeedMessage>` but uses `FetchAndParseFromDownstreamServer` to get parsed feeds for metadata extraction. It always returns JSON.

### `ProtoController<TMessage>` exposes three levels of downstream access
The base class provides three protected methods, each building on the previous:
1. **`FetchBytesFromDownstreamServer(Uri, CancellationToken)`** → `(byte[]? ResponseBytes, IActionResult? Error)` — fetches raw bytes without parsing. Used by `GetProtoResponseFromDownstreamServer` for the fast protobuf pass-through path.
2. **`FetchAndParseFromDownstreamServer(Uri, CancellationToken)`** → `(TMessage? Message, IActionResult? Error)` — fetches and parses into a typed proto message. Used by `MetadataController` to extract metadata from feeds.
3. **`GetProtoResponseFromDownstreamServer(Uri, CancellationToken)`** → `Task<IActionResult>` — fetches and returns either protobuf bytes or proto3 JSON depending on the client's `Accept` header. Used by `TripUpdatesController` and `VehiclePositionsController`.

### `ProtoController<TMessage>` is for downstream feed access
Do **not** make `ServiceAlertsController` inherit `ProtoController<TMessage>` — it would cause an unnecessary `HttpClient` injection. For any future locally-built feed, also inherit `ControllerBase` directly.

### `MetadataController` is JSON-only
`MetadataController` inherits `ProtoController<FeedMessage>` (it needs `FetchAndParseFromDownstreamServer`), but it returns plain C# model objects — not protobuf. It uses `[Produces("application/json")]` to override the global protobuf `ProducesAttribute` filter and serializes manually with `System.Text.Json.JsonSerializer.Serialize()` + `Content(json, "application/json")` to bypass the output formatter pipeline entirely. This works regardless of the `AllowJson` config setting.

### The formatter handles both `byte[]` and `IMessage`
`ProtoOutputFormatter.WriteResponseBodyAsync` handles two cases:
1. `byte[]` — returned by pass-through controllers (raw bytes from upstream, no parsing overhead)
2. `IMessage` — returned by `ServiceAlertsController` (locally built `FeedMessage`)

### Cross-assembly partial classes don't work
The generated `FeedMessage` class lives in `Mtd.GtfsRealTime.Codegen.dll`. You cannot extend it with a `partial class` in `Mtd.GtfsRealTime.Proto.Helpers` (different assembly). If you need to extend a generated proto type, add the extension inside `Mtd.GtfsRealTime.Codegen`.

### `ISerializeDTO` is legacy / unused
The `ISerializeDTO` interface was a previous design. It is no longer referenced or implemented by any type. Do not add new implementations of it.

---

## Tech stack

| Concern | Technology |
|---|---|
| Framework | ASP.NET Core 10 (`Microsoft.NET.Sdk.Web`) |
| Proto codegen | `Grpc.Tools` + `Google.Protobuf` |
| Database | Azure SQL via EF Core 10 (`StopwatchContext`) |
| Authentication | `DefaultAzureCredential` (managed identity in prod; `az login` locally) |
| Configuration | `appsettings.json` + Azure Key Vault (`AddAzureKeyVault`) |
| Logging | Serilog → Seq + Console |
| Output cache | ASP.NET Core Output Cache middleware |
| Swagger UI | `swagger-ui-dist` 5.32.x with custom CSS + dark mode |
| OpenAPI | `Microsoft.AspNetCore.OpenApi` 10.x |

---

## Coding conventions

- **C# 13 / .NET 10** — use the latest language features where appropriate.
- **`TreatWarningsAsErrors = true`** — all projects. Fix warnings; do not suppress them without a comment.
- **Nullable reference types enabled** — always null-check constructor parameters with `ArgumentNullException.ThrowIfNull`.
- **`init` setters on config classes** — options/config POCOs use `required ... { get; init; }`.
- **`static class` for constant containers** — not `record` or `class`.
- **XML docs on all public and protected members** — required.

---

## Configuration and secrets

All sensitive configuration comes from Azure Key Vault. The only secret that needs to be set locally is:

```
KeyVaultUrl = https://<keyvault>.vault.azure.net/
```

Set via `dotnet user-secrets set "KeyVaultUrl" "..."`. Everything else (connection strings, feed URLs, Seq credentials) is read from Key Vault at startup.

In development, `appsettings.Development.json` sets `"AllowJson": true` to enable JSON responses in Swagger UI.

---

## Output cache policies

| Policy constant | Duration | Used by |
|---|---|---|
| `RealTimeDataCacheProfile.NAME` | 5 seconds | `TripUpdatesController`, `VehiclePositionsController`, `MetadataController` |
| `StaticDataCacheProfile.NAME` | 5 minutes | `ServiceAlertsController` |

Both policies vary by `Accept` header. **Always call `app.UseOutputCache()` in the pipeline** — without the middleware, `[OutputCache]` attributes are silently ignored.

---

## OpenAPI / Swagger UI customizations

- `ProtobufBinaryMediaTypeTransformer` — marks protobuf response schemas as `{type: string, format: binary}` so spec consumers know it's a binary download.
- `HideHeadAndOptionsTransformer` — removes `HEAD` and `OPTIONS` from the OpenAPI document (they still work at the HTTP level).
- Swagger UI uses a custom `swagger-custom.css` with brand colors and dark mode support (`html.dark-mode` selector, swagger-ui 5.31+ pattern).

---

## Health checks

Registered under `/health/{tag}`. Tags: `live`, `ready`, `healthy`, `db`, `network`.

`DownstreamHeadHealthCheck` is registered **twice** (once for trip-updates, once for vehicle-positions) under different names. It uses `context.Registration.Name` to resolve the correct URL from `Config.Health` options.

---

## Common pitfalls

- **`app.UseOutputCache()` must be in the pipeline** — `builder.Services.AddOutputCache(...)` alone is not sufficient.
- **Protobuf formatter must be inserted into `options.OutputFormatters`** — it is `Add`ed (appended) to ensure it participates in content negotiation.
- **`AllowJson` is `false` by default** — in production, `SystemTextJsonOutputFormatter` is removed from the pipeline entirely. Do not assume JSON is available.
- **Do not set `Content-Disposition` for JSON responses** — only `ProtoOutputFormatter` sets this header. The JSON path returns via `Content(json, "application/json")` directly.
- **`FeedHeader.Timestamp` should always be set** — `RerouteConverter` sets it to `DateTime.UtcNow.ToPosixTime()`. Pass-through controllers preserve the upstream feed's existing timestamp (it is inside the binary bytes).
- **`System.Text.Json` only serializes properties, not fields** — record types with field declarations (e.g. `public required string[] Ids;`) will serialize as `{}`. Always use properties with `{ get; init; }` on model/DTO records.

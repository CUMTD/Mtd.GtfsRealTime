---
applyTo: "Mtd.GtfsRealTime.Protos/**"
---

# Mtd.GtfsRealTime.Protos

## Purpose

This project contains the shared GTFS-Realtime `.proto` source file(s) and is published as a NuGet content package. It does **not** compile any C# code itself — its sole job is to distribute the `.proto` file to consumers so they can run their own codegen.

## Contents

| File | Description |
|---|---|
| `Protos/gtfsrt.proto` | The GTFS-Realtime 2.0 protocol buffer schema. This is the canonical Google GTFS-RT spec proto file. |
| `Mtd.GtfsRealTime.Protos.props` | MSBuild `.props` file auto-injected into consuming projects via `buildTransitive`. Wires up the `<Protobuf>` item and sets `Protobuf_AdditionalImportDirs` so `protoc` can find `google/transit/gtfs-realtime.proto` imports. |

## How the NuGet package works

When a project references this NuGet package (e.g. `Mtd.GtfsRealTime.Codegen`):

1. The `.proto` file is placed in `contentFiles/any/any/Protos/` inside the package.
2. `buildTransitive/Mtd.GtfsRealTime.Protos.props` is automatically evaluated by MSBuild, which:
   - Adds a `<Protobuf Include="...gtfsrt.proto" GrpcServices="$(MtdGtfsRt)" .../>` item so `Grpc.Tools` picks it up.
   - Appends the `Google.Api.CommonProtos` include path to `Protobuf_AdditionalImportDirs`.

## Modifying the proto file

- Edit `Protos/gtfsrt.proto` to update the schema.
- The GTFS-Realtime spec is maintained at https://github.com/google/transit/tree/master/gtfs-realtime/proto.
- After editing, rebuild `Mtd.GtfsRealTime.Codegen` — `Grpc.Tools` will regenerate the C# classes automatically.
- Do **not** add hand-written `.cs` files to this project. It is a content-only package.

## Controlling gRPC service generation in consumers

The `.props` file exposes a `$(MtdGtfsRt)` MSBuild property (defaults to `Both`) that consumers can override before importing the props to control gRPC stub generation:

```xml
<PropertyGroup>
  <MtdGtfsRt>None</MtdGtfsRt>  <!-- message classes only, no gRPC stubs -->
</PropertyGroup>
```

Valid values: `None`, `Client`, `Server`, `Both`.

## This project in the solution

`Mtd.GtfsRealTime.Codegen` references this project (`<ProjectReference>`) rather than the NuGet package during development so that local proto changes are immediately reflected in the generated C#.

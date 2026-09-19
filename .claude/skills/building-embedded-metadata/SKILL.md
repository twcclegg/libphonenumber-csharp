---
name: building-embedded-metadata
description: Understand or change how resources/ becomes the binary metadata embedded in the assembly — the PhoneNumbers.MetadataBuilder tool, the MSBuild GenerateBinaryMetadata/EmbedBinaryMetadata targets in PhoneNumbers.csproj, the ResourcePack container, and the loaders that read the result. Use when metadata seems stale or missing after a local edit, when a build fails or races in those targets (CS2012, parallel writes to obj, MissingMetadataException), when changing which data sets a trimmed build can drop, or when adding a new kind of embedded data.
---

# Building the embedded metadata

Nothing under `resources/` ships as-is and nothing is read from disk at run time.
`PhoneNumbers.MetadataBuilder` converts the XML and text files into per-region binaries during
`dotnet build`, writing each one through a `GZipStream` (`CompressionLevel.SmallestSize`) as it
goes; `PhoneNumbers.csproj` then embeds those already-compressed files into the assembly. At run
time `MetadataSource` + `EmbeddedResourceMetadataLoader` pull a region's binary out of the
assembly's resources and decompress it on first use.

The whole pipeline lives in **`csharp/PhoneNumbers/PhoneNumbers.csproj`** — the targets named in
the table below. Read the comments there before changing any of it — several encode races that
already broke CI.

## Shape of it

| Target | Produces | From |
| --- | --- | --- |
| `GenerateBinaryMetadata` | everything below, in one `dotnet PhoneNumbers.MetadataBuilder.dll all` call | all of `resources/` |
| `EmbedBinaryMetadata` | `EmbeddedResource` items with explicit `LogicalName`s | the generated files |
| `CleanBinaryMetadata` | (deletes the generated bins on `dotnet clean`) | — |

One target and one process, not the five targets and seven invocations this used to be. The tool
still accepts the individual subcommands (`phone`, `short`, `alternate`, `geocoding`, `carrier`,
`locale`, `timezones`) — `PhoneNumbers.Test.csproj` uses `geocoding` and `carrier` for its own
fixture data — and `all` is a thin loop over them.

Five embedded resource shapes come out:

| Logical name | Contents |
| --- | --- |
| `PhoneNumbers.metadata.<file>` | one resource per region (~540 of them) |
| `PhoneNumbers.geocoding.pack` | every geocoding prefix map, one `ResourcePack` |
| `PhoneNumbers.carrier.pack` | every carrier prefix map, one `ResourcePack` |
| `PhoneNumbers.locale.pack` | every country's display names, one `ResourcePack` |
| `PhoneNumbers.timezones.map_data.bin` | the time zone prefix map |

**Why the last four are single resources.** A trimmed build can drop them via
`ILLink.Substitutions.xml`, and a `<resource>` element there matches an exact name with no wildcard
support — `PhoneNumbers.geocoding.*` is reported as not found (IL2040), not expanded. One resource
per file would mean a ~690-entry substitutions file regenerated on every metadata sync. Packed, the
substitutions file is four fixed lines that never change. `ResourcePack.cs` is the container, source-
linked into MetadataBuilder for the writer; entries stay individually gzipped so a caller still
decompresses only the one map it asked for.

**A pack holds its directory, never its payloads.** `ResourcePack.Read` reopens the resource stream
and reads the one entry asked for. Materialising all entries at load instead is the obvious
simplification and it costs 1.9 MB of permanently retained memory for any process that geocodes
(measured: 1511 KB to 3430 KB), because an embedded resource stream is a view over the already-mapped
assembly image and copying out of it moves the whole data set onto the GC heap. The repo's
`--retained-memory` audit does not cover the geocoder, so nothing in CI would have caught it. `TestResourcePack.cs` asserts every name in the
substitutions file is a real resource, and that each packed data set is exactly one resource —
without those, a rename would silently stop the trimming from working.

## The four rules that keep it working

1. **Invoke the dll, never `dotnet run`.** MetadataBuilder is scheduled by a build-only
   `ProjectReference`, then executed as `dotnet <path>/PhoneNumbers.MetadataBuilder.dll`. `dotnet run`
   builds in a second, uncoordinated process; when the solution builds `PhoneNumbers` and
   `PhoneNumbers.Test` in parallel, both race on MetadataBuilder's `obj` folder and fail with CS2012.
2. **Don't add an `<MSBuild>` call on MetadataBuilder.** The `ProjectReference` already schedules it.
   Doing both creates two parallel builds of the same project in the cross-targeting flow, racing on
   apphost generation.
3. **Keep the `Inputs`/`Outputs` gates.** Generation runs once on the outer cross-targeting build;
   the gates are what make the per-TFM inner builds see fresh outputs and skip. Remove them and three
   concurrent invocations write the same bin file at once — that is the failure that broke CI.
4. **Collect wildcards in a top-level `ItemGroup`, not inside the target.** `Inputs` is evaluated
   before the target body runs, so an item group defined in the body is empty at gate-check time and
   the target reports up-to-date forever.

The per-region metadata `Outputs` are still *sentinel* files (`PhoneNumberMetadata_US` and friends)
standing in for ~540 siblings, because MetadataBuilder is all-or-nothing per subcommand. The four
data-set outputs are named exactly, since each is now a single file.

The tool creates its own output directories. The targets no longer `<MakeDir>` first, and a data set
that relies on the caller having done so fails only on a clean build.

## Symptoms and causes

- **Stale results after editing `resources/` locally** — an `Inputs`/`Outputs` gate decided the
  target was up to date. Delete `csharp/PhoneNumbers/obj` and rebuild. (Also: local edits to
  `resources/` are overwritten by the next upstream sync; see `syncing-upstream-metadata`.)
- **`MissingMetadataException` at run time** — the binary was not embedded, or the `LogicalName`
  no longer matches what the loader asks for. Check the `EmbedBinaryMetadata` item metadata against
  `MetadataManager` / `EmbeddedResourceMetadataLoader`, and inspect the assembly's manifest
  resource names rather than assuming.
- **CS2012 on `refint/PhoneNumbers.MetadataBuilder.dll`** — something reintroduced a second
  concurrent build of MetadataBuilder. See rules 1 and 2.
- **Two files write to the same bin concurrently** — a gate was dropped. See rule 3.

## Changing the binary format

`BuildMetadataFromBin.cs` / `BuildPrefixMapFromBin.cs` (readers, ship) and
`PhoneNumbers.MetadataBuilder` (writer, build-time only) are a matched pair, and MetadataBuilder
source-links the few files it needs from the library rather than referencing it. Change one side
and the other must move in the same commit — there is no format version negotiation, and a
mismatch surfaces as garbage data or an exception on first metadata load, not as a build error.
`TestBuildMetadataFromBin.cs` and `TestBuildPrefixMapFromBin.cs` cover the readers.

`BuildMetadataFromXml.cs` still ships, but only for build time and the legacy
`PhoneNumberUtil(Stream)` constructor that consumers use to load custom XML. Don't delete it as
dead code, and remember it is public surface — see `changing-public-api`.

Adding a new kind of embedded data means: a MetadataBuilder subcommand, a step in `BuildAll`, an
`Inputs`/`Outputs` entry on `GenerateBinaryMetadata`, an `EmbedBinaryMetadata` entry with a stable
`LogicalName`, a `CleanBinaryMetadata` entry, and a loader. If it should be droppable from a trimmed
build, it also needs to be a single `ResourcePack`, an `ILLink.Substitutions.xml` entry gated on a
feature switch, a property in `buildTransitive/libphonenumber-csharp.targets`, and a guard that
throws rather than returning empty when the data is absent — see `PhoneNumbersFeatures`. Verify with a clean build (`dotnet build csharp` after
deleting `obj`) plus a run of the full test suite, since embedding faults only appear at load time.

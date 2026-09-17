---
name: building-embedded-metadata
description: Understand or change how resources/ becomes the binary metadata embedded in the assembly — the PhoneNumbers.MetadataBuilder tool, the MSBuild Generate*/Embed targets in PhoneNumbers.csproj, and the loaders that read the result. Use when metadata seems stale or missing after a local edit, when a build fails or races in those targets (CS2012, parallel writes to obj, MissingMetadataException), or when adding a new kind of embedded data.
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
| `GenerateBinaryMetadata` | `PhoneNumberMetadata_*`, `ShortNumberMetadata_*`, `PhoneNumberAlternateFormats_*` | the three XML files in `resources/` |
| `GenerateGeocodingBins` | per-language, per-prefix bins | `resources/geocoding/**/*.txt` |
| `GenerateCarrierBins` | carrier prefix maps | `resources/carrier/**/*.txt` |
| `GenerateLocaleBins` | per-country display names | `resources/locale/country_names.txt` |
| `GenerateTimezoneBin` | `map_data.bin` | `resources/timezones/map_data.txt` |
| `EmbedBinaryMetadata` | `EmbeddedResource` items with explicit `LogicalName`s | all of the above |
| `CleanBinaryMetadata` | (deletes the generated bins on `dotnet clean`) | — |

Each `Generate*` target invokes the built `PhoneNumbers.MetadataBuilder.dll` with a subcommand
(`phone`, `short`, `alternate`, `geocoding`, `carrier`, `locale`, `timezones`). Logical names are
`PhoneNumbers.metadata.<file>`, `PhoneNumbers.geocoding.<file>`, `PhoneNumbers.carrier.<file>`,
`PhoneNumbers.locale.<file>` and `PhoneNumbers.timezones.map_data.bin`.

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

Note the `Outputs` are *sentinel* files (e.g. `PhoneNumberMetadata_US`, geocoding `en.1`), not the
full output set — MetadataBuilder is all-or-nothing per subcommand, so once the sentinel is current
every sibling is too.

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

Adding a new kind of embedded data means: a MetadataBuilder subcommand, a `Generate*` target with
correct `Inputs`/`Outputs`, an `EmbedBinaryMetadata` entry with a stable `LogicalName`, a
`CleanBinaryMetadata` entry, and a loader. Verify with a clean build (`dotnet build csharp` after
deleting `obj`) plus a run of the full test suite, since embedding faults only appear at load time.

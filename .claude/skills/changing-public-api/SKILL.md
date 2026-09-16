---
name: changing-public-api
description: Add, change or remove public API in the libphonenumber-csharp or libphonenumber-csharp.extensions packages. Use when introducing a public type or member, changing a signature, deciding which of the two packages new API belongs in, or when a build fails on package validation (PKV/CP diagnostics), TFM parity, trim/AOT analysis, or a missing netstandard2.0 fallback.
---

# Changing the public surface

Both packable projects have `EnablePackageValidation` on, so the build itself is the gate: an API
that is inconsistent across target frameworks, or a breaking change against the published baseline,
fails the build rather than shipping.

## First: is a new member authorised?

**Adding a public member to `csharp/PhoneNumbers/` needs explicit sign-off from the user, as its
own decision.** Package validation only catches removals, so nothing automated will object. "It
matches an existing pattern" is not permission — `IMetadataLoader`/`SetMetadataLoader` and
`PrewarmRegionsAsync` were added on exactly that reasoning and both were regretted. Ask, every
time. `PhoneNumbers.Extensions` is exempt and exists to grow.

This is the same rule `AGENTS.md` states; keep the two copies identical. A task like "fix this perf
issue" does not authorise a new public member as a side effect. If the answer is no, an `internal`
member or a helper in `PhoneNumbers.Extensions` usually serves.

## Which package?

- **`csharp/PhoneNumbers/`** (`libphonenumber-csharp`) — anything with a counterpart in the Java
  library. Keep the Java name and shape; see the `porting-upstream-changes` skill.
- **`csharp/PhoneNumbers.Extensions/`** (`libphonenumber-csharp.extensions`) — C#-idiomatic helpers
  with *no* Java counterpart: the `TryParse`-style `PhoneNumber` static class, the
  `System.Text.Json` converter and source-generated context, `PhoneNumberAttribute`,
  `PhoneNumberTypeConverter`. Putting these in the core package makes every future upstream sync a
  diff to reconcile, so new non-ported conveniences belong here.

## The constraints

Both projects target `netstandard2.0;net8.0;net10.0`.

- **TFM parity is mandatory.** A member that exists on `net10.0` but not `netstandard2.0` fails
  package validation. If the implementation needs a modern BCL API, ship the same signature on every
  target with a fallback implementation — for `PhoneNumberUtil` that means the modern half in
  `PhoneNumberUtil.net.cs` and the netstandard2.0 half in `PhoneNumberUtil.netstandard.cs`.
- **Trim and AOT.** `IsAotCompatible` is set on the modern TFMs and warnings are errors, so no
  reflection or dynamic code may be reachable from anything public. The JSON support in Extensions
  goes through a source-generated `JsonSerializerContext` for this reason.
- **Nullable reference types** are enabled on every target but `netstandard2.0`. Annotate regardless
  — an unannotated new API is inconsistent with everything around it.
- **Breaking changes.** Each pack is compared against `PackageValidationBaselineVersion` in the
  csproj (a real nuget.org release — restore downloads it). If a break is genuinely intended, that
  is a maintainer decision about the next version — raise it rather than suppressing the diagnostic
  or bumping the baseline to hide it. Moving the baseline forward *after* a release ships is a
  separate, routine chore: bump both csprojs together to the released version.
- **Extensions is analysed more strictly** — `AnalysisMode=AllEnabledByDefault` there versus the
  default rule set in the core library, so code that builds in `PhoneNumbers/` may not in
  `PhoneNumbers.Extensions/`.
- **XML docs.** `CS1591` (missing doc comment) is suppressed repo-wide, so nothing forces you to
  document a new member. Document it anyway; it is the only reference consumers get.

## Verify

```bash
dotnet build csharp --no-restore                     # catches TFM parity, trim/AOT, validation
dotnet test csharp/PhoneNumbers.slnx -p:TargetFrameworks=net10.0
dotnet pack -c Release csharp/PhoneNumbers           # publish_nuget.yml adds -p:VersionPrefix=<tag minus "v">
dotnet pack -c Release csharp/PhoneNumbers.Extensions
```

`dotnet build` matters more than `dotnet test` here: the tests only run on `net8.0`/`net10.0`, so a
netstandard2.0 gap is invisible to them and shows up only in the build.

## Test the new API

Public entry points get hostile input from real callers. Beyond the normal ported tests, add the new
surface to `csharp/PhoneNumbers.Test/TestPublicApiRobustness.cs` (curated hostile strings) and, where
an invariant is worth asserting, `TestPhoneNumberProperties.cs` (FsCheck). Both run against the real
shipped metadata, and that pair has already caught an unbounded `stackalloc` and a
`KeyNotFoundException` that the well-formed-input suite missed. The `writing-tests` skill has the
detail.

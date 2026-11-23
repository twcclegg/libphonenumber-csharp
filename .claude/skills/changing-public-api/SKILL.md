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
own decision.** Package validation only catches breaks against the published baseline — never
additions, so nothing automated will object. "It matches an existing pattern" is not permission —
`IMetadataLoader`/`SetMetadataLoader` and `PrewarmRegionsAsync` were added on exactly that
reasoning and both were regretted. Ask, every time. `PhoneNumbers.Extensions` is exempt and
exists to grow.

This is the same rule `AGENTS.md` states; keep the two copies identical. A task like "fix this perf
issue" does not authorise a new public member as a side effect. If the answer is no, an `internal`
member or a helper in `PhoneNumbers.Extensions` usually serves.

## Which package?

- **`csharp/PhoneNumbers/`** (`libphonenumber-csharp`) — anything with a counterpart in the Java
  library. Keep the Java name and shape; see the `porting-upstream-changes` skill.
- **`csharp/PhoneNumbers.Extensions/`** (`libphonenumber-csharp.extensions`) — C#-idiomatic helpers
  with *no* Java counterpart: `PhoneNumber.TryParse` (`PhoneNumberParser`), `number.Format(...)` and
  friends (`PhoneNumberFormatting`), the `System.Text.Json` converter, `ValidPhoneNumberAttribute`.
  Putting these in the core package makes every future upstream sync a diff to reconcile, so new
  non-ported conveniences belong here. Follow the Extensions conventions below.

## Extensions conventions

Set by #475; the old `PhoneNumbers.Extensions` namespace is what breaking them looked like.

- **Never reuse a core type's name** for an Extensions type, not even with different casing (VB
  is case-insensitive). The old static class `PhoneNumbers.Extensions.PhoneNumber` made every file
  importing both namespaces fail with CS0104. Member names matter too: a new extension method with
  the same name and receiver as an old one is CS0121 in files importing both, which is why the new
  helpers are `Format`/`IsValidNumber`, not `ToE164`/`IsValid`.
- **Namespaces.** Helpers that operate on core types go in `PhoneNumbers`, so `using PhoneNumbers;`
  is the only import a consumer needs. Framework integrations go in a namespace mirroring the BCL
  one they extend: `PhoneNumbers.Text.Json`, `PhoneNumbers.ComponentModel.DataAnnotations`.
  Never add anything to `PhoneNumbers.Extensions`; it is obsolete.
- **Static members on core types** (`PhoneNumber.TryParse`) are C# 14 `extension(PhoneNumber)` blocks
  inside a class with its own name (`PhoneNumberParser`). The compiler also emits each member as a
  plain static on that class, which is the spelling C# 13 and earlier and VB use. Instance-style
  helpers are classic `this` extension methods, so C# 7.3 and VB get the same spelling as C# 14.
  `PhoneNumbers.Extensions.CSharp12.Test` compiles every fallback spelling under `LangVersion 12`;
  add new API to it. DocFX (2.81) does not list extension-block members, so document their
  signatures in the containing class's remarks.
- **`LangVersion` is pinned to 14** in the Extensions csproj, and `Nullable` is on for every target
  including netstandard2.0, where `Polyfills/` supplies the attributes. Annotate everything.
- **Obsoleting** uses a `PNX` diagnostic id per replaceable thing: add the id, message and
  `UrlFormat` to `Obsoletions.cs`, a section with an explicit `<a id="PNXnnnn"></a>` anchor to
  `docs/diagnostics.md` (DocFX lower-cases generated heading ids; `UrlFormat` substitutes the id
  verbatim), and `[EditorBrowsable(Never)]`, which also drops the type from the docs site. The
  `UrlFormat` page is compiled into consumers' warnings, so it never moves. PNX0001–PNX0005 are
  taken; the next is PNX0006. On netstandard2.0 the internal `System.ObsoleteAttribute` polyfill
  carries the id (the compiler matches the attribute by name), hence `NoWarn CS0436` there.
- **Don't break consumers of obsolete API**: forward to the new implementation only where the
  behaviour is identical; otherwise keep the old body. Their tests live in
  `PhoneNumbers.Extensions.Test/Obsolete/` behind per-file `#pragma warning disable PNX000n`.

## The constraints

Both projects target `netstandard2.0;net8.0;net10.0`.

- **TFM parity is mandatory, and only half of it is checked.** Package validation fails a member
  that exists on `netstandard2.0` but is missing from `net8.0`/`net10.0` (CP0002, because a
  consumer moving up a TFM would lose it). It does **not** catch the reverse: a member present on
  `net10.0` but missing from `netstandard2.0` packs cleanly (verified twice). Check that direction
  yourself. If the implementation needs a modern BCL API, ship the same signature on every target
  with a fallback implementation — for `PhoneNumberUtil` that means the modern half in
  `PhoneNumberUtil.net.cs` and the netstandard2.0 half in `PhoneNumberUtil.netstandard.cs`.
- **Trim and AOT.** `IsAotCompatible` is set on the modern TFMs and warnings are errors, so no
  reflection or dynamic code may be reachable from anything public. The JSON converter is used
  through the consumer's own source-generated context; `PhoneNumbers.Extensions.AotTest` publishes
  that setup with Native AOT in CI and runs it.
- **Nullable reference types** are enabled on every target but `netstandard2.0` in the core library,
  and on every target in Extensions. Annotate regardless — an unannotated new API is inconsistent
  with everything around it.
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
dotnet build csharp --no-restore                     # trim/AOT, validation (one direction of TFM parity)
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

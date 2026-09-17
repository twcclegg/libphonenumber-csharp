---
name: porting-upstream-changes
description: Port a bug fix, feature, or test from the upstream google/libphonenumber Java library into this C# port. Use when parsing, validation, formatting, matching, or geocoding behaviour differs from Java, when a metadata sync stops because the upstream diff touched .java files, or when asked to port or re-check anything against upstream.
---

# Porting an upstream change

This repo is a port, not an independent implementation. A behaviour difference from Java is a bug
here unless `docs/api-differences-from-java.md` documents it as deliberate. Reinventing a fix that
already exists upstream makes the next sync harder, so **always look at the Java first**.

```
Port:
- [ ] 1. Find the upstream Java (and its test)
- [ ] 2. Port the code faithfully
- [ ] 3. Port the test into the right file (writing-tests skill)
- [ ] 4. Build every TFM, run the tests, link the upstream commit
```

## 1. Find the upstream code

The Java lives in `google/libphonenumber` under
`java/libphonenumber/src/com/google/i18n/phonenumbers/`, with tests under
`java/libphonenumber/test/com/google/i18n/phonenumbers/`. Names map almost one to one:

| C# | Java |
| --- | --- |
| `csharp/PhoneNumbers/PhoneNumberUtil.cs` | `PhoneNumberUtil.java` |
| `csharp/PhoneNumbers/AsYouTypeFormatter.cs` | `AsYouTypeFormatter.java` |
| `csharp/PhoneNumbers/PhoneNumberMatcher.cs` | `PhoneNumberMatcher.java` |
| `csharp/PhoneNumbers/ShortNumberInfo.cs` | `ShortNumberInfo.java` |
| `csharp/PhoneNumbers/BuildMetadataFromXml.cs` | `BuildMetadataFromXml.java` |
| `csharp/PhoneNumbers/PhoneNumberOfflineGeocoder.cs` | `PhoneNumberOfflineGeocoder.java` (in `java/geocoder/src/.../geocoding/`) |
| `csharp/PhoneNumbers.Test/TestPhoneNumberUtil.cs` | `PhoneNumberUtilTest.java` |

Fetch the specific file or commit from GitHub rather than cloning the whole upstream repo. If the
user pointed at an upstream issue or commit, read that diff — it usually carries the test too.

## 2. Port it faithfully

- Keep the upstream structure, method names (PascalCased) and comments. Divergence for style's sake
  costs more at the next sync than it saves now.
- Java `CharSequence` parameters become `string`, `java.util.Locale` becomes this library's own
  `Locale`, and enum members keep Java's `SCREAMING_SNAKE_CASE` — all deliberate; see
  `docs/api-differences-from-java.md` before "fixing" any of them.
- If the change needs a BCL API missing from `netstandard2.0`, put the modern implementation in
  `PhoneNumberUtil.net.cs` and the fallback in `PhoneNumberUtil.netstandard.cs`, with the same
  signature on both.
- Don't introduce an ad-hoc `Regex`; go through `RegexCache` / `PhoneRegex` like the surrounding
  code, and never with `RegexOptions.Compiled` for a metadata pattern.
- If the upstream fix changes metadata rather than code, it belongs upstream — do not edit
  `resources/`; the next sync brings it.
- A new public member on `PhoneNumbers` needs the user's explicit sign-off even when it is a
  faithful port — see `changing-public-api`.

## 3. Port the test, into the right file

Ported Java tests go in the class that mirrors the Java test file (`TestPhoneNumberUtil.cs` for
`PhoneNumberUtilTest.java`, and so on). Those classes sit in the `[Collection("TestMetadataTestCase")]`
and use `TestMetadataTestCase.PhoneUtil`, which loads the synthetic
`PhoneNumberMetadataForTesting.xml`. A regression for a *real* region needs the real metadata and
belongs elsewhere — the `writing-tests` skill covers the split.

## 4. Verify

```bash
dotnet test csharp/PhoneNumbers.slnx -p:TargetFrameworks=net10.0
```

Before opening a PR run the full matrix (`dotnet test csharp/PhoneNumbers.slnx`, which adds
net8.0) and a plain `dotnet build csharp` — the tests never run on netstandard2.0, so a broken
fallback in `PhoneNumberUtil.netstandard.cs` shows up only in the build. If the change touches a
parse/format/match hot path, follow the `tuning-hot-paths` skill as well.

In the commit or PR body, link the upstream commit or issue you ported from.

---
name: diagnosing-number-behaviour
description: Work out why a specific phone number parses, validates, formats, geocodes or types the way it does, and whether the cause is upstream Google metadata or a bug in this port. Use for any report of the form "number X in region Y returns Z but should return W", for triaging an incoming issue about a number or country code, and before starting a fix on reported number behaviour.
---

# Diagnosing a reported number result

Most reports of the form "this number is wrong" are **not fixable in this repo**. The library is
code plus a verbatim copy of Google's metadata, and the metadata is the answer far more often than
the code is. Establish which one you are looking at before writing a fix, or you will edit
`resources/` and have it silently reverted by the next sync.

```
Diagnosis:
- [ ] 1. Reproduce against the shipped metadata (scratch test)
- [ ] 2. Compare with Google's demo for the same number + region
- [ ] 3a. Metadata → explain the rule, point upstream, no code change
- [ ] 3b. Port bug → read the Java, then follow porting-upstream-changes
```

## 1. Reproduce against the shipped metadata

Add a temporary `[Fact]` named `Scratch` to `csharp/PhoneNumbers.Test/TestPublicApiRobustness.cs` —
that class already uses `PhoneNumberUtil.GetInstance()`, i.e. the real embedded metadata, where
`TestMetadataTestCase` would give you synthetic regions instead. Run just that fact:

```bash
dotnet test csharp/PhoneNumbers.Test --filter "FullyQualifiedName~Scratch" --logger "console;verbosity=detailed"
```

Print the whole picture, not only the failing assertion: `IsValidNumber`,
`IsPossibleNumberWithReason`, `GetNumberType`, `GetRegionCodeForNumber`, and `Format` in E164 /
INTERNATIONAL / NATIONAL. The distinction between *possible* (length) and *valid* (pattern)
explains a large share of reports on its own. Delete the scratch fact once you have the answer, or
promote it to a real regression case if this turns out to be a port bug.

## 2. Apply the split

**Compare against Google's own demo — <https://libphonenumber.appspot.com> — with the same number
and region.** That single comparison is the discriminator:

| Google's demo | This library | Verdict |
| --- | --- | --- |
| Same (wrong) answer | Same (wrong) answer | **Upstream metadata.** Not fixable here. |
| Correct answer | Different, wrong answer | **Port bug.** Fix it here. |

Also *not* a metadata issue, even though it can look like one: anything about build, packaging,
NuGet, trimming/AOT, performance, or API design; and anything about geocoding/locale **display
names**, which this repo generates itself from a local JDK via `lib/DumpLocale.java` rather than
syncing from Google.

The repo automates this same judgement on new issues — `.github/workflows/triage_metadata_issues.yml`
classifies them against `.github/triage/system_prompt.md` and the verified history in
`.github/triage/metadata_examples.md`. Read those examples when a case is borderline; they are the
human-verified record of what has actually turned out to be metadata here.

## 3a. If it is metadata

Say so, point the reporter at <https://github.com/google/libphonenumber/issues>, and explain that
the next sync (~every two weeks) brings the fix once Google publishes it. **Do not edit
`resources/`** — see the `syncing-upstream-metadata` skill.

To show *why* the library answers as it does, read the rules rather than guessing — but extract
them, never open the file. `PhoneNumberMetadata.xml` is 957 KB over 32,000 lines,
`ShortNumberMetadata.xml` 406 KB, and a geocoding table runs to 3.8 MB; one territory is ~500 lines
of the first. Reading one of these whole costs most of a context window and answers nothing that
these do not:

```bash
# GB validity, types and formats: <generalDesc>, the per-type descs and <availableFormats>
sed -n '/<territory id="GB"/,/<\/territory>/p' resources/PhoneNumberMetadata.xml

# short codes and emergency numbers for the same region
sed -n '/<territory id="GB"/,/<\/territory>/p' resources/ShortNumberMetadata.xml

# prefix → place, longest match wins; same `prefix|value` shape under carrier/ and timezones/
grep -m5 '^4420' resources/geocoding/en/44.txt
```

Those four descs and the format list are the entire basis for validity, type and formatting, so the
extracted block is the whole answer — quoting the exact pattern that rejected the number turns "it's
a metadata issue" into something the reporter can act on upstream. If a pattern is long, quote the
one alternative that matters rather than pasting the block back.

## 3b. If it is a port bug

Read the Java for the same code path before changing anything, and follow the
`porting-upstream-changes` skill. A genuine divergence usually means a ported method drifted, not
that the algorithm needs redesigning. Add the number as a regression case where the `writing-tests`
skill says real-metadata cases go — a case parked in a `TestMetadataTestCase` class proves nothing
about the real number.

Check `docs/api-differences-from-java.md` first: some differences from Java (`string` where Java
takes `CharSequence`, the library's own `Locale` type) are deliberate, documented, and not bugs.

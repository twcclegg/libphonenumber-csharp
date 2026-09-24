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
- [ ] 3a. Metadata → point upstream with regulatory evidence, no code change
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

What makes that report actionable is **evidence from the numbering authority** — the national
regulator's numbering plan, the range-holder's own published allocation, a carrier's documentation —
for the number or range and the classification it should have. Upstream asks for exactly that, and
nothing else substitutes for it.

Do **not** go digging in `resources/` to explain the answer. Citing the pattern that rejected the
number is not evidence: the XML is a build input that this port copies verbatim from Google and
compiles into the embedded binary metadata, so it restates the behaviour you already reproduced in
step 1 rather than justifying it. Upstream reports are not argued or settled on its contents — they
are settled on the regulatory source. Step 2's comparison against Google's demo is the whole
verdict; reading the metadata adds nothing to it and costs a large part of a context window
(`PhoneNumberMetadata.xml` is 957 KB over 32,000 lines; a geocoding table runs to 3.8 MB).

## 3b. If it is a port bug

Read the Java for the same code path before changing anything, and follow the
`porting-upstream-changes` skill. A genuine divergence usually means a ported method drifted, not
that the algorithm needs redesigning. Add the number as a regression case where the `writing-tests`
skill says real-metadata cases go — a case parked in a `TestMetadataTestCase` class proves nothing
about the real number.

Check `docs/api-differences-from-java.md` first: some differences from Java (`string` where Java
takes `CharSequence`, the library's own `Locale` type) are deliberate, documented, and not bugs.

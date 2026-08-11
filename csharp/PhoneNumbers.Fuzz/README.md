# Fuzzing

Coverage-guided fuzzing of the public parsing surface, using [SharpFuzz] to instrument
`PhoneNumbers.dll` and [libFuzzer] to drive it through the [libfuzzer-dotnet] bridge.

`Program.cs` reads one input as `<region byte><number text>`: the first byte selects the default
region, the rest is the string handed to `Parse` and friends. `NumberParseException` is the
documented failure for input that is not a phone number, so it is caught; anything else that
escapes is a crash.

This project is not in `PhoneNumbers.slnx` — see the comment in the `.csproj`.

## Running it locally

Needs clang (to build the bridge) and the .NET SDK. Linux only: the libfuzzer-dotnet bridge does
not support macOS.

```bash
# 1. Build the libFuzzer bridge.
git clone https://github.com/Metalnem/libfuzzer-dotnet libfuzzer-dotnet-src
clang -fsanitize=fuzzer libfuzzer-dotnet-src/libfuzzer-dotnet.cc -o libfuzzer-dotnet

# 2. Publish the target and instrument the library under test.
dotnet publish csharp/PhoneNumbers.Fuzz -c Release -o fuzz-out
dotnet tool restore
dotnet sharpfuzz fuzz-out/PhoneNumbers.dll

# 3. Fuzz. Crashes are written to artifacts/.
mkdir -p artifacts
./libfuzzer-dotnet -timeout=10 -artifact_prefix=artifacts/ \
  --target_path=dotnet --target_arg=fuzz-out/PhoneNumbers.Fuzz.dll \
  csharp/PhoneNumbers.Fuzz/corpus
```

Only `PhoneNumbers.dll` is instrumented. Instrumenting `PhoneNumbers.Fuzz.dll` itself, SharpFuzz,
or dnlib would report coverage for the harness rather than the library.

Add `-max_total_time=<seconds>` to bound a run, or `-runs=<n>` to bound it by iteration count.

## Reproducing a crash

A crash file is just an input. The target runs it directly when it is not hosted by libFuzzer:

```bash
dotnet fuzz-out/PhoneNumbers.Fuzz.dll artifacts/crash-<hash>
```

That reproduces under a debugger too, which is usually the fastest way to get a stack trace. Turn
anything it finds into a case in `PhoneNumbers.Test/TestPublicApiRobustness.cs` so it stays fixed.

## In CI

`.github/workflows/fuzz.yml` runs this weekly and on demand, seeded from the corpus here and from
the previous run's cached corpus. A crash fails the job and uploads the input as an artifact.

[SharpFuzz]: https://github.com/Metalnem/sharpfuzz
[libFuzzer]: https://llvm.org/docs/LibFuzzer.html
[libfuzzer-dotnet]: https://github.com/Metalnem/libfuzzer-dotnet

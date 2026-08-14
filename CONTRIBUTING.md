# Contributing to libphonenumber-csharp

Thank you for considering contributing to libphonenumber-csharp! You can contribute to libphonenumber-csharp with issues and PRs.

Simply filing issues for problems you encounter or contributing any implementation of an issue is greatly appreciated.

### Suggested Workflow

We use and recommend the following workflow:

1. Create an issue for your work.
    - You can skip this step for trivial changes.
    - Reuse an existing issue on the topic, if there is one.
    - For trivial changes, feel free to start work without an agreement from the maintainers.
    - For slightly larger changes, you can reach out to the maintainers to discuss the change.
    - Clearly state that you are going to take on implementing it if you are planning to, you can request that the issue be assigned to you.
2. Create a personal fork of the repository on GitHub (if you don't already have one).
3. In your fork, create a branch off of main (`git checkout -b mybranch`).
    - Name the branch so that it clearly communicates your intentions, such as issue-123 or githubhandle-issue.
4. Make and commit your changes to your branch.
5. Add new tests corresponding to your change, if applicable.
6. Build the repository with your changes.
    - Make sure that the builds are clean.
    - Make sure that the tests are all passing, including your new tests.
7. Create a pull request (PR) against the **main** branch.
    - State in the description what issue or improvement your change is addressing.
    - Check if all the Continuous Integration checks are passing.
8. Wait for feedback or approval of your changes from the maintainers
9. Maintainers will merge once all checks are green and they are happy with the change
    - The next official build will automatically include your change.

Essentially, we are following trunk based development

### Building and testing

```bash
dotnet restore csharp
dotnet build csharp --no-restore
dotnet test csharp/PhoneNumbers.sln -p:TargetFrameworks=net10.0   # what the PR check runs
```

A few things that will fail the build or CI if missed:

* **Warnings are errors.** `TreatWarningsAsErrors` is on for every project, including the trim and AOT analyzers on the modern targets.
* **Package versions live in one place.** Add or change versions in `csharp/Directory.Packages.props`, never in a `PackageReference` — Central Package Management rejects an inline `Version`.
* **The public API is validated.** Package validation compares the packable projects' surface across target frameworks, so a member added on only one target fails the build.
* **Some files are generated.** `LocaleData.cs` and `CountryCodeToRegionCodeMap.cs` are produced by the metadata tooling, and everything under `resources/` is copied verbatim from upstream — metadata fixes belong in [google/libphonenumber](https://github.com/google/libphonenumber), since the next automated sync overwrites local edits.

Changes under `csharp/PhoneNumbers/` also trigger a benchmark run that posts a before/after comparison to the pull request. If you are changing a hot path, look at that comment rather than guessing.

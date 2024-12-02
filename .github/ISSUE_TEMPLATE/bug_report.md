---
name: Bug report
about: Create a report to help us improve
title: ''
labels: ''
assignees: ''

---

# Contributing to libphonenumber-csharp

Thanks for contributing to libphonenumber-csharp!

We appreciate your effort in helping us improve the library by submitting a bug report.

Please read the following checklist before filing an issue.

## Checklist before filing an issue

- [ ] Is the issue reproducible using the [Google demo](http://libphonenumber.appspot.com)?
    - If yes, please file an issue with [Google](https://github.com/google/libphonenumber)
    - If not, proceed to the next steps.
- [ ] Is this issue related to the metadata for a specific region phone number?
    - If yes, please file an issue with [Google](https://github.com/google/libphonenumber)
      - We use all metadata information directly from Google's libphonenumber repository, see an example [here](https://github.com/twcclegg/libphonenumber-csharp/commit/eacacd3783a14880461adad9c38f614469fcca3c)
      - See github action [here](https://github.com/twcclegg/libphonenumber-csharp/actions/workflows/create_new_release_on_new_metadata_update.yml) that will automatically update the metadata and publish a new nuget package.
    - See examples of an issue that should be filed with [Google](https://github.com/google/libphonenumber) and not in this repository
      - [here](https://github.com/twcclegg/libphonenumber-csharp/issues/259)
      - [here](https://github.com/twcclegg/libphonenumber-csharp/issues/214)
      - [here](https://github.com/twcclegg/libphonenumber-csharp/issues/272)
    - If not, proceed to the next steps.
- [ ] Have you upgraded to the latest version of the library?
    - If not, please upgrade to the latest version and check if the issue still exists.
    - If yes, proceed to the next steps.
- [ ] Have you provided a clear and concise title for the issue?
- [ ] Have you included all relevant details and steps to reproduce the issue?
- [ ] Have you attached any necessary logs, screenshots, or code snippets?

Thank you for your contribution!

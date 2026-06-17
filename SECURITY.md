# Security Policy

## Supported Versions

`libphonenumber-csharp` tracks upstream Google libphonenumber metadata releases and
ships frequent updates. Security fixes are applied to the latest released version on
NuGet only. Please make sure you are on the most recent release before reporting an
issue.

| Version            | Supported          |
| ------------------ | ------------------ |
| Latest release     | :white_check_mark: |
| Older releases     | :x:                |

## Reporting a Vulnerability

**Please do not report security vulnerabilities through public GitHub issues,
discussions, or pull requests.**

Instead, report them privately using GitHub's
[private vulnerability reporting](https://github.com/twcclegg/libphonenumber-csharp/security/advisories/new).
This creates a private advisory that only the maintainers can see.

When reporting, please include as much of the following as you can:

- A description of the issue and the affected component (e.g. parsing, matching,
  short-number handling, metadata loading).
- The version of the package you are using and the target framework.
- A minimal proof-of-concept or input that reproduces the problem.
- The potential impact (e.g. denial of service, incorrect validation result,
  information disclosure).

We will acknowledge your report as quickly as we can and keep you updated on the
progress toward a fix and release.

## Scope and Threat Model

This is a library for parsing, formatting, and validating phone numbers. In typical
usage the **phone-number strings passed to the public API are untrusted**
(end-user input), while the **metadata shipped inside the assembly is trusted**.

Security-relevant reports we are particularly interested in include:

- Denial of service from untrusted input (e.g. excessive CPU/memory, catastrophic
  regular-expression backtracking, unbounded allocation).
- Unhandled exceptions escaping the public API for inputs that should instead be
  rejected with `NumberParseException`.
- XML external entity (XXE) or other parsing issues in the `Stream`-based metadata
  loading constructor, when a consumer loads custom metadata.

Reports that require an attacker to supply malicious **metadata XML** are lower
severity, since metadata is normally trusted; we still want to know about them.

## Disclosure

We follow coordinated disclosure. Once a fix is available and released, we will
publish a GitHub Security Advisory crediting the reporter (unless you prefer to
remain anonymous).

PhoneNumbers C# Library
=======================

This is a C# port of libphonenumber, originally from:
  https://github.com/google/libphonenumber

Original Java code is Copyright (C) 2009-2022 Google Inc.

`lib/`
  update script

`PhoneNumbers/`
  Port of libphonenumber Java library

`PhoneNumbers.Test/`
  Port of libphonenumber Java tests in xunit format.

`PhoneNumbers.Extensions/`
  C#-idiomatic helpers with no Java counterpart, shipped as a separate package.

`PhoneNumbers.Extensions.Test/`
  Tests for the above.

`PhoneNumbers.MetadataBuilder/`
  Build-time tool that converts the XML metadata and the geocoding, carrier and timezone
  text files into the per-region binary files the library embeds.

`PhoneNumbers.PerformanceTest/`
  BenchmarkDotNet harness.

`PhoneNumbers.Demo/` and `PhoneNumbers.Demo.Tests/`
  Blazor WebAssembly demo deployed to GitHub Pages, and its bUnit tests.


Known Issues
------------

- Phone number metadata is serialized to a custom binary format rather than protocol buffers.
  The XML source files in `resources/` are converted to per-region binary files at build time
  by `PhoneNumbers.MetadataBuilder`; the published assembly embeds those binaries and never
  reads XML or protocol buffers at runtime.

- Geocoding, timezone, and carrier prefix maps are similarly converted to binary at build time
  and embedded in the assembly. No zip files or text files are needed to run the library or its
  tests.

- Java's public API accepts `CharSequence` in several entry points (e.g.
  `PhoneNumberMatcher`'s constructor, `isViablePhoneNumber`), letting callers pass a `String`,
  `StringBuilder`, or `StringBuffer` without copying. The C# port takes `string` in the
  equivalent spots (`PhoneNumberMatcher(PhoneNumberUtil, string, ...)`,
  `PhoneNumberUtil.IsViablePhoneNumber(string)`) instead of a comparable abstraction, so a
  caller building up a number in a `StringBuilder` has to call `.ToString()` first.

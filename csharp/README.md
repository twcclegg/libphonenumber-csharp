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


Known Issues
------------

- Phone number metadata is serialized to a custom binary format rather than protocol buffers.
  The XML source files in `resources/` are converted to per-region binary files at build time
  by `PhoneNumbers.MetadataBuilder`; the published assembly embeds those binaries and never
  reads XML or protocol buffers at runtime.

- Geocoding, timezone, and carrier prefix maps are similarly converted to binary at build time
  and embedded in the assembly. No zip files or text files are needed to run the library or its
  tests.


Todo
----

- Restore the Java logging calls?
- Find a suitable replace for Java CharSequence in phone numbers parsing API.

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

- Phone numbers metadata is read from XML files and not protocol
  buffers one. I could not make it work using protobuf-csharp
  library. On the other hand, it makes one less dependency.


Todo
----

- Restore the Java logging calls?
- Find a suitable replace for Java CharSequence in phone numbers parsing API.
- Migrate geocoder and related files

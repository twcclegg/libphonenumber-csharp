PhoneNumbers C# Library
=======================

This is a C# port of libphonenumber, originally from:
  http://code.google.com/p/libphonenumber/.

Original Java code is Copyright (C) 2009-2020 Google Inc.

lib/
  NUnit, Google.ProtoBuffersLite binaries and various conversion
  scripts.

PhoneNumbers/
  Port of libphonenumber Java library

PhoneNumbers.Test/
  Port of libphonenumber Java tests in xunit format.


Building
--------

Open csharp/PhoneNumbers.sln VS2019 solution file to get an overview
of the code. "Build All" should put the libraries at the usual places.

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

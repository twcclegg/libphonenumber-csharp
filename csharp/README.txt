PhoneNumbers C# Library
=======================

This is a C# port of libphonenumber, originally from:
  http://code.google.com/p/libphonenumber/.

Original Java code is Copyright (C) 2009-2011 Google Inc.


Project Layout
--------------

The project is a copy of the original libphonenumber with C# code
added in csharp/ root directory. The intent is to keep pulling from
the main repository and update the C# port accordingly.

lib/
  NUnit and Google.ProtoBuffersLite binaries

PhoneNumbers/
  Port of libphonenumber Java library

PhoneNumbers.Test/
  Port of libphonenumber Java tests in NUnit format.


Building
--------

Open csharp/PhoneNumbers.sln VS2008 solution file to get an overview
of the code. "Build All" should put the libraries at the usual places.


Running Tests
-------------

To run the tests, either use a GUI tool or download NUnit binaries from:

  http://www.nunit.org/index.php?p=download

Extract the executables and add the following invocation to
PhoneNumbers.Test Debug panel:

* Start external program: C:\path\to\nunit-console.exe
* Command line arguments: PhoneNumbers.Test.dll /wait /run:PhoneNumbers.Test

Then run PhoneNumbers.Test, the test console window should appear and
the tests run.


Known Issues
------------

- TestEqualWithItalianLeadingZeroSetToDefault is currently disabled as
  generated classes Equals() method does not exist on their related
  Builder.

- Check all Equals call sites

- PhoneNumberOfflineGeocoder always return country English names as I
  have found no way to get localized name in .Net.

- PhoneNumberOfflineGeocoder may raise InvalidArgException() for some
  inputs as .Net does not support all current country codes in
  RegionInfo constructor. For instance RegionType("BS") for Bahamas
  fails for me.

- Phone numbers metadata is read from XML files and not protocol
  buffers one. I could not make it work using protobuf-csharp
  library. On the other hand, it makes one less dependency.


Todo
----

- Restore the Java logging calls?
- Find a suitable replace for Java CharSequence in phone numbers parsing API.
- Migrate geocoder and related files


Porting New Versions
--------------------

This port was first converted from Subversion to Mercurial with
hgsubversion extension, from the source repository:

  http://libphonenumber.googlecode.com/svn

To update the port:

- Ensure you have a recent Mercurial and hgsubversion setup on your
system.

- Update the port SVN metadata with:

  $ hg svn rebuildmeta

And possibly add the following to .hg/hgrc:

  [paths]
  default = http://libphonenumber.googlecode.com/svn

At this point, hg incoming should display new revisions.

- Pull new changes, update to "csharp" branch and merge with
  "default".

- Fire your favorite diff tool and start porting changes.

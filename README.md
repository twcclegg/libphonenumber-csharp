# Overview

# libphonenumber-csharp
Fork of original C# clone here:  https://bitbucket.org/pmezard/libphonenumber-csharp/wiki/Home

# From the original WIKI
## Conversion Notes

C# port of Google's libphonenumber library:

  [[http://code.google.com/p/libphonenumber/]]

  The code was rewritten from the Java source mostly unchanged, please refer to the original documentation for sample code and API documentation.

  The original Apache License 2.0 was preserved.

  See [[https://bitbucket.org/pmezard/libphonenumber-csharp/src/tip/csharp/README.txt|csharp/README.txt]] for details about the port.

## Features

  * Parsing/formatting/validating phone numbers for all countries/regions of the world.
  * GetNumberType - gets the type of the number based on the number itself; able to distinguish Fixed-line, Mobile, Toll-free, Premium Rate, Shared Cost, VoIP and Personal Numbers (whenever feasible).
  * IsNumberMatch - gets a confidence level on whether two numbers could be the same.
  * GetExampleNumber/GetExampleNumberByType - provides valid example numbers for 218 countries/regions, with the option of specifying which type of example phone number is needed.
  * IsPossibleNumber - quickly guessing whether a number is a possible phonenumber by using only the length information, much faster than a full validation.
  * AsYouTypeFormatter - formats phone numbers on-the-fly when users enter each digit.
  * FindNumbers - finds numbers in text input 

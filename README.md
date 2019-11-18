[![Build status](https://ci.appveyor.com/api/projects/status/76abbk0qveot0mbo/branch/master?svg=true)](https://ci.appveyor.com/project/twcclegg/libphonenumber-csharp/branch/master)
[![codecov](https://codecov.io/gh/twcclegg/libphonenumber-csharp/branch/master/graph/badge.svg)](https://codecov.io/gh/twcclegg/libphonenumber-csharp)
[![NuGet](https://img.shields.io/nuget/dt/libphonenumber-csharp.svg)](https://www.nuget.org/packages/libphonenumber-csharp/)


C# port of Google's [libphonenumber library](https://github.com/googlei18n/libphonenumber).

  The code was rewritten from the Java source mostly unchanged, please refer to the original documentation for sample code and API documentation.

  The original Apache License 2.0 was preserved.

  See [this](https://github.com/twcclegg/libphonenumber-csharp/blob/master/csharp/README.txt "csharp/README.txt") for details about the port.

## Example

```cs
  var phoneNumberUtil = PhoneNumbers.PhoneNumberUtil.GetInstance();
  var e164PhoneNumber = "+44 117 496 0123";
  var nationalPhoneNumber = "2024561111";
  var smsShortNumber = "83835";
  var phoneNumber = phoneNumberUtil.Parse(e164PhoneNumber, null);
  phoneNumber = phoneNumberUtil.Parse(nationalPhoneNumber, "US");
  phoneNumber = phoneNumberUtil.Parse(smsShortNumber, "US");
```

## Features

  * Parsing/formatting/validating phone numbers for all countries/regions of the world.
  * GetNumberType - gets the type of the number based on the number itself; able to distinguish Fixed-line, Mobile, Toll-free, Premium Rate, Shared Cost, VoIP and Personal Numbers (whenever feasible).
  * IsNumberMatch - gets a confidence level on whether two numbers could be the same.
  * GetExampleNumber/GetExampleNumberByType - provides valid example numbers for 218 countries/regions, with the option of specifying which type of example phone number is needed.
  * IsPossibleNumber - quickly guessing whether a number is a possible phonenumber by using only the length information, much faster than a full validation.
  * AsYouTypeFormatter - formats phone numbers on-the-fly when users enter each digit.
  * FindNumbers - finds numbers in text input 

## ToDo

  * port read/write source xml data to binary for better performance and smaller .nupkg size (WIP)
  * update / add / port new unit tests from java source

## How to unfold automatic generated files
  * Install Jetbrains - Resharper for Visual Studio
  * File by file, right click and "Cleanup code"
  * Check the unfolded file


Available on NuGet as package [`libphonenumber-csharp`](https://www.nuget.org/packages/libphonenumber-csharp).

[![Build status](https://ci.appveyor.com/api/projects/status/76abbk0qveot0mbo/branch/main?svg=true)](https://ci.appveyor.com/project/twcclegg/libphonenumber-csharp/branch/main)
[![codecov](https://codecov.io/gh/twcclegg/libphonenumber-csharp/branch/main/graph/badge.svg)](https://codecov.io/gh/twcclegg/libphonenumber-csharp)
[![NuGet](https://img.shields.io/nuget/dt/libphonenumber-csharp.svg)](https://www.nuget.org/packages/libphonenumber-csharp/)

C# port of Google's [libphonenumber library](https://github.com/google/libphonenumber).

The code was rewritten from the Java source mostly unchanged, please refer to the original documentation for sample code and API documentation.

The original Apache License 2.0 was preserved.

See [this](csharp/README.md) for details about the port.

Phone number metadata is updated in the Google repo approximately every two weeks. This library is automatically updated by a [scheduled github action](https://github.com/twcclegg/libphonenumber-csharp/actions/workflows/create_new_release_on_new_metadata_update.yml) to include the latest metadata, usually within a day.

## Installation

Run the following command to add this library to your project

```
dotnet add package libphonenumber-csharp
```

Available on NuGet as package [`libphonenumber-csharp`](https://www.nuget.org/packages/libphonenumber-csharp).

## Examples

### Parsing a phone number
```csharp
using PhoneNumbers;

var phoneNumberUtil = PhoneNumberUtil.GetInstance();
var e164PhoneNumber = "+44 117 496 0123";
var nationalPhoneNumber = "2024561111";
var smsShortNumber = "83835";
var phoneNumber = phoneNumberUtil.Parse(e164PhoneNumber, null);
phoneNumber = phoneNumberUtil.Parse(nationalPhoneNumber, "US");
phoneNumber = phoneNumberUtil.Parse(smsShortNumber, "US");
```

### Formatting a phone number
```csharp
using PhoneNumbers;

var phoneNumberUtil = PhoneNumberUtil.GetInstance();
var phoneNumber = phoneNumberUtil.Parse("+14156667777", "US");
var formattedPhoneNumber = phoneNumberUtil.Format(phoneNumber, PhoneNumberFormat.INTERNATIONAL);
var formattedPhoneNumberNational = phoneNumberUtil.Format(phoneNumber, PhoneNumberFormat.NATIONAL);

Console.WriteLine(formattedPhoneNumber.ToString()); // +1 415-666-7777
Console.WriteLine(formattedPhoneNumberNational.ToString()); // (415) 666-7777
```

### Check if a phone number is valid
```csharp
using PhoneNumbers;

var phoneNumberUtil = PhoneNumberUtil.GetInstance();
var phoneNumber = phoneNumberUtil.Parse("+14156667777", "US");
var isValid = phoneNumberUtil.IsValidNumber(phoneNumber);

Console.WriteLine(isValid); // true
```

### Get the type of a phone number
```csharp
using PhoneNumbers;

var phoneNumberUtil = PhoneNumberUtil.GetInstance();
var phoneNumber = phoneNumberUtil.Parse("+14156667777", "US");
var numberType = phoneNumberUtil.GetNumberType(phoneNumber);

Console.WriteLine(numberType); // PhoneNumberType.FIXED_LINE_OR_MOBILE
```

See [PhoneNumberType.cs](csharp/PhoneNumbers/PhoneNumberType.cs) for the various possible types of phone numbers

### Get the region code for a phone number
```csharp
using PhoneNumbers;

var phoneNumberUtil = PhoneNumberUtil.GetInstance();
var phoneNumber = phoneNumberUtil.Parse("+14156667777", null);
var regionCode = phoneNumberUtil.GetRegionCodeForNumber(phoneNumber);

Console.WriteLine(regionCode); // US
```

## Features

* Parsing/formatting/validating phone numbers for all countries/regions of the world.
* GetNumberType - gets the type of the number based on the number itself; able to distinguish Fixed-line, Mobile, Toll-free, Premium Rate, Shared Cost, VoIP and Personal Numbers (whenever feasible).
* IsNumberMatch - gets a confidence level on whether two numbers could be the same.
* GetExampleNumber/GetExampleNumberByType - provides valid example numbers for 218 countries/regions, with the option of specifying which type of example phone number is needed.
* IsPossibleNumber - quickly guessing whether a number is a possible phone number by using only the length information, much faster than a full validation.
* AsYouTypeFormatter - formats phone numbers on-the-fly when users enter each digit.
* FindNumbers - finds numbers in text input

See [PhoneNumberUtil.cs](csharp/PhoneNumbers/PhoneNumberUtil.cs) for the various methods and properties available.

## ToDo

* port read / write source xml data to binary for better performance and smaller .nupkg size (WIP)
* update / add / port new unit tests and logging from java source

## How to unfold automatic generated files

* Install Jetbrains - Resharper for Visual Studio
* File by file, right click and "Cleanup code"
* Check the unfolded file

## Running tests locally

To run tests locally, you will need a zip version of the `geocoding.zip` file stored in the `resources` folder
and `testgeocoding.zip` file stored in the `resources/test` folder.

On linux, you can run the following commands to generate the zip accordingly

```bash
(cd resources/geocoding; zip -r ../../resources/geocoding.zip *)
(cd resources/test/geocoding; zip -r ../../../resources/test/testgeocoding.zip *)
```

For windows, you can use the following powershell script

```powershell
Compress-Archive -Path "resources\geocoding\*" -DestinationPath "resources\geocoding.zip"
Compress-Archive -Path "resources\test\geocoding\*" -DestinationPath "resources\test\testgeocoding.zip"
```

## Contributing
See [CONTRIBUTING.md](CONTRIBUTING.md)

## Donations

[![Buy me a beer](https://raw.githubusercontent.com/twcclegg/libphonenumber-csharp/main/bmacButton.png)](https://www.buymeacoffee.com/tclegg)

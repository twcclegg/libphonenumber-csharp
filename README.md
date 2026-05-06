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

### Get the time zones for a phone number
```csharp
using PhoneNumbers;

var phoneNumberUtil = PhoneNumberUtil.GetInstance();
var timeZonesMapper = PhoneNumberToTimeZonesMapper.GetInstance();
var phoneNumber = phoneNumberUtil.Parse("+12128120000", null);
var timeZones = timeZonesMapper.GetTimeZonesForNumber(phoneNumber);

Console.WriteLine(string.Join(", ", timeZones)); // America/New_York
```

Returns a `List<string>` of [IANA time zone identifiers](https://www.iana.org/time-zones). For numbers that span multiple time zones (e.g. a country-level lookup), the list will contain more than one entry. Returns `["Etc/Unknown"]` for invalid or unrecognised numbers.

Use `GetTimeZonesForGeographicalNumber` instead if you have already validated the number and want to skip the internal type check.

### Get the carrier name for a phone number
```csharp
using PhoneNumbers;

var phoneNumberUtil = PhoneNumberUtil.GetInstance();
var carrierMapper = PhoneNumberToCarrierMapper.GetInstance();
var phoneNumber = phoneNumberUtil.Parse("+917503397672", null);
var carrierName = carrierMapper.GetNameForNumber(phoneNumber, Locale.English);

Console.WriteLine(carrierName); // Aircel
```

> **Note:** Carrier data reflects the original network allocation. If the country supports mobile number portability, the number may have since moved to a different carrier. Use `GetSafeDisplayName` to return an empty string in those regions.

## Features

* Parsing/formatting/validating phone numbers for all countries/regions of the world.
* GetNumberType - gets the type of the number based on the number itself; able to distinguish Fixed-line, Mobile, Toll-free, Premium Rate, Shared Cost, VoIP and Personal Numbers (whenever feasible).
* IsNumberMatch - gets a confidence level on whether two numbers could be the same.
* GetExampleNumber/GetExampleNumberByType - provides valid example numbers for 218 countries/regions, with the option of specifying which type of example phone number is needed.
* IsPossibleNumber - quickly guessing whether a number is a possible phone number by using only the length information, much faster than a full validation.
* AsYouTypeFormatter - formats phone numbers on-the-fly when users enter each digit.
* FindNumbers - finds numbers in text input
* PhoneNumberToCarrierMapper - looks up the carrier name originally assigned to a mobile or pager number, with locale-aware output and a safe-display mode for regions with mobile number portability.

See [PhoneNumberUtil.cs](csharp/PhoneNumbers/PhoneNumberUtil.cs) for the various methods and properties available.

## Why keep libphonenumber-csharp up to date?
A lot of the functionality depends on updated metadata that is published by the google repository, see example [here](https://github.com/google/libphonenumber/releases/tag/v8.13.55).

This means that if you don't keep the package up to date, methods like `IsValidNumber` will return false for newer numbers that rely on the updated metadata

Therefore, we recommend you keep this nuget package as up to date as possible using automated means (such as dependabot) as metadata changes published by the google repository is frequent, usually a few times a month.

For more information on metadata usage, please refer to the [main repository faq](https://github.com/google/libphonenumber/blob/master/FAQ.md#metadata)

## ToDo

* update / add / port new unit tests and logging from java source

## How to unfold automatic generated files

* Install Jetbrains - Resharper for Visual Studio
* File by file, right click and "Cleanup code"
* Check the unfolded file

## Running tests locally

```bash
dotnet test csharp/PhoneNumbers.sln
```

## Contributing
See [CONTRIBUTING.md](CONTRIBUTING.md)

## Donations

[![Buy me a beer](https://raw.githubusercontent.com/twcclegg/libphonenumber-csharp/main/bmacButton.png)](https://www.buymeacoffee.com/tclegg)

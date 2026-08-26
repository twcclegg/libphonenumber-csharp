## Confirmed metadata issues

This list is exhaustive: every issue confirmed, by reading its actual
resolution, to be a report about upstream Google libphonenumber metadata
(a prefix, numbering plan, or country code) rather than this port's code.
There is no separate "not metadata" list — absence from this file *is*
the negative signal.

This list is kept up to date automatically: when the triage workflow
closes a new issue as a metadata issue, it opens a PR appending it here.

- #2 "The phone number metadata xml is out-of-date" — literally a stale-metadata report.
- #9 "Metadata 7.4.1" — Brazilian numbering plan (9-prefix rollout) was out of date.
- #14 "Update To 7.7.4" — metadata version bump request.
- #25 "Personal numbering" — Spanish personal-number length rule; matches Google's own demo.
- #26 "Support for 833 toll free area code" — new NANP toll-free code, numbering-plan addition.
- #39 "Leading 0 causes incorrect parsing" — resolved by a metadata release.
- #49 "New Longer NZ Mobile failing validation" — matches Google's own demo; reporter filed with Google directly.
- #62 "Support for 0516 numbers in TR" — resolved by upgrading to a release with newer metadata.
- #69 "IsValidNumber return false for valid Solomon Islands phone numbers" — matches Google's own demo.
- #72 "Parsing numbers with leading zeros" — validity of the numbers in question is metadata-controlled.
- #91 "Vietnamese Phone Numbers Not Returning as Valid" — resolved by a release carrying newer metadata.
- #94 "Manila phone numbers extending to 8 digits" — numbering-plan change pending upstream.
- #100 "Australian phone number beginning with 0460 not recognised as valid" — matches Google's own demo; resolved by a later metadata release.
- #101 "Dutch 097 numbers not recognized" — matches Google's own demo (TOO_LONG).
- #104 "Australian phone numbers starting with 048 not recognised as valid" — resolved by upgrading to a release with newer metadata.
- #109 "Letters in number are not recognized and no exception is thrown." — a port-parity request pointed upstream; e164 output already matched.
- #112 "Australian phone number beginning with 0480 not recognised as valid" — resolved by upgrading to a release with newer metadata.
- #113 "Some New Zealand Mobile Numbers Failing" — matches Google's own demo; resolved by a later metadata release.
- #131 "French IoT 15-digit phone numbers" — Google controls whether this numbering range is supported at all.
- #133 "Some phone number return wrong results for CN" — matches Google's own demo.
- #134 "Issue in verifying some Zimbabwe Econet phone numbers" — matches Google's own demo.
- #135 "MAX_LENGTH_FOR_NSN difference" — a numbering-plan constant that had drifted from upstream.
- #137 "Some Uruguay phones numbers are returned invalid" — matches Google's own demo; resolved by a later metadata release.
- #139 "Library not handling 11 digit NL phone numbers" — reproduces in Google's own library.
- #140 "Library invalidating AU number" — resolved by upgrading to a release with newer metadata.
- #143 "Required to update US phone number code 627" — new area code, metadata-controlled.
- #145 "Get Supported Regions returns invalid regions" — mirrors Google's own CountryCodeToRegionCodeMap.
- #149 "Outdated Version. Next Update?" — resolved once metadata was brought up to date.
- #153 "Outdated MetaData version" — resolved by a metadata release.
- #159 "Some French numbers returns false for IsValidNumber" — Martinique/overseas-region numbering overlap; metadata-controlled.
- #165 "Country Code is added to NationalNumber in PhoneNumber Object if Number is too short" — reproduces in Google's own demo; metadata-controlled.
- #173 "New Kazakhstan country code." — new country code, metadata-controlled.
- #181 "IsPossibleNumberForTypeWithReason returns valid mobile when IsValidNumber returns invalid" — matches Google's own demo.
- #182 "Valid US number is resulting in IsValid to false" — Google's demo showed it valid; resolved once this port's metadata caught up.
- #213 "Vietnamese phone number does not validate" — matches Google's own demo.
- #214 "French mobile numbers with prefix \"07\" are not detected as mobile numbers" — matches Google's own demo.
- #259 "New regulation for phone number prefixes in Australia" — new prefix regulation, metadata-controlled.
- #272 "New phone number format for Benin not supported" — new numbering-plan format, fixed by a later metadata release.
- #313 "SA mobile numbers starting with 579 are rejected by IsValidNumber (IsPossibleNumber=true)" — behavior matches Google's own demo for that prefix.

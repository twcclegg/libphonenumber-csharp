#nullable disable
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace PhoneNumbers
{
    public sealed class PhoneMetadata
    {
        public const int GeneralDescFieldNumber = 1;
        public const int FixedLineFieldNumber = 2;
        public const int MobileFieldNumber = 3;
        public const int TollFreeFieldNumber = 4;
        public const int PremiumRateFieldNumber = 5;
        public const int SharedCostFieldNumber = 6;
        public const int PersonalNumberFieldNumber = 7;
        public const int VoipFieldNumber = 8;
        public const int PagerFieldNumber = 21;
        public const int UanFieldNumber = 25;
        public const int EmergencyFieldNumber = 27;
        public const int VoicemailFieldNumber = 28;
        public const int ShortCodeFieldNumber = 29;
        public const int StandardRateFieldNumber = 30;
        public const int CarrierSpecificFieldNumber = 31;
        public const int SmsServicesFieldNumber = 33;
        public const int NoInternationalDiallingFieldNumber = 24;
        public const int IdFieldNumber = 9;
        public const int CountryCodeFieldNumber = 10;
        public const int InternationalPrefixFieldNumber = 11;
        public const int PreferredInternationalPrefixFieldNumber = 17;
        public const int NationalPrefixFieldNumber = 12;
        public const int PreferredExtnPrefixFieldNumber = 13;
        public const int NationalPrefixForParsingFieldNumber = 15;
        public const int NationalPrefixTransformRuleFieldNumber = 16;
        public const int SameMobileAndFixedLinePatternFieldNumber = 18;
        public const int NumberFormatFieldNumber = 19;
        public const int IntlNumberFormatFieldNumber = 20;
        public const int MainCountryForCodeFieldNumber = 22;
        public const int LeadingDigitsFieldNumber = 23;
        public const int LeadingZeroPossibleFieldNumber = 26;
        public const int MobileNumberPortableRegionFieldNumber = 32;

        internal readonly List<NumberFormat> intlNumberFormat_ = new List<NumberFormat>();
        internal readonly List<NumberFormat> numberFormat_ = new List<NumberFormat>();

        public static PhoneMetadata DefaultInstance { get; } = new();

        public PhoneMetadata DefaultInstanceForType => DefaultInstance;

        public bool HasGeneralDesc => GeneralDesc != PhoneNumberDesc.DefaultInstance;
        public PhoneNumberDesc GeneralDesc { get; internal set; } = PhoneNumberDesc.DefaultInstance;

        public bool HasFixedLine => FixedLine != PhoneNumberDesc.DefaultInstance;
        public PhoneNumberDesc FixedLine { get; internal set; } = PhoneNumberDesc.DefaultInstance;

        public bool HasMobile => Mobile != PhoneNumberDesc.DefaultInstance;
        public PhoneNumberDesc Mobile { get; internal set; } = PhoneNumberDesc.DefaultInstance;

        public bool HasTollFree => TollFree != PhoneNumberDesc.DefaultInstance;
        public PhoneNumberDesc TollFree { get; internal set; } = PhoneNumberDesc.DefaultInstance;

        public bool HasPremiumRate => PremiumRate != PhoneNumberDesc.DefaultInstance;
        public PhoneNumberDesc PremiumRate { get; internal set; } = PhoneNumberDesc.DefaultInstance;

        public bool HasSharedCost => SharedCost != PhoneNumberDesc.DefaultInstance;
        public PhoneNumberDesc SharedCost { get; internal set; } = PhoneNumberDesc.DefaultInstance;

        public bool HasPersonalNumber => PersonalNumber != PhoneNumberDesc.DefaultInstance;
        public PhoneNumberDesc PersonalNumber { get; internal set; } = PhoneNumberDesc.DefaultInstance;

        public bool HasVoip => Voip != PhoneNumberDesc.DefaultInstance;
        public PhoneNumberDesc Voip { get; internal set; } = PhoneNumberDesc.DefaultInstance;

        public bool HasPager => Pager != PhoneNumberDesc.DefaultInstance;
        public PhoneNumberDesc Pager { get; internal set; } = PhoneNumberDesc.DefaultInstance;

        public bool HasUan => Uan != PhoneNumberDesc.DefaultInstance;
        public PhoneNumberDesc Uan { get; internal set; } = PhoneNumberDesc.DefaultInstance;

        public bool HasEmergency => Emergency != PhoneNumberDesc.DefaultInstance;
        public PhoneNumberDesc Emergency { get; internal set; } = PhoneNumberDesc.DefaultInstance;

        public bool HasVoicemail => Voicemail != PhoneNumberDesc.DefaultInstance;
        public PhoneNumberDesc Voicemail { get; internal set; } = PhoneNumberDesc.DefaultInstance;

        public bool HasShortCode => ShortCode != PhoneNumberDesc.DefaultInstance;
        public PhoneNumberDesc ShortCode { get; internal set; } = PhoneNumberDesc.DefaultInstance;

        public bool HasStandardRate => StandardRate != PhoneNumberDesc.DefaultInstance;
        public PhoneNumberDesc StandardRate { get; internal set; } = PhoneNumberDesc.DefaultInstance;

        public bool HasCarrierSpecific => CarrierSpecific != PhoneNumberDesc.DefaultInstance;
        public PhoneNumberDesc CarrierSpecific { get; internal set; } = PhoneNumberDesc.DefaultInstance;

        public bool HasSmsServices => SmsServices != PhoneNumberDesc.DefaultInstance;
        public PhoneNumberDesc SmsServices { get; internal set; } = PhoneNumberDesc.DefaultInstance;

        public bool HasNoInternationalDialling => NoInternationalDialling != PhoneNumberDesc.DefaultInstance;
        public PhoneNumberDesc NoInternationalDialling { get; internal set; } = PhoneNumberDesc.DefaultInstance;

        public bool HasId => Id?.Length > 0;
        public string Id { get; internal set; } = "";

        public bool HasCountryCode => CountryCode != 0;
        public int CountryCode { get; internal set; }

        public bool HasInternationalPrefix => InternationalPrefix?.Length > 0;
        public string InternationalPrefix { get; internal set; } = "";

        public bool HasPreferredInternationalPrefix => PreferredInternationalPrefix?.Length > 0;
        public string PreferredInternationalPrefix { get; internal set; } = "";

        public bool HasNationalPrefix => NationalPrefix?.Length > 0;
        public string NationalPrefix { get; internal set; } = "";

        public bool HasPreferredExtnPrefix => PreferredExtnPrefix?.Length > 0;
        public string PreferredExtnPrefix { get; internal set; } = "";

        public bool HasNationalPrefixForParsing => NationalPrefixForParsing?.Length > 0;
        public string NationalPrefixForParsing { get; internal set; } = "";

        private sbyte _nationalPrefixForParsingLiteral;
        internal Match MatchNationalPrefixForParsing(string value)
        {
            if (_nationalPrefixForParsingLiteral == 0)
                _nationalPrefixForParsingLiteral = (sbyte)(Regex.Escape(NationalPrefixForParsing) == NationalPrefixForParsing ? 1 : -1);

            if (_nationalPrefixForParsingLiteral > 0
                && !value.StartsWith(NationalPrefixForParsing, StringComparison.Ordinal))
                return null;

            return PhoneRegex.Get(NationalPrefixForParsing).MatchBeginning(value);
        }

        public bool HasNationalPrefixTransformRule => NationalPrefixTransformRule?.Length > 0;
        public string NationalPrefixTransformRule { get; internal set; } = "";

        public bool HasSameMobileAndFixedLinePattern => SameMobileAndFixedLinePattern;
        public bool SameMobileAndFixedLinePattern { get; internal set; }

        public IList<NumberFormat> NumberFormatList => numberFormat_;

        public int NumberFormatCount => numberFormat_.Count;

        public IList<NumberFormat> IntlNumberFormatList => intlNumberFormat_;

        public int IntlNumberFormatCount => intlNumberFormat_.Count;

        public bool HasMainCountryForCode => MainCountryForCode;
        public bool MainCountryForCode { get; internal set; }

        public bool HasLeadingDigits => LeadingDigits?.Length > 0;
        public string LeadingDigits { get; internal set; } = "";

        private sbyte _leadingDigitsLiteral;
        internal bool IsMatchLeadingDigits(string value)
        {
            if (_leadingDigitsLiteral == 0)
                _leadingDigitsLiteral = (sbyte)(Regex.Escape(LeadingDigits) == LeadingDigits ? 1 : -1);

            return _leadingDigitsLiteral > 0
                ? value.StartsWith(LeadingDigits, StringComparison.Ordinal)
                : PhoneRegex.Get(LeadingDigits).IsMatchBeginning(value);
        }

        public bool HasMobileNumberPortableRegion => MobileNumberPortableRegion;
        public bool MobileNumberPortableRegion { get; internal set; }

        public bool IsInitialized
        {
            get
            {
                if (!HasId) return false;
                return NumberFormatList.All(element => element.IsInitialized) && IntlNumberFormatList.All(element => element.IsInitialized);
            }
        }

        public NumberFormat GetNumberFormat(int index) => numberFormat_[index];

        public NumberFormat GetIntlNumberFormat(int index) => intlNumberFormat_[index];

        public static Builder CreateBuilder() => new Builder();

        public Builder ToBuilder() => CreateBuilder(this);

        public Builder CreateBuilderForType() => new Builder();

        public static Builder CreateBuilder(PhoneMetadata prototype) => new Builder().MergeFrom(prototype);

        [DebuggerNonUserCode]
        [CompilerGenerated]
        [GeneratedCode("ProtoGen", "2.3.0.277")]
        public class Builder
        {
            protected Builder ThisBuilder => this;

            internal protected PhoneMetadata MessageBeingBuilt { get; private set; }

            public Builder() => MessageBeingBuilt = new();
            internal Builder(PhoneMetadata desc) => MessageBeingBuilt = desc;

            public PhoneMetadata DefaultInstanceForType => DefaultInstance;


            public bool HasGeneralDesc => MessageBeingBuilt.HasGeneralDesc;

            public PhoneNumberDesc GeneralDesc
            {
                get => MessageBeingBuilt.GeneralDesc;
                set => SetGeneralDesc(value);
            }

            public bool HasFixedLine => MessageBeingBuilt.HasFixedLine;

            public PhoneNumberDesc FixedLine
            {
                get => MessageBeingBuilt.FixedLine;
                set => SetFixedLine(value);
            }

            public bool HasMobile => MessageBeingBuilt.HasMobile;

            public PhoneNumberDesc Mobile
            {
                get => MessageBeingBuilt.Mobile;
                set => SetMobile(value);
            }

            public bool HasTollFree => MessageBeingBuilt.HasTollFree;

            public PhoneNumberDesc TollFree
            {
                get => MessageBeingBuilt.TollFree;
                set => SetTollFree(value);
            }

            public bool HasPremiumRate => MessageBeingBuilt.HasPremiumRate;

            public PhoneNumberDesc PremiumRate
            {
                get => MessageBeingBuilt.PremiumRate;
                set => SetPremiumRate(value);
            }

            public bool HasSharedCost => MessageBeingBuilt.HasSharedCost;

            public PhoneNumberDesc SharedCost
            {
                get => MessageBeingBuilt.SharedCost;
                set => SetSharedCost(value);
            }

            public bool HasPersonalNumber => MessageBeingBuilt.HasPersonalNumber;

            public PhoneNumberDesc PersonalNumber
            {
                get => MessageBeingBuilt.PersonalNumber;
                set => SetPersonalNumber(value);
            }

            public bool HasVoip => MessageBeingBuilt.HasVoip;

            public PhoneNumberDesc Voip
            {
                get => MessageBeingBuilt.Voip;
                set => SetVoip(value);
            }

            public bool HasPager => MessageBeingBuilt.HasPager;

            public PhoneNumberDesc Pager
            {
                get => MessageBeingBuilt.Pager;
                set => SetPager(value);
            }

            public bool HasUan => MessageBeingBuilt.HasUan;

            public PhoneNumberDesc Uan
            {
                get => MessageBeingBuilt.Uan;
                set => SetUan(value);
            }

            public bool HasEmergency => MessageBeingBuilt.HasEmergency;

            public PhoneNumberDesc Emergency
            {
                get => MessageBeingBuilt.Emergency;
                set => SetEmergency(value);
            }

            public bool HasVoicemail => MessageBeingBuilt.HasVoicemail;

            public PhoneNumberDesc Voicemail
            {
                get => MessageBeingBuilt.Voicemail;
                set => SetVoicemail(value);
            }

            public bool HasShortCode => MessageBeingBuilt.HasShortCode;

            public PhoneNumberDesc ShortCode
            {
                get => MessageBeingBuilt.ShortCode;
                set => SetShortCode(value);
            }

            public bool HasStandardRate => MessageBeingBuilt.HasStandardRate;

            public PhoneNumberDesc StandardRate
            {
                get => MessageBeingBuilt.StandardRate;
                set => SetStandardRate(value);
            }

            public bool HasCarrierSpecific => MessageBeingBuilt.HasCarrierSpecific;

            public PhoneNumberDesc CarrierSpecific
            {
                get => MessageBeingBuilt.CarrierSpecific;
                set => SetCarrierSpecific(value);
            }

            public bool HasSmsServices => MessageBeingBuilt.HasSmsServices;

            public PhoneNumberDesc SmsServices
            {
                get => MessageBeingBuilt.SmsServices;
                set => SetSmsServices(value);
            }

            public bool HasNoInternationalDialling => MessageBeingBuilt.HasNoInternationalDialling;

            public PhoneNumberDesc NoInternationalDialling
            {
                get => MessageBeingBuilt.NoInternationalDialling;
                set => SetNoInternationalDialling(value);
            }

            public bool HasId => MessageBeingBuilt.HasId;

            public string Id
            {
                get => MessageBeingBuilt.Id;
                set => SetId(value);
            }

            public bool HasCountryCode => MessageBeingBuilt.HasCountryCode;

            public int CountryCode
            {
                get => MessageBeingBuilt.CountryCode;
                set => SetCountryCode(value);
            }

            public bool HasInternationalPrefix => MessageBeingBuilt.HasInternationalPrefix;

            public string InternationalPrefix
            {
                get => MessageBeingBuilt.InternationalPrefix;
                set => SetInternationalPrefix(value);
            }

            public bool HasPreferredInternationalPrefix => MessageBeingBuilt.HasPreferredInternationalPrefix;

            public string PreferredInternationalPrefix
            {
                get => MessageBeingBuilt.PreferredInternationalPrefix;
                set => SetPreferredInternationalPrefix(value);
            }

            public bool HasNationalPrefix => MessageBeingBuilt.HasNationalPrefix;

            public string NationalPrefix
            {
                get => MessageBeingBuilt.NationalPrefix;
                set => SetNationalPrefix(value);
            }

            public bool HasPreferredExtnPrefix => MessageBeingBuilt.HasPreferredExtnPrefix;

            public string PreferredExtnPrefix
            {
                get => MessageBeingBuilt.PreferredExtnPrefix;
                set => SetPreferredExtnPrefix(value);
            }

            public bool HasNationalPrefixForParsing => MessageBeingBuilt.HasNationalPrefixForParsing;

            public string NationalPrefixForParsing
            {
                get => MessageBeingBuilt.NationalPrefixForParsing;
                set => SetNationalPrefixForParsing(value);
            }

            public bool HasNationalPrefixTransformRule => MessageBeingBuilt.HasNationalPrefixTransformRule;

            public string NationalPrefixTransformRule
            {
                get => MessageBeingBuilt.NationalPrefixTransformRule;
                set => SetNationalPrefixTransformRule(value);
            }

            public bool HasSameMobileAndFixedLinePattern => MessageBeingBuilt.HasSameMobileAndFixedLinePattern;

            public bool SameMobileAndFixedLinePattern
            {
                get => MessageBeingBuilt.SameMobileAndFixedLinePattern;
                set => SetSameMobileAndFixedLinePattern(value);
            }

            public IList<NumberFormat> NumberFormatList => MessageBeingBuilt.numberFormat_;

            public int NumberFormatCount => MessageBeingBuilt.NumberFormatCount;

            public IList<NumberFormat> IntlNumberFormatList => MessageBeingBuilt.intlNumberFormat_;

            public int IntlNumberFormatCount => MessageBeingBuilt.IntlNumberFormatCount;

            public bool HasMainCountryForCode => MessageBeingBuilt.HasMainCountryForCode;

            public bool MainCountryForCode
            {
                get => MessageBeingBuilt.MainCountryForCode;
                set => SetMainCountryForCode(value);
            }

            public bool HasLeadingDigits => MessageBeingBuilt.HasLeadingDigits;

            public string LeadingDigits
            {
                get => MessageBeingBuilt.LeadingDigits;
                set => SetLeadingDigits(value);
            }

            public bool HasMobileNumberPortableRegion => MessageBeingBuilt.HasMobileNumberPortableRegion;

            public bool MobileNumberPortableRegion
            {
                get => MessageBeingBuilt.MobileNumberPortableRegion;
                set => SetMobileNumberPortableRegion(value);
            }

            public Builder Clear()
            {
                MessageBeingBuilt = new PhoneMetadata();
                return this;
            }

            public Builder Clone()
            {
                return new Builder().MergeFrom(MessageBeingBuilt);
            }

            public PhoneMetadata Build()
            {
                return BuildPartial();
            }

            public PhoneMetadata BuildPartial()
            {
                if (MessageBeingBuilt == null)
                    throw new InvalidOperationException("build() has already been called on this Builder");


                var returnMe = MessageBeingBuilt;
                MessageBeingBuilt = null;
                return returnMe;
            }


            public Builder MergeFrom(PhoneMetadata other)
            {
                if (other == DefaultInstance) return this;
                if (other.HasGeneralDesc) MergeGeneralDesc(other.GeneralDesc);
                if (other.HasFixedLine) MergeFixedLine(other.FixedLine);
                if (other.HasMobile) MergeMobile(other.Mobile);
                if (other.HasTollFree) MergeTollFree(other.TollFree);
                if (other.HasPremiumRate) MergePremiumRate(other.PremiumRate);
                if (other.HasSharedCost) MergeSharedCost(other.SharedCost);
                if (other.HasPersonalNumber) MergePersonalNumber(other.PersonalNumber);
                if (other.HasVoip) MergeVoip(other.Voip);
                if (other.HasPager) MergePager(other.Pager);
                if (other.HasUan) MergeUan(other.Uan);
                if (other.HasEmergency) MergeEmergency(other.Emergency);
                if (other.HasVoicemail) MergeVoicemail(other.Voicemail);
                if (other.HasShortCode) MergeShortCode(other.ShortCode);
                if (other.HasStandardRate) MergeStandardRate(other.StandardRate);
                if (other.HasCarrierSpecific) MergeCarrierSpecific(other.CarrierSpecific);
                if (other.HasSmsServices) MergeSmsServices(other.SmsServices);
                if (other.HasNoInternationalDialling) MergeNoInternationalDialling(other.NoInternationalDialling);
                if (other.HasId) Id = other.Id;
                if (other.HasCountryCode) CountryCode = other.CountryCode;
                if (other.HasInternationalPrefix) InternationalPrefix = other.InternationalPrefix;
                if (other.HasPreferredInternationalPrefix)
                    PreferredInternationalPrefix = other.PreferredInternationalPrefix;
                if (other.HasNationalPrefix) NationalPrefix = other.NationalPrefix;
                if (other.HasPreferredExtnPrefix) PreferredExtnPrefix = other.PreferredExtnPrefix;
                if (other.HasNationalPrefixForParsing) NationalPrefixForParsing = other.NationalPrefixForParsing;
                if (other.HasNationalPrefixTransformRule)
                    NationalPrefixTransformRule = other.NationalPrefixTransformRule;
                if (other.HasSameMobileAndFixedLinePattern)
                    SameMobileAndFixedLinePattern = other.SameMobileAndFixedLinePattern;
                if (other.numberFormat_.Count != 0) MessageBeingBuilt.numberFormat_.AddRange(other.numberFormat_);
                if (other.intlNumberFormat_.Count != 0)
                    MessageBeingBuilt.intlNumberFormat_.AddRange(other.intlNumberFormat_);
                if (other.HasMainCountryForCode) MainCountryForCode = other.MainCountryForCode;
                if (other.HasLeadingDigits) LeadingDigits = other.LeadingDigits;
                if (other.HasMobileNumberPortableRegion) MobileNumberPortableRegion = other.MobileNumberPortableRegion;
                return this;
            }

            public Builder SetGeneralDesc(PhoneNumberDesc value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.GeneralDesc = value;
                return this;
            }

            public Builder SetGeneralDesc(PhoneNumberDesc.Builder builderForValue)
            {
                if (builderForValue == null) throw new ArgumentNullException(nameof(builderForValue));
                MessageBeingBuilt.GeneralDesc = builderForValue.Build();
                return this;
            }

            public Builder MergeGeneralDesc(PhoneNumberDesc value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                if (MessageBeingBuilt.HasGeneralDesc &&
                    MessageBeingBuilt.GeneralDesc != PhoneNumberDesc.DefaultInstance)
                    MessageBeingBuilt.GeneralDesc = PhoneNumberDesc.CreateBuilder(MessageBeingBuilt.GeneralDesc)
                        .MergeFrom(value).BuildPartial();
                else MessageBeingBuilt.GeneralDesc = value;
                return this;
            }

            public Builder ClearGeneralDesc()
            {
                MessageBeingBuilt.GeneralDesc = PhoneNumberDesc.DefaultInstance;
                return this;
            }

            public Builder SetFixedLine(PhoneNumberDesc value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.FixedLine = value;
                return this;
            }

            public Builder SetFixedLine(PhoneNumberDesc.Builder builderForValue)
            {
                if (builderForValue == null) throw new ArgumentNullException(nameof(builderForValue));
                MessageBeingBuilt.FixedLine = builderForValue.Build();
                return this;
            }

            public Builder MergeFixedLine(PhoneNumberDesc value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                if (MessageBeingBuilt.HasFixedLine &&
                    MessageBeingBuilt.FixedLine != PhoneNumberDesc.DefaultInstance)
                    MessageBeingBuilt.FixedLine = PhoneNumberDesc.CreateBuilder(MessageBeingBuilt.FixedLine)
                        .MergeFrom(value).BuildPartial();
                else MessageBeingBuilt.FixedLine = value;
                return this;
            }

            public Builder ClearFixedLine()
            {
                MessageBeingBuilt.FixedLine = PhoneNumberDesc.DefaultInstance;
                return this;
            }

            public Builder SetMobile(PhoneNumberDesc value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.Mobile = value;
                return this;
            }

            public Builder SetMobile(PhoneNumberDesc.Builder builderForValue)
            {
                if (builderForValue == null) throw new ArgumentNullException(nameof(builderForValue));
                MessageBeingBuilt.Mobile = builderForValue.Build();
                return this;
            }

            public Builder MergeMobile(PhoneNumberDesc value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                if (MessageBeingBuilt.HasMobile &&
                    MessageBeingBuilt.Mobile != PhoneNumberDesc.DefaultInstance)
                    MessageBeingBuilt.Mobile = PhoneNumberDesc.CreateBuilder(MessageBeingBuilt.Mobile).MergeFrom(value)
                        .BuildPartial();
                else MessageBeingBuilt.Mobile = value;
                return this;
            }

            public Builder ClearMobile()
            {
                MessageBeingBuilt.Mobile = PhoneNumberDesc.DefaultInstance;
                return this;
            }

            public Builder SetTollFree(PhoneNumberDesc value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.TollFree = value;
                return this;
            }

            public Builder SetTollFree(PhoneNumberDesc.Builder builderForValue)
            {
                if (builderForValue == null) throw new ArgumentNullException(nameof(builderForValue));
                MessageBeingBuilt.TollFree = builderForValue.Build();
                return this;
            }

            public Builder MergeTollFree(PhoneNumberDesc value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                if (MessageBeingBuilt.HasTollFree &&
                    MessageBeingBuilt.TollFree != PhoneNumberDesc.DefaultInstance)
                    MessageBeingBuilt.TollFree = PhoneNumberDesc.CreateBuilder(MessageBeingBuilt.TollFree)
                        .MergeFrom(value).BuildPartial();
                else MessageBeingBuilt.TollFree = value;
                return this;
            }

            public Builder ClearTollFree()
            {
                MessageBeingBuilt.TollFree = PhoneNumberDesc.DefaultInstance;
                return this;
            }

            public Builder SetPremiumRate(PhoneNumberDesc value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.PremiumRate = value;
                return this;
            }

            public Builder SetPremiumRate(PhoneNumberDesc.Builder builderForValue)
            {
                if (builderForValue == null) throw new ArgumentNullException(nameof(builderForValue));
                MessageBeingBuilt.PremiumRate = builderForValue.Build();
                return this;
            }

            public Builder MergePremiumRate(PhoneNumberDesc value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                if (MessageBeingBuilt.HasPremiumRate &&
                    MessageBeingBuilt.PremiumRate != PhoneNumberDesc.DefaultInstance)
                    MessageBeingBuilt.PremiumRate = PhoneNumberDesc.CreateBuilder(MessageBeingBuilt.PremiumRate)
                        .MergeFrom(value).BuildPartial();
                else MessageBeingBuilt.PremiumRate = value;
                return this;
            }

            public Builder ClearPremiumRate()
            {
                MessageBeingBuilt.PremiumRate = PhoneNumberDesc.DefaultInstance;
                return this;
            }

            public Builder SetSharedCost(PhoneNumberDesc value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.SharedCost = value;
                return this;
            }

            public Builder SetSharedCost(PhoneNumberDesc.Builder builderForValue)
            {
                if (builderForValue == null) throw new ArgumentNullException(nameof(builderForValue));
                MessageBeingBuilt.SharedCost = builderForValue.Build();
                return this;
            }

            public Builder MergeSharedCost(PhoneNumberDesc value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                if (MessageBeingBuilt.HasSharedCost &&
                    MessageBeingBuilt.SharedCost != PhoneNumberDesc.DefaultInstance)
                    MessageBeingBuilt.SharedCost = PhoneNumberDesc.CreateBuilder(MessageBeingBuilt.SharedCost)
                        .MergeFrom(value).BuildPartial();
                else MessageBeingBuilt.SharedCost = value;
                return this;
            }

            public Builder ClearSharedCost()
            {
                MessageBeingBuilt.SharedCost = PhoneNumberDesc.DefaultInstance;
                return this;
            }

            public Builder SetPersonalNumber(PhoneNumberDesc value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.PersonalNumber = value;
                return this;
            }

            public Builder SetPersonalNumber(PhoneNumberDesc.Builder builderForValue)
            {
                if (builderForValue == null) throw new ArgumentNullException(nameof(builderForValue));
                MessageBeingBuilt.PersonalNumber = builderForValue.Build();
                return this;
            }

            public Builder MergePersonalNumber(PhoneNumberDesc value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                if (MessageBeingBuilt.HasPersonalNumber &&
                    MessageBeingBuilt.PersonalNumber != PhoneNumberDesc.DefaultInstance)
                    MessageBeingBuilt.PersonalNumber = PhoneNumberDesc.CreateBuilder(MessageBeingBuilt.PersonalNumber)
                        .MergeFrom(value).BuildPartial();
                else MessageBeingBuilt.PersonalNumber = value;
                return this;
            }

            public Builder ClearPersonalNumber()
            {
                MessageBeingBuilt.PersonalNumber = PhoneNumberDesc.DefaultInstance;
                return this;
            }

            public Builder SetVoip(PhoneNumberDesc value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.Voip = value;
                return this;
            }

            public Builder SetVoip(PhoneNumberDesc.Builder builderForValue)
            {
                if (builderForValue == null) throw new ArgumentNullException(nameof(builderForValue));
                MessageBeingBuilt.Voip = builderForValue.Build();
                return this;
            }

            public Builder MergeVoip(PhoneNumberDesc value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                if (MessageBeingBuilt.HasVoip &&
                    MessageBeingBuilt.Voip != PhoneNumberDesc.DefaultInstance)
                    MessageBeingBuilt.Voip = PhoneNumberDesc.CreateBuilder(MessageBeingBuilt.Voip).MergeFrom(value)
                        .BuildPartial();
                else MessageBeingBuilt.Voip = value;
                return this;
            }

            public Builder ClearVoip()
            {
                MessageBeingBuilt.Voip = PhoneNumberDesc.DefaultInstance;
                return this;
            }

            public Builder SetPager(PhoneNumberDesc value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.Pager = value;
                return this;
            }

            public Builder SetPager(PhoneNumberDesc.Builder builderForValue)
            {
                if (builderForValue == null) throw new ArgumentNullException(nameof(builderForValue));
                MessageBeingBuilt.Pager = builderForValue.Build();
                return this;
            }

            public Builder MergePager(PhoneNumberDesc value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                if (MessageBeingBuilt.HasPager &&
                    MessageBeingBuilt.Pager != PhoneNumberDesc.DefaultInstance)
                    MessageBeingBuilt.Pager = PhoneNumberDesc.CreateBuilder(MessageBeingBuilt.Pager).MergeFrom(value)
                        .BuildPartial();
                else MessageBeingBuilt.Pager = value;
                return this;
            }

            public Builder ClearPager()
            {
                MessageBeingBuilt.Pager = PhoneNumberDesc.DefaultInstance;
                return this;
            }

            public Builder SetUan(PhoneNumberDesc value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.Uan = value;
                return this;
            }

            public Builder SetUan(PhoneNumberDesc.Builder builderForValue)
            {
                if (builderForValue == null) throw new ArgumentNullException(nameof(builderForValue));
                MessageBeingBuilt.Uan = builderForValue.Build();
                return this;
            }

            public Builder MergeUan(PhoneNumberDesc value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                if (MessageBeingBuilt.HasUan &&
                    MessageBeingBuilt.Uan != PhoneNumberDesc.DefaultInstance)
                    MessageBeingBuilt.Uan = PhoneNumberDesc.CreateBuilder(MessageBeingBuilt.Uan).MergeFrom(value)
                        .BuildPartial();
                else MessageBeingBuilt.Uan = value;
                return this;
            }

            public Builder ClearUan()
            {
                MessageBeingBuilt.Uan = PhoneNumberDesc.DefaultInstance;
                return this;
            }

            public Builder SetEmergency(PhoneNumberDesc value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.Emergency = value;
                return this;
            }

            public Builder SetEmergency(PhoneNumberDesc.Builder builderForValue)
            {
                if (builderForValue == null) throw new ArgumentNullException(nameof(builderForValue));
                MessageBeingBuilt.Emergency = builderForValue.Build();
                return this;
            }

            public Builder MergeEmergency(PhoneNumberDesc value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                if (MessageBeingBuilt.HasEmergency &&
                    MessageBeingBuilt.Emergency != PhoneNumberDesc.DefaultInstance)
                    MessageBeingBuilt.Emergency = PhoneNumberDesc.CreateBuilder(MessageBeingBuilt.Emergency)
                        .MergeFrom(value).BuildPartial();
                else MessageBeingBuilt.Emergency = value;
                return this;
            }

            public Builder ClearEmergency()
            {
                MessageBeingBuilt.Emergency = PhoneNumberDesc.DefaultInstance;
                return this;
            }

            public Builder SetVoicemail(PhoneNumberDesc value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.Voicemail = value;
                return this;
            }

            public Builder SetVoicemail(PhoneNumberDesc.Builder builderForValue)
            {
                if (builderForValue == null) throw new ArgumentNullException(nameof(builderForValue));
                MessageBeingBuilt.Voicemail = builderForValue.Build();
                return this;
            }

            public Builder MergeVoicemail(PhoneNumberDesc value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                if (MessageBeingBuilt.HasVoicemail &&
                    MessageBeingBuilt.Voicemail != PhoneNumberDesc.DefaultInstance)
                    MessageBeingBuilt.Voicemail = PhoneNumberDesc.CreateBuilder(MessageBeingBuilt.Voicemail)
                        .MergeFrom(value).BuildPartial();
                else MessageBeingBuilt.Voicemail = value;
                return this;
            }

            public Builder ClearVoicemail()
            {
                MessageBeingBuilt.Voicemail = PhoneNumberDesc.DefaultInstance;
                return this;
            }

            public Builder SetShortCode(PhoneNumberDesc value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.ShortCode = value;
                return this;
            }

            public Builder SetShortCode(PhoneNumberDesc.Builder builderForValue)
            {
                if (builderForValue == null) throw new ArgumentNullException(nameof(builderForValue));
                MessageBeingBuilt.ShortCode = builderForValue.Build();
                return this;
            }

            public Builder MergeShortCode(PhoneNumberDesc value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                if (MessageBeingBuilt.HasShortCode &&
                    MessageBeingBuilt.ShortCode != PhoneNumberDesc.DefaultInstance)
                    MessageBeingBuilt.ShortCode = PhoneNumberDesc.CreateBuilder(MessageBeingBuilt.ShortCode)
                        .MergeFrom(value).BuildPartial();
                else MessageBeingBuilt.ShortCode = value;
                return this;
            }

            public Builder ClearShortCode()
            {
                MessageBeingBuilt.ShortCode = PhoneNumberDesc.DefaultInstance;
                return this;
            }

            public Builder SetStandardRate(PhoneNumberDesc value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.StandardRate = value;
                return this;
            }

            public Builder SetStandardRate(PhoneNumberDesc.Builder builderForValue)
            {
                if (builderForValue == null) throw new ArgumentNullException(nameof(builderForValue));
                MessageBeingBuilt.StandardRate = builderForValue.Build();
                return this;
            }

            public Builder MergeStandardRate(PhoneNumberDesc value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                if (MessageBeingBuilt.HasStandardRate &&
                    MessageBeingBuilt.StandardRate != PhoneNumberDesc.DefaultInstance)
                    MessageBeingBuilt.StandardRate = PhoneNumberDesc.CreateBuilder(MessageBeingBuilt.StandardRate)
                        .MergeFrom(value).BuildPartial();
                else MessageBeingBuilt.StandardRate = value;
                return this;
            }

            public Builder ClearStandardRate()
            {
                MessageBeingBuilt.StandardRate = PhoneNumberDesc.DefaultInstance;
                return this;
            }

            public Builder SetCarrierSpecific(PhoneNumberDesc value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.CarrierSpecific = value;
                return this;
            }

            public Builder SetCarrierSpecific(PhoneNumberDesc.Builder builderForValue)
            {
                if (builderForValue == null) throw new ArgumentNullException(nameof(builderForValue));
                MessageBeingBuilt.CarrierSpecific = builderForValue.Build();
                return this;
            }

            public Builder MergeCarrierSpecific(PhoneNumberDesc value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                if (MessageBeingBuilt.HasCarrierSpecific &&
                    MessageBeingBuilt.CarrierSpecific != PhoneNumberDesc.DefaultInstance)
                    MessageBeingBuilt.CarrierSpecific = PhoneNumberDesc.CreateBuilder(MessageBeingBuilt.CarrierSpecific)
                        .MergeFrom(value).BuildPartial();
                else MessageBeingBuilt.CarrierSpecific = value;
                return this;
            }

            public Builder ClearCarrierSpecific()
            {
                MessageBeingBuilt.CarrierSpecific = PhoneNumberDesc.DefaultInstance;
                return this;
            }

            public Builder SetSmsServices(PhoneNumberDesc value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.SmsServices = value;
                return this;
            }

            public Builder SetSmsServices(PhoneNumberDesc.Builder builderForValue)
            {
                if (builderForValue == null) throw new ArgumentNullException(nameof(builderForValue));
                MessageBeingBuilt.SmsServices = builderForValue.Build();
                return this;
            }

            public Builder MergeSmsServices(PhoneNumberDesc value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                if (MessageBeingBuilt.HasSmsServices &&
                    MessageBeingBuilt.SmsServices != PhoneNumberDesc.DefaultInstance)
                    MessageBeingBuilt.SmsServices = PhoneNumberDesc.CreateBuilder(MessageBeingBuilt.SmsServices)
                        .MergeFrom(value).BuildPartial();
                else MessageBeingBuilt.SmsServices = value;
                return this;
            }

            public Builder ClearSmsServices()
            {
                MessageBeingBuilt.SmsServices = PhoneNumberDesc.DefaultInstance;
                return this;
            }

            public Builder SetNoInternationalDialling(PhoneNumberDesc value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.NoInternationalDialling = value;
                return this;
            }

            public Builder SetNoInternationalDialling(PhoneNumberDesc.Builder builderForValue)
            {
                if (builderForValue == null) throw new ArgumentNullException(nameof(builderForValue));
                MessageBeingBuilt.NoInternationalDialling = builderForValue.Build();
                return this;
            }

            public Builder MergeNoInternationalDialling(PhoneNumberDesc value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                if (MessageBeingBuilt.HasNoInternationalDialling &&
                    MessageBeingBuilt.NoInternationalDialling != PhoneNumberDesc.DefaultInstance)
                    MessageBeingBuilt.NoInternationalDialling = PhoneNumberDesc
                        .CreateBuilder(MessageBeingBuilt.NoInternationalDialling).MergeFrom(value).BuildPartial();
                else MessageBeingBuilt.NoInternationalDialling = value;
                return this;
            }

            public Builder ClearNoInternationalDialling()
            {
                MessageBeingBuilt.NoInternationalDialling = PhoneNumberDesc.DefaultInstance;
                return this;
            }

            public Builder SetId(string value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.Id = value;
                return this;
            }

            public Builder ClearId()
            {
                MessageBeingBuilt.Id = "";
                return this;
            }

            public Builder SetCountryCode(int value)
            {
                MessageBeingBuilt.CountryCode = value;
                return this;
            }

            public Builder ClearCountryCode()
            {
                MessageBeingBuilt.CountryCode = 0;
                return this;
            }

            public Builder SetInternationalPrefix(string value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.InternationalPrefix = value;
                return this;
            }

            public Builder ClearInternationalPrefix()
            {
                MessageBeingBuilt.InternationalPrefix = "";
                return this;
            }

            public Builder SetPreferredInternationalPrefix(string value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.PreferredInternationalPrefix = value;
                return this;
            }

            public Builder ClearPreferredInternationalPrefix()
            {
                MessageBeingBuilt.PreferredInternationalPrefix = "";
                return this;
            }

            public Builder SetNationalPrefix(string value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.NationalPrefix = value;
                return this;
            }

            public Builder ClearNationalPrefix()
            {
                MessageBeingBuilt.NationalPrefix = "";
                return this;
            }

            public Builder SetPreferredExtnPrefix(string value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.PreferredExtnPrefix = value;
                return this;
            }

            public Builder ClearPreferredExtnPrefix()
            {
                MessageBeingBuilt.PreferredExtnPrefix = "";
                return this;
            }

            public Builder SetNationalPrefixForParsing(string value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.NationalPrefixForParsing = value;
                return this;
            }

            public Builder ClearNationalPrefixForParsing()
            {
                MessageBeingBuilt.NationalPrefixForParsing = "";
                return this;
            }

            public Builder SetNationalPrefixTransformRule(string value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.NationalPrefixTransformRule = value;
                return this;
            }

            public Builder ClearNationalPrefixTransformRule()
            {
                MessageBeingBuilt.NationalPrefixTransformRule = "";
                return this;
            }

            public Builder SetSameMobileAndFixedLinePattern(bool value)
            {
                MessageBeingBuilt.SameMobileAndFixedLinePattern = value;
                return this;
            }

            public Builder ClearSameMobileAndFixedLinePattern()
            {
                MessageBeingBuilt.SameMobileAndFixedLinePattern = false;
                return this;
            }

            public NumberFormat GetNumberFormat(int index)
            {
                return MessageBeingBuilt.GetNumberFormat(index);
            }

            public Builder SetNumberFormat(int index, NumberFormat value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.numberFormat_[index] = value;
                return this;
            }

            public Builder SetNumberFormat(int index, NumberFormat.Builder builderForValue)
            {
                if (builderForValue == null) throw new ArgumentNullException(nameof(builderForValue));
                MessageBeingBuilt.numberFormat_[index] = builderForValue.Build();
                return this;
            }

            public Builder AddNumberFormat(NumberFormat value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.numberFormat_.Add(value);
                return this;
            }

            public Builder AddNumberFormat(NumberFormat.Builder builderForValue)
            {
                if (builderForValue == null) throw new ArgumentNullException(nameof(builderForValue));
                MessageBeingBuilt.numberFormat_.Add(builderForValue.Build());
                return this;
            }

            public Builder AddRangeNumberFormat(IEnumerable<NumberFormat> values)
            {
                MessageBeingBuilt.numberFormat_.AddRange(values);
                return this;
            }

            public Builder ClearNumberFormat()
            {
                MessageBeingBuilt.numberFormat_.Clear();
                return this;
            }

            public NumberFormat GetIntlNumberFormat(int index)
            {
                return MessageBeingBuilt.GetIntlNumberFormat(index);
            }

            public Builder SetIntlNumberFormat(int index, NumberFormat value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.intlNumberFormat_[index] = value;
                return this;
            }

            public Builder SetIntlNumberFormat(int index, NumberFormat.Builder builderForValue)
            {
                if (builderForValue == null) throw new ArgumentNullException(nameof(builderForValue));
                MessageBeingBuilt.intlNumberFormat_[index] = builderForValue.Build();
                return this;
            }

            public Builder AddIntlNumberFormat(NumberFormat value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.intlNumberFormat_.Add(value);
                return this;
            }

            public Builder AddIntlNumberFormat(NumberFormat.Builder builderForValue)
            {
                if (builderForValue == null) throw new ArgumentNullException(nameof(builderForValue));
                MessageBeingBuilt.intlNumberFormat_.Add(builderForValue.Build());
                return this;
            }

            public Builder AddRangeIntlNumberFormat(IEnumerable<NumberFormat> values)
            {
                MessageBeingBuilt.intlNumberFormat_.AddRange(values);
                return this;
            }

            public Builder ClearIntlNumberFormat()
            {
                MessageBeingBuilt.intlNumberFormat_.Clear();
                return this;
            }

            public Builder SetMainCountryForCode(bool value)
            {
                MessageBeingBuilt.MainCountryForCode = value;
                return this;
            }

            public Builder ClearMainCountryForCode()
            {
                MessageBeingBuilt.MainCountryForCode = false;
                return this;
            }

            public Builder SetLeadingDigits(string value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.LeadingDigits = value;
                return this;
            }

            public Builder ClearLeadingDigits()
            {
                MessageBeingBuilt.LeadingDigits = "";
                return this;
            }

            public Builder SetMobileNumberPortableRegion(bool value)
            {
                MessageBeingBuilt.MobileNumberPortableRegion = value;
                return this;
            }

            public Builder ClearMobileNumberPortableRegion()
            {
                MessageBeingBuilt.MobileNumberPortableRegion = false;
                return this;
            }
        }


        #region Lite runtime methods

        public override int GetHashCode()
        {
            var hash = GetType().GetHashCode();
            if (HasGeneralDesc) hash ^= GeneralDesc.GetHashCode();
            if (HasFixedLine) hash ^= FixedLine.GetHashCode();
            if (HasMobile) hash ^= Mobile.GetHashCode();
            if (HasTollFree) hash ^= TollFree.GetHashCode();
            if (HasPremiumRate) hash ^= PremiumRate.GetHashCode();
            if (HasSharedCost) hash ^= SharedCost.GetHashCode();
            if (HasPersonalNumber) hash ^= PersonalNumber.GetHashCode();
            if (HasVoip) hash ^= Voip.GetHashCode();
            if (HasPager) hash ^= Pager.GetHashCode();
            if (HasUan) hash ^= Uan.GetHashCode();
            if (HasEmergency) hash ^= Emergency.GetHashCode();
            if (HasVoicemail) hash ^= Voicemail.GetHashCode();
            if (HasShortCode) hash ^= ShortCode.GetHashCode();
            if (HasStandardRate) hash ^= StandardRate.GetHashCode();
            if (HasCarrierSpecific) hash ^= CarrierSpecific.GetHashCode();
            if (HasSmsServices) hash ^= SmsServices.GetHashCode();
            if (HasNoInternationalDialling) hash ^= NoInternationalDialling.GetHashCode();
            if (HasId) hash ^= Id.GetHashCode();
            if (HasCountryCode) hash ^= CountryCode.GetHashCode();
            if (HasInternationalPrefix) hash ^= InternationalPrefix.GetHashCode();
            if (HasPreferredInternationalPrefix) hash ^= PreferredInternationalPrefix.GetHashCode();
            if (HasNationalPrefix) hash ^= NationalPrefix.GetHashCode();
            if (HasPreferredExtnPrefix) hash ^= PreferredExtnPrefix.GetHashCode();
            if (HasNationalPrefixForParsing) hash ^= NationalPrefixForParsing.GetHashCode();
            if (HasNationalPrefixTransformRule) hash ^= NationalPrefixTransformRule.GetHashCode();
            if (HasSameMobileAndFixedLinePattern) hash ^= SameMobileAndFixedLinePattern.GetHashCode();
            foreach (var i in numberFormat_)
                hash ^= i.GetHashCode();
            foreach (var i in intlNumberFormat_)
                hash ^= i.GetHashCode();
            if (HasMainCountryForCode) hash ^= MainCountryForCode.GetHashCode();
            if (HasLeadingDigits) hash ^= LeadingDigits.GetHashCode();
            if (HasMobileNumberPortableRegion) hash ^= MobileNumberPortableRegion.GetHashCode();
            return hash;
        }

        public override bool Equals(object obj)
        {
            var other = obj as PhoneMetadata;
            if (other == null) return false;
            if (HasGeneralDesc != other.HasGeneralDesc ||
                HasGeneralDesc && !GeneralDesc.Equals(other.GeneralDesc)) return false;
            if (HasFixedLine != other.HasFixedLine || HasFixedLine && !FixedLine.Equals(other.FixedLine)) return false;
            if (HasMobile != other.HasMobile || HasMobile && !Mobile.Equals(other.Mobile)) return false;
            if (HasTollFree != other.HasTollFree || HasTollFree && !TollFree.Equals(other.TollFree)) return false;
            if (HasPremiumRate != other.HasPremiumRate ||
                HasPremiumRate && !PremiumRate.Equals(other.PremiumRate)) return false;
            if (HasSharedCost != other.HasSharedCost ||
                HasSharedCost && !SharedCost.Equals(other.SharedCost)) return false;
            if (HasPersonalNumber != other.HasPersonalNumber ||
                HasPersonalNumber && !PersonalNumber.Equals(other.PersonalNumber)) return false;
            if (HasVoip != other.HasVoip || HasVoip && !Voip.Equals(other.Voip)) return false;
            if (HasPager != other.HasPager || HasPager && !Pager.Equals(other.Pager)) return false;
            if (HasUan != other.HasUan || HasUan && !Uan.Equals(other.Uan)) return false;
            if (HasEmergency != other.HasEmergency || HasEmergency && !Emergency.Equals(other.Emergency)) return false;
            if (HasVoicemail != other.HasVoicemail || HasVoicemail && !Voicemail.Equals(other.Voicemail)) return false;
            if (HasShortCode != other.HasShortCode || HasShortCode && !ShortCode.Equals(other.ShortCode)) return false;
            if (HasStandardRate != other.HasStandardRate ||
                HasStandardRate && !StandardRate.Equals(other.StandardRate)) return false;
            if (HasCarrierSpecific != other.HasCarrierSpecific ||
                HasCarrierSpecific && !CarrierSpecific.Equals(other.CarrierSpecific)) return false;
            if (HasSmsServices != other.HasSmsServices ||
                HasSmsServices && !SmsServices.Equals(other.SmsServices)) return false;
            if (HasNoInternationalDialling != other.HasNoInternationalDialling || HasNoInternationalDialling &&
                !NoInternationalDialling.Equals(other.NoInternationalDialling)) return false;
            if (HasId != other.HasId || HasId && !Id.Equals(other.Id)) return false;
            if (HasCountryCode != other.HasCountryCode ||
                HasCountryCode && !CountryCode.Equals(other.CountryCode)) return false;
            if (HasInternationalPrefix != other.HasInternationalPrefix || HasInternationalPrefix &&
                !InternationalPrefix.Equals(other.InternationalPrefix)) return false;
            if (HasPreferredInternationalPrefix != other.HasPreferredInternationalPrefix ||
                HasPreferredInternationalPrefix &&
                !PreferredInternationalPrefix.Equals(other.PreferredInternationalPrefix)) return false;
            if (HasNationalPrefix != other.HasNationalPrefix ||
                HasNationalPrefix && !NationalPrefix.Equals(other.NationalPrefix)) return false;
            if (HasPreferredExtnPrefix != other.HasPreferredExtnPrefix || HasPreferredExtnPrefix &&
                !PreferredExtnPrefix.Equals(other.PreferredExtnPrefix)) return false;
            if (HasNationalPrefixForParsing != other.HasNationalPrefixForParsing || HasNationalPrefixForParsing &&
                !NationalPrefixForParsing.Equals(other.NationalPrefixForParsing)) return false;
            if (HasNationalPrefixTransformRule != other.HasNationalPrefixTransformRule ||
                HasNationalPrefixTransformRule &&
                !NationalPrefixTransformRule.Equals(other.NationalPrefixTransformRule)) return false;
            if (HasSameMobileAndFixedLinePattern != other.HasSameMobileAndFixedLinePattern ||
                HasSameMobileAndFixedLinePattern &&
                !SameMobileAndFixedLinePattern.Equals(other.SameMobileAndFixedLinePattern)) return false;
            if (numberFormat_.Count != other.numberFormat_.Count) return false;
            for (var ix = 0; ix < numberFormat_.Count; ix++)
                if (!numberFormat_[ix].Equals(other.numberFormat_[ix])) return false;
            if (intlNumberFormat_.Count != other.intlNumberFormat_.Count) return false;
            for (var ix = 0; ix < intlNumberFormat_.Count; ix++)
                if (!intlNumberFormat_[ix].Equals(other.intlNumberFormat_[ix])) return false;
            if (HasMainCountryForCode != other.HasMainCountryForCode || HasMainCountryForCode &&
                !MainCountryForCode.Equals(other.MainCountryForCode)) return false;
            if (HasLeadingDigits != other.HasLeadingDigits ||
                HasLeadingDigits && !LeadingDigits.Equals(other.LeadingDigits)) return false;
            if (HasMobileNumberPortableRegion != other.HasMobileNumberPortableRegion || HasMobileNumberPortableRegion &&
                !MobileNumberPortableRegion.Equals(other.MobileNumberPortableRegion)) return false;
            return true;
        }

        #endregion
    }
}

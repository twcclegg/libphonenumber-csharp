using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace PhoneNumbers
{
    public sealed class PhoneNumber : IEquatable<PhoneNumber>
    {
        public const int CountryCodeFieldNumber = 1;
        public const int NationalNumberFieldNumber = 2;
        public const int ExtensionFieldNumber = 3;
        public const int ItalianLeadingZeroFieldNumber = 4;
        public const int NumberOfLeadingZerosFieldNumber = 8;
        public const int RawInputFieldNumber = 5;
        public const int CountryCodeSourceFieldNumber = 6;
        public const int PreferredDomesticCarrierCodeFieldNumber = 7;

        internal PhoneNumber Clone() => (PhoneNumber)MemberwiseClone();

        public static PhoneNumber DefaultInstance { get; } = new();

        public PhoneNumber DefaultInstanceForType => DefaultInstance;

        public bool HasCountryCode => CountryCode != 0;

        public int CountryCode { get; internal set; }

        public bool HasNationalNumber => NationalNumber != 0;

        public ulong NationalNumber { get; internal set; }

        public bool HasExtension => Extension?.Length > 0;

        public string Extension { get; internal set; } = "";

        [Obsolete("Use " + nameof(HasNumberOfLeadingZeros)), EditorBrowsable(EditorBrowsableState.Never)]
        public bool HasItalianLeadingZero => HasNumberOfLeadingZeros;

        [Obsolete("Use " + nameof(HasNumberOfLeadingZeros)), EditorBrowsable(EditorBrowsableState.Never)]
        public bool ItalianLeadingZero => HasNumberOfLeadingZeros;

        public bool HasNumberOfLeadingZeros => _numberOfLeadingZeros != 0;

        internal byte _numberOfLeadingZeros;
        public int NumberOfLeadingZeros
        {
            get => _numberOfLeadingZeros;
            internal set => _numberOfLeadingZeros = checked((byte)value);
        }

        public bool HasRawInput => RawInput?.Length > 0;

        public string RawInput { get; internal set; } = "";

        public bool HasCountryCodeSource => _countryCodeSource != 0;

        private byte _countryCodeSource;
        public Types.CountryCodeSource CountryCodeSource
        {
            get => (Types.CountryCodeSource)_countryCodeSource;
            internal set => _countryCodeSource = (byte)value;
        }

        public bool HasPreferredDomesticCarrierCode => _preferredDomesticCarrierCode != null;

        private string _preferredDomesticCarrierCode;
        public string PreferredDomesticCarrierCode
        {
            get => _preferredDomesticCarrierCode ?? "";
            internal set => _preferredDomesticCarrierCode = value;
        }

        public bool IsInitialized
        {
            get
            {
                if (!HasCountryCode) return false;
                if (!HasNationalNumber) return false;
                return true;
            }
        }

        public static Builder CreateBuilder()
        {
            return new Builder();
        }

        public Builder ToBuilder()
        {
            return CreateBuilder(this);
        }

        public Builder CreateBuilderForType()
        {
            return new Builder();
        }

        public static Builder CreateBuilder(PhoneNumber prototype)
        {
            return new Builder().MergeFrom(prototype);
        }

        #region Nested types

        [DebuggerNonUserCode]
        [CompilerGenerated]
        [GeneratedCode("ProtoGen", "2.3.0.277")]
        public static class Types
        {
            [CompilerGenerated]
            [GeneratedCode("ProtoGen", "2.3.0.277")]
            public enum CountryCodeSource
            {
                UNSPECIFIED = 0,
                FROM_NUMBER_WITH_PLUS_SIGN = 1,
                FROM_NUMBER_WITH_IDD = 5,
                FROM_NUMBER_WITHOUT_PLUS_SIGN = 10,
                FROM_DEFAULT_COUNTRY = 20
            }
        }

        #endregion

        [DebuggerNonUserCode]
        [CompilerGenerated]
        [GeneratedCode("ProtoGen", "2.3.0.277")]
        public class Builder
        {
            protected Builder ThisBuilder => this;

            internal protected PhoneNumber MessageBeingBuilt { get; private set; } = new PhoneNumber();

            public PhoneNumber DefaultInstanceForType => DefaultInstance;


            public bool HasCountryCode => MessageBeingBuilt.HasCountryCode;

            public int CountryCode
            {
                get => MessageBeingBuilt.CountryCode;
                set => SetCountryCode(value);
            }

            public bool HasNationalNumber => MessageBeingBuilt.HasNationalNumber;

            public ulong NationalNumber
            {
                get => MessageBeingBuilt.NationalNumber;
                set => SetNationalNumber(value);
            }

            public bool HasExtension => MessageBeingBuilt.HasExtension;

            public string Extension
            {
                get => MessageBeingBuilt.Extension;
                set => SetExtension(value);
            }

            [Obsolete("Use " + nameof(HasNumberOfLeadingZeros)), EditorBrowsable(EditorBrowsableState.Never)]
            public bool HasItalianLeadingZero => MessageBeingBuilt.HasItalianLeadingZero;

            [Obsolete("Use " + nameof(NumberOfLeadingZeros)), EditorBrowsable(EditorBrowsableState.Never)]
            public bool ItalianLeadingZero
            {
                get => MessageBeingBuilt.ItalianLeadingZero;
                set => SetItalianLeadingZero(value);
            }

            public bool HasNumberOfLeadingZeros => MessageBeingBuilt.HasNumberOfLeadingZeros;

            public int NumberOfLeadingZeros
            {
                get => MessageBeingBuilt.NumberOfLeadingZeros;
                set => SetNumberOfLeadingZeros(value);
            }

            public bool HasRawInput => MessageBeingBuilt.HasRawInput;

            public string RawInput
            {
                get => MessageBeingBuilt.RawInput;
                set => SetRawInput(value);
            }

            public bool HasCountryCodeSource => MessageBeingBuilt.HasCountryCodeSource;

            public Types.CountryCodeSource CountryCodeSource
            {
                get => MessageBeingBuilt.CountryCodeSource;
                set => SetCountryCodeSource(value);
            }

            public bool HasPreferredDomesticCarrierCode => MessageBeingBuilt.HasPreferredDomesticCarrierCode;

            public string PreferredDomesticCarrierCode
            {
                get => MessageBeingBuilt.PreferredDomesticCarrierCode;
                set => SetPreferredDomesticCarrierCode(value);
            }

            public Builder Clear()
            {
                MessageBeingBuilt = new PhoneNumber();
                return this;
            }

            public Builder Clone()
            {
                return new Builder().MergeFrom(MessageBeingBuilt);
            }

            public PhoneNumber Build()
            {
                return BuildPartial();
            }

            public PhoneNumber BuildPartial()
            {
                if (MessageBeingBuilt == null)
                    throw new InvalidOperationException("build() has already been called on this Builder");
                var returnMe = MessageBeingBuilt;
                MessageBeingBuilt = null;
                return returnMe;
            }


            public Builder MergeFrom(PhoneNumber other)
            {
                if (other == DefaultInstance) return this;
                if (other.HasCountryCode) CountryCode = other.CountryCode;
                if (other.HasNationalNumber) NationalNumber = other.NationalNumber;
                if (other.HasExtension) Extension = other.Extension;
                if (other.HasNumberOfLeadingZeros) NumberOfLeadingZeros = other.NumberOfLeadingZeros;
                if (other.HasRawInput) RawInput = other.RawInput;
                if (other.HasCountryCodeSource) CountryCodeSource = other.CountryCodeSource;
                if (other.HasPreferredDomesticCarrierCode)
                    PreferredDomesticCarrierCode = other.PreferredDomesticCarrierCode;
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

            public Builder SetNationalNumber(ulong value)
            {
                MessageBeingBuilt.NationalNumber = value;
                return this;
            }

            public Builder ClearNationalNumber()
            {
                MessageBeingBuilt.NationalNumber = 0UL;
                return this;
            }

            public Builder SetExtension(string value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.Extension = value;
                return this;
            }

            public Builder ClearExtension()
            {
                MessageBeingBuilt.Extension = "";
                return this;
            }

            [Obsolete("Use " + nameof(SetNumberOfLeadingZeros)), EditorBrowsable(EditorBrowsableState.Never)]
            public Builder SetItalianLeadingZero(bool value)
            {
                MessageBeingBuilt.NumberOfLeadingZeros = value ? 1 : 0;
                return this;
            }

            [Obsolete("Use " + nameof(ClearNumberOfLeadingZeros)), EditorBrowsable(EditorBrowsableState.Never)]
            public Builder ClearItalianLeadingZero()
            {
                MessageBeingBuilt.NumberOfLeadingZeros = 0;
                return this;
            }

            public Builder SetNumberOfLeadingZeros(int value)
            {
                MessageBeingBuilt.NumberOfLeadingZeros = value;
                return this;
            }

            public Builder ClearNumberOfLeadingZeros()
            {
                MessageBeingBuilt.NumberOfLeadingZeros = 0;
                return this;
            }

            public Builder SetRawInput(string value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.RawInput = value;
                return this;
            }

            public Builder ClearRawInput()
            {
                MessageBeingBuilt.RawInput = "";
                return this;
            }

            public Builder SetCountryCodeSource(Types.CountryCodeSource value)
            {
                MessageBeingBuilt.CountryCodeSource = value;
                return this;
            }

            public Builder ClearCountryCodeSource()
            {
                MessageBeingBuilt.CountryCodeSource = Types.CountryCodeSource.UNSPECIFIED;
                return this;
            }

            public Builder SetPreferredDomesticCarrierCode(string value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.PreferredDomesticCarrierCode = value;
                return this;
            }

            public Builder ClearPreferredDomesticCarrierCode()
            {
                MessageBeingBuilt.PreferredDomesticCarrierCode = null;
                return this;
            }
        }

        public override int GetHashCode()
        {
            var hash = 0;
            hash ^= CountryCode;
            hash ^= NationalNumber.GetHashCode();
            if (HasExtension) hash ^= Extension.GetHashCode();
            hash ^= _numberOfLeadingZeros;
            if (HasRawInput) hash ^= RawInput.GetHashCode();
            hash ^= _countryCodeSource;
            if (_preferredDomesticCarrierCode != null) hash ^= _preferredDomesticCarrierCode.GetHashCode();
            return hash;
        }

        public override bool Equals(object obj) => Equals(obj as PhoneNumber);

        public bool Equals(PhoneNumber other)
        {
            if (other is null)
                return false;

            if (CountryCode != other.CountryCode) return false;
            if (NationalNumber != other.NationalNumber) return false;
            if (Extension != other.Extension) return false;
            if (_numberOfLeadingZeros != other._numberOfLeadingZeros) return false;
            if (RawInput != other.RawInput) return false;
            if (_countryCodeSource != other._countryCodeSource) return false;
            if (_preferredDomesticCarrierCode != other._preferredDomesticCarrierCode) return false;
            return true;
        }
    }
}
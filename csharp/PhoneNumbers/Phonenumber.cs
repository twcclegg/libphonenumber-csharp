using System.Diagnostics.CodeAnalysis;

namespace PhoneNumbers
{

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class PhoneNumber
    {
        public int? CountryCode { get; set; }

        public ulong? NationalNumber;

        public string Extension;

        public bool ItalianLeadingZero;

        public int NumberOfLeadingZeros;

        public string RawInput;

        public Types.CountryCodeSource? CountryCodeSource { get; set; }

        public string PreferredDomesticCarrierCode;

        public class Types
        {
            public enum CountryCodeSource
            {
                Unspecified = 0,
                FromNumberWithPlusSign = 1,
                FromNumberWithIdd = 5,
                FromNumberWithoutPlusSign = 10,
                FromDefaultCountry = 20,
            }
        }

        internal PhoneNumber MergeFrom(PhoneNumber number)
        {
            CountryCode = number.CountryCode;
            Extension = number.Extension;
            CountryCodeSource = number.CountryCodeSource;
            ItalianLeadingZero = number.ItalianLeadingZero;
            NationalNumber = number.NationalNumber;
            NumberOfLeadingZeros = number.NumberOfLeadingZeros;
            PreferredDomesticCarrierCode = number.PreferredDomesticCarrierCode;
            RawInput = number.RawInput;
            return this;
        }

        public override int GetHashCode()
        {
            var hash = GetType().GetHashCode();
            hash ^= CountryCode?.GetHashCode() ?? 0;
            hash ^= NationalNumber?.GetHashCode() ?? 0;
            hash ^= Extension?.GetHashCode() ?? 0;
            hash ^= ItalianLeadingZero.GetHashCode();
            hash ^= NumberOfLeadingZeros.GetHashCode();
            hash ^= RawInput?.GetHashCode() ?? 0;
            hash ^= CountryCodeSource?.GetHashCode() ?? 0;
            hash ^= PreferredDomesticCarrierCode?.GetHashCode() ?? 0;
            return hash;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is PhoneNumber other)) return false;
            if (CountryCode != other.CountryCode) return false;
            if (NationalNumber != other.NationalNumber) return false;
            if (Extension != other.Extension) return false;
            if (ItalianLeadingZero != other.ItalianLeadingZero) return false;
            if (NumberOfLeadingZeros != other.NumberOfLeadingZeros) return false;
            if (RawInput != other.RawInput) return false;
            if (CountryCodeSource != other.CountryCodeSource) return false;
            if (PreferredDomesticCarrierCode != other.PreferredDomesticCarrierCode) return false;
            return true;
        }
    }
}

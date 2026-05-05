#nullable disable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace PhoneNumbers
{
    public class PhoneNumberToTimeZonesMapper
    {
        private static readonly string[] UNKNOWN_TIMEZONE = { "Etc/Unknown" };

        private readonly ImmutableDictionary<long, string[]> map;
        private readonly PhoneNumberUtil phoneUtil;

        internal PhoneNumberToTimeZonesMapper(IDictionary<long, string[]> source)
        {
            map = source.ToImmutableDictionary();
            phoneUtil = PhoneNumberUtil.GetInstance();
        }

        private List<string> LookUpPrefix(long phonePrefix)
        {
            while (0L < phonePrefix)
            {
                if (map.ContainsKey(phonePrefix))
                    return map[phonePrefix].ToList();

                phonePrefix /= 10L;
            }

            return UNKNOWN_TIMEZONE.ToList();
        }

        /// <summary>
        ///
        /// Returns a list of time zones to which a phone number belongs.
        /// This method assumes the validity of the number passed in has already been checked, and that
        /// the number is geo-localizable. We consider fixed-line and mobile numbers possible candidates
        /// for geo-localization.
        ///
        /// param number  a valid phone number for which we want to get the time zones to which it belongs
        /// return  a list of the corresponding time zones or a single element list with the default
        ///     unknown time zone if no other time zone was found or if the number was invalid
        /// </summary>
        /// <param name="number">a valid phone number for which we want to get the time zones to which it belongs</param>
        ///
        /// <returns>
        /// a list of the corresponding time zones or a single element list with the default
        /// unknown time zone if no other time zone was found or if the number was invalid
        /// </returns>
        public List<string> GetTimeZonesForGeographicalNumber(PhoneNumber number)
        {
            return LookUpPrefix(long.Parse(string.Concat(number.CountryCode.ToString(), phoneUtil.GetNationalSignificantNumber(number))));
        }

        /// <summary>
        ///
        /// As per GetTimeZonesForGeographicalNumber(PhoneNumber)
        /// but explicitly checks the validity of the number passed in.
        ///
        /// param number  the phone number for which we want to get the time zones to which it belongs
        /// return  a list of the corresponding time zones or a single element list with the default
        /// unknown time zone if no other time zone was found or if the number was invalid
        ///
        /// </summary>
        /// <param name="number">the phone number for which we want to get the time zones to which it belongs</param>
        /// <returns>
        /// a list of the corresponding time zones or a single element list with the default
        /// unknown time zone if no other time zone was found or if the number was invalid
        /// </returns>
        public List<string> GetTimeZonesForNumber(PhoneNumber number)
        {
            PhoneNumberType numberType = phoneUtil.GetNumberType(number);
            if (PhoneNumberType.UNKNOWN == numberType)
                return UNKNOWN_TIMEZONE.ToList();
            else if (!phoneUtil.IsNumberGeographical(numberType, number.CountryCode))
            {
                return LookUpPrefix((long)number.CountryCode);
            }

            return GetTimeZonesForGeographicalNumber(number);
        }


        /// <summary>
        /// Returns a string with the ICU unknown time zone.
        /// </summary>
        /// <returns></returns>
        public string GetUnknownTimeZone()
        {
            return UNKNOWN_TIMEZONE[0];
        }

        private const string TZMAP_DATA_DIRECTORY = "timezones.";
        // Build-time-generated binary file (see PhoneNumbers.MetadataBuilder timezones subcommand).
        // Replaces the legacy "map_data.txt" text file: same data, no runtime line/split parsing.
        private const string TZMAP_BIN_FILENAME = "map_data.bin";
        private static PhoneNumberToTimeZonesMapper Create(string timezoneDataDirectory)
        {
            var asm = typeof(PhoneNumberToTimeZonesMapper).Assembly;
            var allNames = asm.GetManifestResourceNames();
            var prefix = asm.GetName().Name + "." + timezoneDataDirectory;
            var names = allNames.Where(n => n.StartsWith(prefix, StringComparison.Ordinal)).ToList();
            var mapFile = names.FirstOrDefault(s => s.EndsWith(TZMAP_BIN_FILENAME, StringComparison.Ordinal))
                ?? throw new MissingMetadataException(
                    $"Timezone data resource '{prefix}{TZMAP_BIN_FILENAME}' not found on assembly '{asm.GetName().Name}'.");
            using var fp = asm.GetManifestResourceStream(mapFile);
            var prefixMap = BuildPrefixMapFromBin.ReadTimezoneMap(fp);
            // Rehydrate as IDictionary<long, string[]> to match the existing constructor contract.
            IDictionary<long, string[]> dict = prefixMap;
            return new PhoneNumberToTimeZonesMapper(dict);
        }

        private static readonly object lockObj = new object();
        private static PhoneNumberToTimeZonesMapper instance = null;
        public static PhoneNumberToTimeZonesMapper GetInstance()
        {
            lock (lockObj)
            {
                if (null == instance)
                {
                    instance = Create(TZMAP_DATA_DIRECTORY);
                }

                return instance;
            }
        }
    }
}

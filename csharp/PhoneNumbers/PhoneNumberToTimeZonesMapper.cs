using System;
using System.Collections.Generic;
using System.Linq;

namespace PhoneNumbers
{
    public class PhoneNumberToTimeZonesMapper
    {
        private static readonly string[] UNKNOWN_TIMEZONE = { "Etc/Unknown" };

        private readonly IDictionary<long, string[]> map;
        private readonly PhoneNumberUtil phoneUtil;

        internal PhoneNumberToTimeZonesMapper(IDictionary<long, string[]> source)
        {
            map = source;
            phoneUtil = PhoneNumberUtil.GetInstance();
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
            long phonePrefix = long.Parse(string.Concat(number.CountryCode.ToString(), phoneUtil.GetNationalSignificantNumber(number)));

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
        private const string TZMAP_Filename = "map_data.txt";
        private static PhoneNumberToTimeZonesMapper Create(string timezoneDataDirectory)
        {
            char[] splitters = { '&' }; // separates multiple entries in a string in input file
            var asm = typeof(PhoneNumberToTimeZonesMapper).Assembly;
            var allNames = asm.GetManifestResourceNames();
            var prefix = asm.GetName().Name + "." + timezoneDataDirectory;
            var names = allNames.Where(n => n.StartsWith(prefix, StringComparison.Ordinal)).ToList();
            // read map file
            var mapFile = names.Where(s => s.EndsWith(TZMAP_Filename, StringComparison.Ordinal)).First();
            using var fp = asm.GetManifestResourceStream(mapFile);
            var prefixMap = TimezoneMapDataReader.GetPrefixMap(fp, splitters);

            return new PhoneNumberToTimeZonesMapper(prefixMap);
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

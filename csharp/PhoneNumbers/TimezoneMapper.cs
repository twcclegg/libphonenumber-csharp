using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace PhoneNumbers
{
    public class TimezoneMapper
    {
        private readonly IDictionary<long, string[]> map;
        private readonly IDictionary<string, List<string[]>> dotnetmap;
        private readonly ConcurrentDictionary<string, TimeZoneInfo> tziCache;
        private readonly PhoneNumberUtil phoneUtil;

        internal TimezoneMapper(IDictionary<long, string[]> source, IDictionary<string, List<string[]>> dotnetSource, IList<TimeZoneInfo> initZones)
        {
            map = source;
            dotnetmap = dotnetSource;
            tziCache = new ConcurrentDictionary<string, TimeZoneInfo>();
            foreach (var timeZone in initZones)
            {
                tziCache.TryAdd(timeZone.Id, timeZone);
            }
            phoneUtil = PhoneNumberUtil.GetInstance();
        }

        /// <summary>
        /// Attempts to match longest prefix of <paramref name="phoneNumber"/> against a fixed list
        /// of valid prefixes, and returns the array of IANA timezone names associated with the prefix.
        /// </summary>
        /// <param name="phoneNumber">A presumed valid PhoneNumber object</param>
        /// <returns>the (possibly empty) array of IANA timezone names associated with <paramref name="phoneNumber"/></returns>
        public string[] GetTimezones(PhoneNumber phoneNumber)
        {
            long phonePrefix = long.Parse(string.Concat(phoneNumber.CountryCode.ToString(), phoneUtil.GetNationalSignificantNumber(phoneNumber)));

            while (0L < phonePrefix)
            {
                if (map.ContainsKey(phonePrefix))
                    return map[phonePrefix];

                phonePrefix /= 10L;
            }

            return Array.Empty<string>();
        }

        private bool TryFetchTimeZoneInfo(string dotnetName, out TimeZoneInfo timeZoneInfo)
        {
            timeZoneInfo = null;
            try
            {
                if (!tziCache.TryGetValue(dotnetName, out timeZoneInfo))
                {
                    timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(dotnetName);
                    tziCache.TryAdd(dotnetName, timeZoneInfo);
                }

                return true;
            }
            catch
            {
                // intentionally ignored (unrecognized time zone name)
            }

            return false;
        }

        private string[] FindRegion(string regionName, string[] timezoneNames)
        {
            foreach (var zoneName in timezoneNames)
            {
                if (dotnetmap.ContainsKey(zoneName))
                {
                    var result = dotnetmap[zoneName].FirstOrDefault(a => a[0].Equals(regionName));
                    if (null != result)
                        return result;
                }
            }

            return Array.Empty<string>();
        }

        /// <summary>
        /// Attempts to match longest prefix of <paramref name="phoneNumber"/> against a fixed list
        /// of valid prefixes, and returns .Net TimeZoneInfo instance associated with that prefix.
        /// </summary>
        /// <param name="phoneNumber">A presumed valid PhoneNumber object</param>
        /// <param name="timeZoneInfo">This out parameter references the associated .Net TimeZoneInfo instance, if a match is found. Otherwise, this out parameter is null</param>
        /// <returns>True if a match is found, false otherwise.</returns>
        public bool TryGetTimeZoneInfo(PhoneNumber phoneNumber, out TimeZoneInfo timeZoneInfo)
        {
            timeZoneInfo = null;
            var tzs = GetTimezones(phoneNumber);
            if (tzs.Any())
            {
                string regionCode = phoneUtil.GetRegionCodeForNumber(phoneNumber) ?? PhoneNumberUtil.REGION_CODE_FOR_NON_GEO_ENTITY;
                string[] aa = FindRegion(regionCode, tzs);

                if (aa.Any() && TryFetchTimeZoneInfo(aa[1], out timeZoneInfo))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Attempts to match longest prefix of <paramref name="phoneNumber"/> against a fixed list
        /// of valid prefixes, and returns the array of offsets, in minutes from UTC, for each timezone associated with
        /// the prefix.
        /// </summary>
        /// <param name="phoneNumber">A presumed valid PhoneNumber object.</param>
        /// <returns>the (possibly empty) array of offsets, in minutes from UTC, for each timezone associated with <paramref name="phoneNumber"/></returns>
        public int[] GetOffsetsFromUtc(PhoneNumber phoneNumber)
        {
            string regionCode = phoneUtil.GetRegionCodeForNumber(phoneNumber) ?? PhoneNumberUtil.REGION_CODE_FOR_NON_GEO_ENTITY;
            var tzs = GetTimezones(phoneNumber);
            int[] offsets = new int[tzs.Length];
            for (int i = 0; i < tzs.Length; i++)
            {
                offsets[i] = 0;
                if (dotnetmap.ContainsKey(tzs[i]))
                {
                    var list = dotnetmap[tzs[i]];
                    var res = list.FirstOrDefault(a => a[0].Equals(regionCode)) ?? list[0];

                    if (TryFetchTimeZoneInfo(res[1], out var tzinfo))
                    {
                        var altTime = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tzinfo);
                        offsets[i] = (int)Math.Floor(altTime.Offset.TotalMinutes);
                    }
                }
            }

            return offsets;
        }

        private const string TZMAP_DATA_DIRECTORY = "timezones.";
        private const string TZMAP_Filename = "map_data.txt";
        private const string DotnetMAP_Filename = "windowsZones.xml";
        private static TimezoneMapper Create(string timezoneDataDirectory)
        {
            char[] splitters = { '&' };
            var asm = typeof(TimezoneMapper).Assembly;
            var allNames = asm.GetManifestResourceNames();
            var prefix = asm.GetName().Name + "." + timezoneDataDirectory;
            var names = allNames.Where(n => n.StartsWith(prefix, StringComparison.Ordinal)).ToList();
            // read files
            var mapFile = names.Where(s => s.EndsWith(TZMAP_Filename, StringComparison.Ordinal)).First();
            var dnMapping = names.Where(s => s.EndsWith(DotnetMAP_Filename, StringComparison.Ordinal)).First();
            using var fp = asm.GetManifestResourceStream(mapFile);
            var prefixMap = TimezoneReader.GetPrefixMap(fp, splitters);

            using var dfp = asm.GetManifestResourceStream(dnMapping);
            var dotnetMap = TimezoneReader.GetIanaWindowsMap(dfp);

            return new TimezoneMapper(prefixMap, dotnetMap, TimeZoneInfo.GetSystemTimeZones().ToList());
        }

        private static readonly object lockObj = new object();
        private static TimezoneMapper instance = null;
        public static TimezoneMapper GetInstance()
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

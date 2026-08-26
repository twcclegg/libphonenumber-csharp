#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Collections.Immutable;
using System.IO;
using System.Text;

namespace PhoneNumbers
{
    internal static class TimezoneMapDataReader
    {
        private static List<string> LineReader(StreamReader reader, char fieldDelimiter = '|')
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length < 1 || line[0] == '#')
                    continue;

                var indexOfDelimiter = line.IndexOf(fieldDelimiter);
                if (indexOfDelimiter == -1)
                    continue;

                return [line.Substring(0, indexOfDelimiter), line.Substring(indexOfDelimiter + 1)];
            }

            return null;
        }

        /// <summary>
        /// Consumes 'map_data.txt' from this repository and returns a mapping from numerical prefixes
        /// of phone numbers to IANA time zone names associated with the number.
        /// </summary>
        /// <param name="fp">Input stream for 'map_data.txt'</param>
        /// <param name="splitters">array of char that delimits separate time zones in a string.</param>
        /// <returns></returns>
        internal static IDictionary<long, string[]> GetPrefixMap(Stream fp, char[] splitters)
        {
            if (fp is null)
                return ImmutableDictionary<long, string[]>.Empty;

            var tmpMap = new SortedDictionary<long, string[]>();
            using var lines = new StreamReader(fp, Encoding.UTF8);
            List<string> line;
            while ((line = LineReader(lines)) != null)
            {
                var pnPrefix = line[0];
                tmpMap[long.Parse(pnPrefix, CultureInfo.InvariantCulture)] = line[1].Split(splitters, StringSplitOptions.RemoveEmptyEntries);
            }

            return tmpMap;
        }
    }
}

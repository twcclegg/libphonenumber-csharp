using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Xml;

namespace PhoneNumbers
{
    public static class TimezoneReader
    {
        private class TZMapDTO
        {
            public TZMapDTO(string ianaName, string regionCode, string windowsName)
            {
                IanaTZName = ianaName;
                ISORegionName = regionCode;
                DotnetTZName = windowsName;
            }
            public string IanaTZName { get; set; } = string.Empty;
            public string ISORegionName { get; set; } = string.Empty;
            public string DotnetTZName { get; set; } = string.Empty;

            public string[] GetValue()
            {
                string[] v = { ISORegionName, DotnetTZName };
                return v;
            }
            public string GetKey()
            {
                return IanaTZName;
            }
        }

        private static List<T> ToList<T>(System.Collections.IEnumerable coll)
        {
            List<T> list = new List<T>();
            if (null != coll)
            {
                foreach (var it in coll)
                {
                    if (it is T tit)
                        list.Add(tit);
                }
            }

            return list;
        }

        private static IList<TZMapDTO> GetTZList(StreamReader reader)
        {
            List<TZMapDTO> fullList = new List<TZMapDTO>();
            string[] topLevelXPath = { "//mapZone" };
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(reader.ReadToEnd());

            foreach (var tlx in topLevelXPath)
            {
                List<XmlNode> allNodes = ToList<XmlNode>(xmlDoc.SelectNodes(tlx));


                foreach (XmlNode node in allNodes)
                {
                    if (null != node.Attributes)
                    {
                        int counter = 0;
                        string wzName = string.Empty;
                        string region = string.Empty;
                        while (counter < node.Attributes.Count)
                        {
                            XmlAttribute attr = node.Attributes[counter++] as XmlAttribute;
                            if ("other".Equals(attr.Name))
                            {
                                wzName = attr.Value.Trim();
                            }
                            if ("territory".Equals(attr.Name))
                            {
                                region = attr.Value.Trim();
                            }
                        }

                        DateTime cur = DateTimeOffset.UtcNow.DateTime;
                        counter = 0;
                        while (counter < node.Attributes.Count)
                        {
                            XmlAttribute attr = node.Attributes[counter++] as XmlAttribute;
                            if ("type".Equals(attr.Name))
                            {
                                var ianaNodes = attr.Value.Split(' ');
                                foreach (var inode in ianaNodes)
                                {
                                    var ianaName = inode.Trim();
                                    var dto = new TZMapDTO(ianaName, region, wzName);
                                    fullList.Add(dto);
                                }
                            }
                        }
                    }
                }
            }

            return fullList;
        }

        /// <summary>
        /// consuming XML data and rearranging it for a mapping lookup.
        /// data is from https://raw.githubusercontent.com/unicode-org/cldr/main/common/supplemental/windowsZones.xml
        /// and maps Microsoft Windows and .Net TimeZonInfo Ids (strings) to IANA timezone names like "America/New_York".
        /// Region codes in the original input are retained as enrichment data, potentially useful for determining details
        /// like whether or not daylight savings time applies for a specific region at a specific instant
        /// in Universal Time (aka UTC).
        /// </summary>
        ///
        private static IDictionary<string, List<string[]>> ReadIanaWindowsMap(StreamReader reader)
        {
            if (null == reader || reader.EndOfStream || !reader.BaseStream.CanRead)
            {
                return ImmutableDictionary<string, List<string[]>>.Empty;
            }

            var res = GetTZList(reader);
            if (res is null || res.Count < 1)
            {
                return ImmutableDictionary<string, List<string[]>>.Empty;
            }

            var mapping = new ConcurrentDictionary<string, List<string[]>>(3, res.Count);
            foreach (var el in res)
            {
                var key = el.GetKey();  // the IANA name of the time zone
                if (!mapping.ContainsKey(key))
                    mapping[key] = new List<string[]>();
                mapping[key].Add(el.GetValue()); // value = string[] { ISORegionName, DotnetTZName }
            }

            return mapping;
        }

        private static List<string> LineReader(StreamReader reader, char fieldDelimiter = '|')
        {
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (null == line)
                    break;

                line = line.Trim();
                if (line.Length < 1 || '#' == line[0])
                    continue;

                var indexOfPipe = line.IndexOf(fieldDelimiter);
                if (indexOfPipe == -1)
                    continue;

                var lineFields = new string[] { line.Substring(0, indexOfPipe), line.Substring(indexOfPipe + 1) };
                return new List<string>(lineFields);
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
        public static IDictionary<int, string[]> GetPrefixMap(Stream fp, char[] splitters)
        {
            var tmpMap = new SortedDictionary<int, string[]>();
            using (var lines = new StreamReader(fp, Encoding.UTF8))
            {
                List<string> line;
                while (null != (line = LineReader(lines)))
                {
                    var pnPrefix = line[0];
                    tmpMap[int.Parse(pnPrefix)] = line[1].Split(splitters, StringSplitOptions.RemoveEmptyEntries);
                }
            }

            return tmpMap;
        }

        /// <summary>
        /// Return mapping from IANA time zone names to .Net/Windows time zone names
        /// as spcified in XML file maintained by unicode.org at
        /// https://github.com/unicode-org/cldr/blob/main/common/supplemental/windowsZones.xml");
        /// </summary>
        /// <param name="fp">Stream resource from which to consume XML input</param>
        /// <returns>Mapping from IANA time zone names to .Net/Windows time zone names.</returns>
        public static IDictionary<string, List<string[]>> GetIanaWindowsMap(Stream fp)
        {
            using (var xmlReader = new StreamReader(fp, Encoding.UTF8))
            {
                return ReadIanaWindowsMap(xmlReader);
            }
        }


    }
}

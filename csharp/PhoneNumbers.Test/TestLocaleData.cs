using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace PhoneNumbers.Test
{
    /// <summary>
    /// The locale country names moved from a generated 48k-line C# dictionary to per-country
    /// binary resources built from resources/locale/country_names.txt. These check the pipeline
    /// end to end - text, to bin, to embedded resource, to lookup - since a mistake anywhere in it
    /// shows up as a geocoder that quietly stops naming countries rather than as a build failure.
    /// </summary>
    public class TestLocaleData
    {
        /// <summary>Counts as of the move; a wholesale loss of data would not be subtle.</summary>
        private const int ExpectedCountries = 249;

        [Fact]
        public void EveryCountryIsEmbedded()
        {
            Assert.Equal(ExpectedCountries, LocaleNames.SupportedCountries().Count());
        }

        [Fact]
        public void NamesLoadForAKnownCountry()
        {
            var names = LocaleNames.ForCountry("US");

            Assert.NotNull(names);
            Assert.Equal("United States", names["aa"]);
            // Most languages alias the first one that produced the same name, to store it once.
            Assert.Equal("*aa", names["en"]);
        }

        [Fact]
        public void NamesAreLocalised()
        {
            Assert.Equal("Deutschland", LocaleNames.ForCountry("DE")["de"]);
            Assert.Equal("France", LocaleNames.ForCountry("FR")["aa"]);
            Assert.Equal("iZimbabwe", LocaleNames.ForCountry("ZW")["zu"]);
        }

        /// <summary>
        /// Regions that are not ISO countries reach this lookup through the geocoder's fallback -
        /// AC and XK both do - and must come back empty rather than throwing.
        /// </summary>
        [Fact]
        public void UnknownCountriesReturnNull()
        {
            Assert.Null(LocaleNames.ForCountry("AC"));
            Assert.Null(LocaleNames.ForCountry("XK"));
            Assert.Null(LocaleNames.ForCountry("ZZ"));
            Assert.Null(LocaleNames.ForCountry(""));
        }

        [Fact]
        public void RepeatedLookupsReturnTheCachedInstance()
        {
            Assert.Same(LocaleNames.ForCountry("GB"), LocaleNames.ForCountry("GB"));
        }

        /// <summary>
        /// An alias should point at a language present in the same country. Thirteen do not, all
        /// of them Congo pointing at Norwegian Bokmal: DumpLocale shares one name-to-language map
        /// across every country, so the first country to use a name owns it and a later country
        /// with the same name aliases out of its own map. GetDisplayCountry falls back to English
        /// when that happens, so the effect is a missing translation rather than a missing name.
        ///
        /// Pinned rather than fixed: this predates moving the data out of LocaleData.cs, and
        /// correcting the generator would change the shipped names. The point here is that the
        /// move preserved the data exactly, and that the set does not grow unnoticed.
        /// </summary>
        [Fact]
        public void DanglingAliasesAreOnlyTheKnownOnes()
        {
            var dangling = new List<string>();

            foreach (var country in LocaleNames.SupportedCountries())
            {
                var names = LocaleNames.ForCountry(country);
                foreach (var entry in names.Where(entry => entry.Value.Length != 0 && entry.Value[0] == '*'))
                {
                    var target = entry.Value.Substring(1);
                    if (!names.TryGetValue(target, out var resolved) || resolved.Length == 0 || resolved[0] == '*')
                        dangling.Add($"{country}/{entry.Key}");
                }
            }

            dangling.Sort(System.StringComparer.Ordinal);
            Assert.Equal(
                new[]
                {
                    "CG/ak", "CG/bm", "CG/bs", "CG/eu", "CG/fo", "CG/ha", "CG/ki",
                    "CG/lg", "CG/ln", "CG/pl", "CG/rn", "CG/sn", "CG/so",
                },
                dangling);
        }

        /// <summary>
        /// The public dictionary is kept for callers outside the library, so it has to agree with
        /// the per-country path the library itself uses.
        /// </summary>
        [Fact]
        public void PublicDataMatchesThePerCountryLookup()
        {
            Assert.Equal(ExpectedCountries, LocaleData.Data.Count);

            foreach (var country in new[] { "US", "DE", "FR", "GB", "ZW" })
            {
                var expected = LocaleNames.ForCountry(country);
                var actual = LocaleData.Data[country];

                Assert.Equal(expected.Count, actual.Count);
                foreach (var entry in expected)
                    Assert.Equal(entry.Value, actual[entry.Key]);
            }
        }

        /// <summary>
        /// Locale.GetDisplayCountry is what reads this data, and it has to follow an alias to the
        /// language holding the name. Uses English, which resolves on the first attempt, so the
        /// assertion cannot be satisfied by the current-culture or English fallbacks behind it.
        /// </summary>
        [Fact]
        public void DisplayCountryResolvesAliases()
        {
            Assert.Equal("United States", new Locale("en", "US").GetDisplayCountry("en"));
            Assert.Equal("Germany", new Locale("en", "DE").GetDisplayCountry("en"));
            Assert.Equal("Deutschland", new Locale("de", "DE").GetDisplayCountry("de"));
        }

        /// <summary>A region with no entry falls through to the empty description.</summary>
        [Fact]
        public void DisplayCountryIsEmptyForANonIsoRegion()
        {
            Assert.Equal("", new Locale("en", "XK").GetDisplayCountry("en"));
        }
    }
}

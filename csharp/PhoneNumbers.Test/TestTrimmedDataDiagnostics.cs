using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Xunit;

namespace PhoneNumbers.Test
{
    /// <summary>
    /// What a consumer sees when a data set their build opted out of is used anyway.
    /// <para>
    /// The data is present in the test assembly, so these drive the diagnostics directly rather
    /// than by trimming. That is the point: the messages are the entire user-facing contract of the
    /// opt-out, and the failure they replace is the geocoder silently answering <c>""</c>.
    /// </para>
    /// </summary>
    public class TestTrimmedDataDiagnostics
    {
        [Fact]
        public void GeocodingDataTrimmed_NamesThePropertyAndTheAffectedTypes()
        {
            var ex = TrimmedDataErrors.GeocodingDataTrimmed();

            Assert.IsType<MissingMetadataException>(ex);
            Assert.IsAssignableFrom<InvalidOperationException>(ex);
            Assert.Contains("PhoneNumbersIncludeGeocodingData", ex.Message, StringComparison.Ordinal);
            Assert.Contains("PhoneNumberOfflineGeocoder", ex.Message, StringComparison.Ordinal);
            Assert.Contains("PhoneNumberToCarrierMapper", ex.Message, StringComparison.Ordinal);
            Assert.Contains("PhoneNumberToTimeZonesMapper", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void LocaleDataTrimmed_NamesThePropertyAndTheAffectedApi()
        {
            var ex = TrimmedDataErrors.LocaleDataTrimmed();

            Assert.IsType<MissingMetadataException>(ex);
            Assert.IsAssignableFrom<InvalidOperationException>(ex);
            Assert.Contains("PhoneNumbersIncludeLocaleNameData", ex.Message, StringComparison.Ordinal);
            Assert.Contains("GetDisplayCountry", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// A reader whose pack is absent must fail at construction. Before the pack existed this
        /// combination produced an empty lookup table and every query returned <c>""</c>, which is
        /// the silently-wrong-answer case the guard exists to remove.
        /// </summary>
        [Fact]
        public void PrefixFileReader_ThrowsWhenItsDataSetIsNotEmbedded()
        {
            var ex = Assert.Throws<MissingMetadataException>(
                () => new PrefixFileReader("nosuchdataset."));

            Assert.Contains("PhoneNumbersIncludeGeocodingData", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// The reader for a data set that <i>is</i> embedded must not throw, so the guard above
        /// cannot be satisfied by something that always throws.
        /// </summary>
        [Fact]
        public void PrefixFileReader_ConstructsWhenItsDataSetIsEmbedded()
        {
            var reader = new PrefixFileReader("geocoding.");

            Assert.NotNull(reader);
        }

        // ---- the two halves of the opt-out must agree on the switch names ----------------------

        private static XDocument LoadEmbedded(Assembly assembly, string name)
        {
            using var stream = assembly.GetManifestResourceStream(name);
            Assert.NotNull(stream);
            return XDocument.Load(stream!);
        }

        /// <summary>
        /// The feature-switch names exist in exactly two places — the <c>feature</c> attributes in
        /// <c>ILLink.Substitutions.xml</c> and the <c>RuntimeHostConfigurationOption</c> items in
        /// <c>buildTransitive/libphonenumber-csharp.targets</c> — and nothing else ties them
        /// together.
        /// <para>
        /// Misspell either by one letter and the whole opt-out stops working: ILLink treats an
        /// undefined feature as "do not apply", so a trimmed build keeps shipping the data with no
        /// warning, no error, and every other test still green. Measured: a one-letter change left
        /// PhoneNumbers.dll at 2,299,904 bytes instead of 231,936. Dropping <c>Trim="true"</c> has
        /// the same effect, because the switch then never reaches ILLink at all.
        /// </para>
        /// </summary>
        [Fact]
        public void SubstitutionsAndBuildTargetsAgreeOnTheFeatureSwitches()
        {
            var substitutions = LoadEmbedded(typeof(PhoneNumberUtil).Assembly, "ILLink.Substitutions.xml");
            var targets = LoadEmbedded(typeof(TestTrimmedDataDiagnostics).Assembly, "PhoneNumbers.Test.buildTransitive.targets");

            var removedBy = substitutions.Descendants("resource")
                .Select(e => new
                {
                    Feature = e.Attribute("feature")?.Value,
                    Value = e.Attribute("featurevalue")?.Value,
                })
                .ToList();
            Assert.NotEmpty(removedBy);

            // Every removal must be gated, and gated on the value the targets file actually emits
            // when a consumer opts out. An ungated <resource> would drop the data unconditionally.
            foreach (var removal in removedBy)
            {
                Assert.False(string.IsNullOrEmpty(removal.Feature),
                    "every <resource> in ILLink.Substitutions.xml must carry a feature attribute, "
                    + "or it removes the data from every trimmed build whether asked to or not");
                Assert.Equal("false", removal.Value);
            }

            var gatedOn = removedBy.Select(r => r.Feature!).Distinct().OrderBy(n => n, StringComparer.Ordinal);

            var ns = targets.Root!.Name.Namespace;
            var emitted = targets.Descendants(ns + "RuntimeHostConfigurationOption").ToList();
            Assert.NotEmpty(emitted);

            foreach (var option in emitted)
            {
                // Without Trim="true" the switch reaches runtimeconfig.json but never reaches
                // ILLink, so the resource survives while the app still reports the opt-out.
                Assert.Equal("true", option.Attribute("Trim")?.Value);
            }

            var declared = emitted
                .Select(o => o.Attribute("Include")?.Value!)
                .Distinct()
                .OrderBy(n => n, StringComparer.Ordinal);

            Assert.Equal(gatedOn, declared);
        }

        /// <summary>
        /// Guards the coupling between <c>ILLink.Substitutions.xml</c> and the resources it names.
        /// <para>
        /// The substitutions file removes resources by exact name and supports no wildcard, so a
        /// renamed pack would not fail anything: the entry would simply stop matching and a trimmed
        /// build would quietly keep shipping the data the property asked it to drop. The library's
        /// own build does not trim, so nothing else in CI would notice.
        /// </para>
        /// </summary>
        [Fact]
        public void SubstitutionsFileNamesOnlyResourcesThatExist()
        {
            var assembly = typeof(PhoneNumberUtil).Assembly;

            var elements = LoadEmbedded(assembly, "ILLink.Substitutions.xml")
                .Descendants("resource").ToList();
            Assert.NotEmpty(elements);

            var actual = new HashSet<string>(assembly.GetManifestResourceNames(), StringComparer.Ordinal);
            foreach (var element in elements)
            {
                var name = element.Attribute("name")?.Value;
                Assert.False(string.IsNullOrEmpty(name),
                    "every <resource> in ILLink.Substitutions.xml needs a name attribute");
                Assert.True(actual.Contains(name!),
                    $"ILLink.Substitutions.xml removes '{name}', which is not an embedded resource of " +
                    "PhoneNumbers. Either the resource was renamed and the substitutions file was not " +
                    "updated, or the entry is stale; a trimmed build would silently keep shipping the data.");
            }
        }

        /// <summary>
        /// The converse of <c>SubstitutionsFileNamesOnlyResourcesThatExist</c>: a new removable data
        /// set added without a substitutions entry would ship in every trimmed build, and nothing
        /// else would notice.
        /// </summary>
        [Fact]
        public void EveryRemovableDataResourceHasASubstitutionsEntry()
        {
            var assembly = typeof(PhoneNumberUtil).Assembly;
            var substitutions = LoadEmbedded(assembly, "ILLink.Substitutions.xml");
            var removed = substitutions.Descendants("resource")
                .Select(e => e.Attribute("name")?.Value)
                .ToHashSet(StringComparer.Ordinal);

            foreach (var prefix in new[]
                     {
                         "PhoneNumbers.geocoding.", "PhoneNumbers.carrier.",
                         "PhoneNumbers.locale.", "PhoneNumbers.timezones.",
                     })
            {
                var present = assembly.GetManifestResourceNames()
                    .Where(n => n.StartsWith(prefix, StringComparison.Ordinal))
                    .ToList();

                Assert.True(present.Count == 1,
                    $"Expected exactly one embedded resource under '{prefix}', found {present.Count}: "
                    + string.Join(", ", present.Take(5)) + ". Each removable data set must be one "
                    + "resource so a single substitutions entry can drop it.");

                Assert.True(removed.Contains(present[0]),
                    $"'{present[0]}' is a removable data resource with no entry in "
                    + "ILLink.Substitutions.xml, so it would ship in every trimmed build.");
            }
        }
    }
}

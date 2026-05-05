/*
 * Copyright (C) 2012 The Libphonenumber Authors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace PhoneNumbers.Test
{
    /// <summary>
    /// Some basic tests to check that the phone number metadata can be correctly loaded.
    /// </summary>
    /// <remarks>
    /// Joined to the <c>TestMetadataTestCase</c> xunit collection so this class serializes with
    /// the other tests that hit <see cref="MetadataManager"/> indirectly (via
    /// <see cref="PhoneNumberMatcher"/> et al). <see cref="SetMetadataLoader_RoutesLookupsThroughCustomLoader"/>
    /// installs a fake loader as a side effect; without serialization, parallel matcher tests
    /// can briefly see the fake and fail to look up alternate formats.
    /// <para/>Author: Lara Rennie
    /// </remarks>
    [Collection("TestMetadataTestCase")]
    public class TestMedataManager
    {
        [Fact]
        public void TestAlternateFormatsContainsData()
        {
            // We should have some data for Germany.
            var germanyAlternateFormats = MetadataManager.GetAlternateFormatsForCountry(49);
            Assert.NotNull(germanyAlternateFormats);
            Assert.True(germanyAlternateFormats.NumberFormatList.Count > 0);
        }

        [Fact]
        public void TestAlternateFormatsFailsGracefully()
        {
            var noAlternateFormats = MetadataManager.GetAlternateFormatsForCountry(999);
            Assert.Null(noAlternateFormats);
        }

        /// <summary>
        /// Smoke test for <see cref="MetadataManager.SetMetadataLoader"/>: after swapping in a
        /// loader that records lookup attempts, GetAlternateFormatsForCountry should route through
        /// it. Verifies the static config knob is wired up; restores the default loader on exit so
        /// other tests in this assembly continue to see real metadata.
        /// </summary>
        [Fact]
        public void SetMetadataLoader_RoutesLookupsThroughCustomLoader()
        {
            var fake = new RecordingLoader();
            MetadataManager.SetMetadataLoader(fake);
            try
            {
                // 49 = Germany; previously cached values from earlier tests live in the OLD source
                // and are not re-cached here, so a fresh lookup must hit the new loader. Ask for a
                // calling code we know is non-geo for the alternate-formats source.
                var result = MetadataManager.GetAlternateFormatsForCountry(49);
                Assert.Null(result); // RecordingLoader returns null for everything.
                Assert.Contains("PhoneNumberAlternateFormats_49", fake.RequestedFiles);
            }
            finally
            {
                // Reset back to the default so the assembly's other tests still work.
                MetadataManager.SetMetadataLoader(new EmbeddedResourceMetadataLoader());
            }
        }

        [Fact]
        public void SetMetadataLoader_RejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => MetadataManager.SetMetadataLoader(null!));
        }

        private sealed class RecordingLoader : IMetadataLoader
        {
            public List<string> RequestedFiles { get; } = new();
            public Stream? LoadMetadata(string fileName)
            {
                RequestedFiles.Add(fileName);
                return null;
            }
        }
    }
}

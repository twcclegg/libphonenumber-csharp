/*
 * Copyright (C) 2011 Google Inc.
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
using System;
using System.Collections.Generic;
using Xunit;

namespace PhoneNumbers.Test
{
    /**
    * Unittests for MappingFileProvider.java
    *
    * @author Shaopeng Jia
    */
    [Collection("TestMetadataTestCase")]
    public class TestMappingFileProvider
    {
        private readonly MappingFileProvider mappingProvider = new MappingFileProvider();

        public TestMappingFileProvider()
        {
            var mapping = new SortedDictionary<int, HashSet<String>>();
            mapping[1] = new HashSet<String>(new[] { "en" });
            mapping[86] = new HashSet<String>(new[] { "zh", "en", "zh_Hant" });
            mapping[41] = new HashSet<String>(new[] { "de", "fr", "it", "rm" });
            mapping[65] = new HashSet<String>(new[] { "en", "zh_Hans", "ms", "ta" });
            mappingProvider.ReadFileConfigs(mapping);
        }

        [Fact]
        public void TestGetFileName()
        {
            Assert.Equal("1_en", mappingProvider.GetFileName(1, "en", "", ""));
            Assert.Equal("1_en", mappingProvider.GetFileName(1, "en", "", "US"));
            Assert.Equal("1_en", mappingProvider.GetFileName(1, "en", "", "GB"));
            Assert.Equal("41_de", mappingProvider.GetFileName(41, "de", "", "CH"));
            Assert.Equal("", mappingProvider.GetFileName(44, "en", "", "GB"));
            Assert.Equal("86_zh", mappingProvider.GetFileName(86, "zh", "", ""));
            Assert.Equal("86_zh", mappingProvider.GetFileName(86, "zh", "Hans", ""));
            Assert.Equal("86_zh", mappingProvider.GetFileName(86, "zh", "", "CN"));
            Assert.Equal("", mappingProvider.GetFileName(86, "", "", "CN"));
            Assert.Equal("86_zh", mappingProvider.GetFileName(86, "zh", "Hans", "CN"));
            Assert.Equal("86_zh", mappingProvider.GetFileName(86, "zh", "Hans", "SG"));
            Assert.Equal("86_zh", mappingProvider.GetFileName(86, "zh", "", "SG"));
            Assert.Equal("86_zh_Hant", mappingProvider.GetFileName(86, "zh", "", "TW"));
            Assert.Equal("86_zh_Hant", mappingProvider.GetFileName(86, "zh", "", "HK"));
            Assert.Equal("86_zh_Hant", mappingProvider.GetFileName(86, "zh", "Hant", "TW"));
        }
    }
}
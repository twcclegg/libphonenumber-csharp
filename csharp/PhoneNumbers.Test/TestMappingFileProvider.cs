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
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace PhoneNumbers.Test
{
    /**
    * Unittests for MappingFileProvider.java
    *
    * @author Shaopeng Jia
    */
    [TestFixture]
    class TestMappingFileProvider
    {
        private readonly MappingFileProvider mappingProvider = new MappingFileProvider();

        [TestFixtureSetUp]
        public void SetupFixture()
        {
            var mapping = new SortedDictionary<int, HashSet<String>>();
            mapping[1] = new HashSet<String>(new[] { "en" });
            mapping[86] = new HashSet<String>(new[] { "zh", "en", "zh_Hant" });
            mapping[41] = new HashSet<String>(new[] { "de", "fr", "it", "rm" });
            mapping[65] = new HashSet<String>(new[] { "en", "zh_Hans", "ms", "ta" });
            mappingProvider.ReadFileConfigs(mapping);
        }

        [Test]
        public void TestGetFileName()
        {
            Assert.AreEqual("1_en", mappingProvider.GetFileName(1, "en", "", ""));
            Assert.AreEqual("1_en", mappingProvider.GetFileName(1, "en", "", "US"));
            Assert.AreEqual("1_en", mappingProvider.GetFileName(1, "en", "", "GB"));
            Assert.AreEqual("41_de", mappingProvider.GetFileName(41, "de", "", "CH"));
            Assert.AreEqual("", mappingProvider.GetFileName(44, "en", "", "GB"));
            Assert.AreEqual("86_zh", mappingProvider.GetFileName(86, "zh", "", ""));
            Assert.AreEqual("86_zh", mappingProvider.GetFileName(86, "zh", "Hans", ""));
            Assert.AreEqual("86_zh", mappingProvider.GetFileName(86, "zh", "", "CN"));
            Assert.AreEqual("", mappingProvider.GetFileName(86, "", "", "CN"));
            Assert.AreEqual("86_zh", mappingProvider.GetFileName(86, "zh", "Hans", "CN"));
            Assert.AreEqual("86_zh", mappingProvider.GetFileName(86, "zh", "Hans", "SG"));
            Assert.AreEqual("86_zh", mappingProvider.GetFileName(86, "zh", "", "SG"));
            Assert.AreEqual("86_zh_Hant", mappingProvider.GetFileName(86, "zh", "", "TW"));
            Assert.AreEqual("86_zh_Hant", mappingProvider.GetFileName(86, "zh", "", "HK"));
            Assert.AreEqual("86_zh_Hant", mappingProvider.GetFileName(86, "zh", "Hant", "TW"));
        }
    }
}
#nullable enable
/*
 * Copyright (C) 2026 The Libphonenumber Authors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 */

using System.Collections.Generic;
using System.IO;
using Xunit;

namespace PhoneNumbers.Test
{
    /// <summary>
    /// Unit tests for <see cref="MetadataSource"/>. Uses a fake <see cref="IMetadataLoader"/> so
    /// the tests don't depend on the production embedded resources or assembly layout.
    /// </summary>
    public class TestMetadataSource
    {
        private sealed class FakeLoader : IMetadataLoader
        {
            public Dictionary<string, byte[]> Files { get; } = new();
            public List<string> RequestedFiles { get; } = new();

            public Stream? LoadMetadata(string fileName)
            {
                RequestedFiles.Add(fileName);
                return Files.TryGetValue(fileName, out var bytes) ? new MemoryStream(bytes) : null;
            }
        }

        private static byte[] SerializeMetadata(PhoneMetadata metadata)
        {
            using var ms = new MemoryStream();
            BuildMetadataFromBin.WriteMetadata(ms, metadata);
            return ms.ToArray();
        }

        [Fact]
        public void ReturnsNullWhenLoaderHasNoResource()
        {
            var source = new MetadataSource(new FakeLoader(), "PhoneNumberMetadata");
            Assert.Null(source.GetMetadataForRegion("ZZ"));
            Assert.Null(source.GetMetadataForNonGeographicalRegion(999));
        }

        [Fact]
        public void LoadsAndCachesRegionMetadata()
        {
            var loader = new FakeLoader();
            loader.Files["PhoneNumberMetadata_TW"] = SerializeMetadata(new PhoneMetadata
            {
                Id = "TW",
                CountryCode = 886,
            });

            var source = new MetadataSource(loader, "PhoneNumberMetadata");
            var first = source.GetMetadataForRegion("TW");
            var second = source.GetMetadataForRegion("TW");

            Assert.NotNull(first);
            Assert.Equal("TW", first!.Id);
            Assert.Equal(886, first.CountryCode);
            Assert.Same(first, second);
            // Loader should be invoked exactly once per key — second call hits the cache.
            Assert.Single(loader.RequestedFiles);
        }

        [Fact]
        public void NonGeoLookupsFormatTheKeyAsCountryCode()
        {
            var loader = new FakeLoader();
            loader.Files["PhoneNumberMetadata_800"] = SerializeMetadata(new PhoneMetadata
            {
                Id = "001",
                CountryCode = 800,
            });

            var source = new MetadataSource(loader, "PhoneNumberMetadata");
            var metadata = source.GetMetadataForNonGeographicalRegion(800);
            Assert.NotNull(metadata);
            Assert.Equal(800, metadata!.CountryCode);
        }

        [Fact]
        public void DefaultLoaderResolvesEmbeddedBinaryMetadata()
        {
            // Smoke test: the build pipeline produces .bin files and embeds them under
            // "PhoneNumbers.metadata.*". This test catches a mismatch between the LogicalName
            // chosen by the MSBuild target and the suffix-matching done by
            // EmbeddedResourceMetadataLoader, which would silently break production lookups.
            var loader = new EmbeddedResourceMetadataLoader();
            var source = new MetadataSource(loader, "PhoneNumberMetadata");
            var us = source.GetMetadataForRegion("US");
            Assert.NotNull(us);
            Assert.Equal("US", us!.Id);
            Assert.Equal(1, us.CountryCode);
        }

        [Fact]
        public void RegionAndNonGeoCachesAreIndependent()
        {
            // Sanity check that the two caches don't accidentally collide on similar keys.
            var loader = new FakeLoader();
            loader.Files["PhoneNumberMetadata_42"] = SerializeMetadata(new PhoneMetadata { Id = "42-region" });
            // Note: in production a region code "42" doesn't exist; this is purely about cache isolation.

            var source = new MetadataSource(loader, "PhoneNumberMetadata");
            var asRegion = source.GetMetadataForRegion("42");
            var asNonGeo = source.GetMetadataForNonGeographicalRegion(42);

            Assert.NotNull(asRegion);
            Assert.NotNull(asNonGeo);
            Assert.Equal(2, loader.RequestedFiles.Count); // independent loads
        }
    }
}

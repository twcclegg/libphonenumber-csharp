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

using System.Collections.Concurrent;
using System.Globalization;

namespace PhoneNumbers
{
    /// <summary>
    /// Lazily loads per-region <see cref="PhoneMetadata"/> from binary files via a configurable
    /// <see cref="IMetadataLoader"/>. Replaces eager XML parsing on first <c>GetInstance()</c>.
    /// </summary>
    /// <remarks>
    /// Mirrors the Java upstream's <c>MetadataSource</c> /
    /// <c>NonGeographicalEntityMetadataSource</c> pair, but collapsed into one class because the
    /// surrounding hierarchy (CompositeMetadataContainer, BlockingMetadataBootstrappingGuard, etc.)
    /// only exists in Java to compensate for things <see cref="ConcurrentDictionary{TKey,TValue}"/>
    /// already gives us for free.
    /// </remarks>
    internal sealed class MetadataSource
    {
        private readonly IMetadataLoader loader;
        private readonly string filePrefix;
        private readonly ConcurrentDictionary<string, PhoneMetadata?> regionCache = new();
        private readonly ConcurrentDictionary<int, PhoneMetadata?> nonGeoCache = new();

        public MetadataSource(IMetadataLoader loader, string filePrefix)
        {
            this.loader = loader;
            this.filePrefix = filePrefix;
        }

        /// <summary>
        /// Returns the metadata for a geographical region (e.g. "TW"), or <c>null</c> if the
        /// loader has no resource for that region. The result is cached for subsequent lookups.
        /// </summary>
        public PhoneMetadata? GetMetadataForRegion(string regionCode)
            => regionCache.GetOrAdd(regionCode, key => Load(key));

        /// <summary>
        /// Returns the metadata for a non-geographical entity (e.g. country calling code 800), or
        /// <c>null</c> if no metadata exists for that calling code.
        /// </summary>
        public PhoneMetadata? GetMetadataForNonGeographicalRegion(int countryCallingCode)
            => nonGeoCache.GetOrAdd(countryCallingCode, key =>
                Load(key.ToString(CultureInfo.InvariantCulture)));

        private PhoneMetadata? Load(string key)
        {
            using var stream = loader.LoadMetadata($"{filePrefix}_{key}");
            return stream == null ? null : BuildMetadataFromBin.ReadMetadata(stream);
        }
    }
}

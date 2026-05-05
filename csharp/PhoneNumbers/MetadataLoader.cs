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

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace PhoneNumbers
{
    /// <summary>
    /// Resolves a logical metadata file name (e.g. <c>PhoneNumberMetadata_TW</c>) into a stream
    /// containing that file's binary contents. Implement this to load metadata from a custom
    /// location — disk, a CDN, a trimmed resource bundle, etc. The default
    /// <see cref="EmbeddedResourceMetadataLoader"/> reads from the assembly's manifest resources.
    /// </summary>
    /// <remarks>
    /// <para>The contract is "exact logical name": callers pass the same string that the build
    /// pipeline writes as the <c>LogicalName</c> suffix (e.g. <c>PhoneNumberMetadata_TW</c>,
    /// <c>ShortNumberMetadata_TW</c>, <c>PhoneNumberAlternateFormats_49</c>). Implementers should
    /// not perform fuzzy matching.</para>
    /// <para>Implementations must be thread-safe — <see cref="MetadataSource"/> serializes lookups
    /// per cache key but may invoke the loader concurrently for distinct keys.</para>
    /// <para>If a custom loader holds disposable resources (e.g. an HTTP client or DB connection),
    /// the implementer is responsible for managing their lifetime; <see cref="MetadataSource"/>
    /// does not implement <see cref="IDisposable"/>.</para>
    /// <para>Mirrors <c>com.google.i18n.phonenumbers.MetadataLoader</c> in Java.</para>
    /// </remarks>
    public interface IMetadataLoader
    {
        /// <summary>
        /// Returns an open stream for the given file, or <c>null</c> if it does not exist. The
        /// caller takes ownership and must dispose the returned stream.
        /// </summary>
        Stream? LoadMetadata(string fileName);
    }

    /// <summary>
    /// In-memory <see cref="IMetadataLoader"/> backed by a dictionary of pre-serialized binary
    /// metadata. Used by <see cref="PhoneNumberUtil"/>'s legacy Stream constructor: the parsed XML
    /// is round-tripped through <see cref="BuildMetadataFromBin"/> once at construction so the
    /// rest of the library can use the same lazy <c>MetadataSource</c> path as production code.
    /// </summary>
    internal sealed class InMemoryMetadataLoader : IMetadataLoader
    {
        private readonly Dictionary<string, byte[]> data;

        public InMemoryMetadataLoader(Dictionary<string, byte[]> data)
        {
            this.data = data;
        }

        public Stream? LoadMetadata(string fileName)
            => data.TryGetValue(fileName, out var bytes) ? new MemoryStream(bytes, writable: false) : null;
    }

    /// <summary>
    /// Default <see cref="IMetadataLoader"/> implementation that reads metadata from a .NET
    /// assembly's embedded manifest resources. Resource lookups are O(1) — the loader simply
    /// concatenates the configured prefix with the supplied file name and calls
    /// <c>Assembly.GetManifestResourceStream</c>.
    /// </summary>
    public sealed class EmbeddedResourceMetadataLoader : IMetadataLoader
    {
        /// <summary>
        /// Default resource-name prefix the build pipeline applies via the <c>LogicalName</c>
        /// attribute on the generated <c>EmbeddedResource</c> items in <c>PhoneNumbers.csproj</c>.
        /// Concatenated with the <c>fileName</c> argument to <see cref="LoadMetadata"/> to form
        /// the full manifest resource name (e.g. <c>PhoneNumbers.metadata.PhoneNumberMetadata_TW</c>).
        /// </summary>
        public const string DefaultResourcePrefix = "PhoneNumbers.metadata.";

        private readonly Assembly assembly;
        private readonly string resourcePrefix;

        /// <summary>
        /// Constructs a loader against the main <c>PhoneNumbers</c> assembly using the default
        /// resource prefix.
        /// </summary>
        public EmbeddedResourceMetadataLoader()
            : this(typeof(PhoneNumberUtil).Assembly, DefaultResourcePrefix) { }

        /// <summary>
        /// Constructs a loader against an arbitrary assembly using the default resource prefix.
        /// Useful for tests that ship metadata embedded in the test assembly.
        /// </summary>
        public EmbeddedResourceMetadataLoader(Assembly assembly)
            : this(assembly, DefaultResourcePrefix) { }

        /// <summary>
        /// Constructs a loader with a custom assembly and resource-name prefix.
        /// </summary>
        /// <param name="assembly">Assembly to read manifest resources from.</param>
        /// <param name="resourcePrefix">Prefix prepended to every <c>fileName</c> passed to
        /// <see cref="LoadMetadata"/> before looking it up.</param>
        public EmbeddedResourceMetadataLoader(Assembly assembly, string resourcePrefix)
        {
            this.assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
            this.resourcePrefix = resourcePrefix ?? throw new ArgumentNullException(nameof(resourcePrefix));
        }

        public Stream? LoadMetadata(string fileName)
            => assembly.GetManifestResourceStream(resourcePrefix + fileName);
    }
}

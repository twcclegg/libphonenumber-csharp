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

namespace PhoneNumbers
{
    /// <summary>
    /// Thrown when metadata for a region or country calling code that is expected to exist cannot
    /// be loaded (resource missing, stream truncated, etc.). Mirrors
    /// <c>com.google.i18n.phonenumbers.MissingMetadataException</c> in the Java upstream.
    /// </summary>
    public class MissingMetadataException : InvalidOperationException
    {
        public MissingMetadataException(string message) : base(message) { }
        public MissingMetadataException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}

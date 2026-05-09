#nullable disable
/*
 * Copyright (C) 2026 The Libphonenumber Authors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 */

using System.Diagnostics;

namespace PhoneNumbers
{
    internal static class PhoneNumberLogger
    {
        internal static void Warning(string message) => Trace.TraceWarning(message);

        internal static void Info(string message) => Trace.TraceInformation(message);

        internal static void Severe(string message) => Trace.TraceError(message);
    }
}

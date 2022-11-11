/*
 * Copyright (C) 2009 Google Inc.
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
using System.Collections.Concurrent;

namespace PhoneNumbers
{
    public class RegexCache
    {
        private readonly ConcurrentDictionary<string, PhoneRegex> cache;

        public RegexCache(int size)
        {
            cache = new ConcurrentDictionary<string, PhoneRegex>(Environment.ProcessorCount, size);
        }

        public PhoneRegex GetPatternForRegex(string regex)
        {
            return cache.GetOrAdd(regex, (k) => new PhoneRegex(regex));
        }

        // This method is used for testing.
        public bool ContainsRegex(string regex)
        {
            return cache.ContainsKey(regex);
        }
    }
}
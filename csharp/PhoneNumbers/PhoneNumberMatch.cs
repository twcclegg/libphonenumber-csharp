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

namespace PhoneNumbers
{
    public class PhoneNumberMatch
    {
        public int Start { get; }
        public int Length => RawString.Length;
        public string RawString { get; }
        public PhoneNumber Number { get; }

#if !NETSTANDARD2_0
        public PhoneNumberMatch(int start, string? rawString, PhoneNumber? number)
#else
        public PhoneNumberMatch(int start, string rawString, PhoneNumber number)
#endif
        {
            if (start < 0)
                throw new ArgumentException("Start index must be >= 0.", nameof(start));
            if (rawString is null)
                throw new ArgumentNullException(nameof(rawString));
            if (number is null)
                throw new ArgumentNullException(nameof(number));
            Start = start;
            RawString = rawString;
            Number = number;
        }

#if !NETSTANDARD2_0
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            if (this == obj)
                return true;
            if (obj is null || GetType() != obj.GetType())
                return false;
            var p = (PhoneNumberMatch)obj;
            return RawString == p.RawString && Start == p.Start && Number.Equals(p.Number);
        }

        public override int GetHashCode()
        {
            var hash = GetType().GetHashCode();
            hash ^= Start;
            hash ^= RawString.GetHashCode();
            hash ^= Number.GetHashCode();
            return hash;
        }

        public override string ToString() => $"PhoneNumberMatch [{Start},{Length}) {RawString}";
    }
}

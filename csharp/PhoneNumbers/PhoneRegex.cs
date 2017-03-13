/*
 * Copyright (C) 2011 Patrick Mezard
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
using System.Text.RegularExpressions;

namespace PhoneNumbers
{
    public class PhoneRegex : Regex
    {
        private Regex allRegex_;
        private Regex beginRegex_;

        public PhoneRegex(String pattern)
            : this(pattern, RegexOptions.None)
        {
        }

        public PhoneRegex(String pattern, RegexOptions options)
            : base(pattern, options)
        {
            var o = options | InternalRegexOptions.Default;
            allRegex_ = new Regex(String.Format("^(?:{0})$", pattern), o);
            beginRegex_ = new Regex(String.Format("^(?:{0})", pattern), o);
        }

        public Match MatchAll(String value)
        {
            return allRegex_.Match(value);
        }

        public Match MatchBeginning(String value)
        {
            return beginRegex_.Match(value);
        }
    }
}

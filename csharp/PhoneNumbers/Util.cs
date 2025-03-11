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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

#if !SIGNED
[assembly: InternalsVisibleTo("PhoneNumbers.Test")]
#else
[assembly: InternalsVisibleTo("PhoneNumbers.Test, PublicKey=0024000004800000940000000602000000240000525341310004000001000100bd3070027f51a9975cac34376755e3629985626c0ccbb41bb057f7d06dd6940dafb35ed0358fd96f24525cde3229cecc6fc9eb3bf582ecb6cf3a837f422d38fe2f5d2d7d0b75a5fe9120c77d3a0d25b9b60060cd715146920d675b6f639bcf9845bcf0f42070caca24be55143958dcc4eaa7e4e2941ecf2fab4ba479aaee8dc2")]
#endif

namespace PhoneNumbers
{
    internal class EnumerableFromConstructor<T> : IEnumerable<T>
    {
        private readonly Func<IEnumerator<T>> fn;

        public EnumerableFromConstructor(Func<IEnumerator<T>> fn)
        {
            this.fn = fn;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return fn();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return fn();
        }
    }
}

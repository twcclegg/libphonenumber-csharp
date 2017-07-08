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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PhoneNumbers
{
    public class RegexCache
    {
        class Entry
        {
            public PhoneRegex Regex;
            public LinkedListNode<string> Node;
        }

        private int size_;
        private LinkedList<string> lru_;
        private Dictionary<string, Entry> cache_;

        public RegexCache(int size)
        {
            size_ = size;
            cache_ = new Dictionary<string, Entry>(size);
            lru_ = new LinkedList<string>();
        }

        public PhoneRegex GetPatternForRegex(string regex)
        {
            lock (this)
            {
                Entry e = null;
                if (!cache_.TryGetValue(regex, out e))
                {
                    // Insert new node
                    var r = new PhoneRegex(regex);
                    var n = lru_.AddFirst(regex);
                    cache_[regex] = e = new Entry { Regex = r, Node = n };
                    // Check cache size
                    if (lru_.Count > size_)
                    {
                        var o = lru_.Last.Value;
                        cache_.Remove(o);
                        lru_.RemoveLast();
                    }
                }
                else
                {
                    if (e.Node != lru_.First)
                    {
                        lru_.Remove(e.Node);
                        lru_.AddFirst(e.Node);
                    }
                }
                return e.Regex;
            }
        }

        // This method is used for testing.
        public bool ContainsRegex(string regex)
        {
            lock (this)
            {
                return cache_.ContainsKey(regex);
            }
        }
    }
}

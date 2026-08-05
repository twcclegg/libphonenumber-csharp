#nullable disable
using System;
using System.Text;

namespace PhoneNumbers
{
    /// <summary>
    /// Per-thread reuse for the two scratch buffers a parse needs. ParseHelper allocated a
    /// StringBuilder for the national number and another for its normalized form on every call, which
    /// together accounted for roughly half of what a parse allocated.
    /// <para>
    /// Acquire clears the slot it takes from, so a nested parse - Parse is reachable from
    /// IsNumberMatch and the example-number helpers - gets a fresh builder rather than sharing one
    /// that is still in use. Buffers are only returned on the success path; the throw paths simply
    /// leave them to the collector, which costs an allocation on the next call and nothing else.
    /// </para>
    /// </summary>
    internal static class StringBuilderCache
    {
        /// <summary>
        /// Parse rejects input longer than MAX_INPUT_STRING_LENGTH (250), so a buffer larger than this
        /// grew for some other reason and is not worth keeping alive per thread.
        /// </summary>
        private const int MaxRetainedCapacity = 512;

        [ThreadStatic] private static StringBuilder first;
        [ThreadStatic] private static StringBuilder second;

        public static StringBuilder Acquire(int capacity)
        {
            var candidate = first;
            if (candidate != null && candidate.Capacity >= capacity)
            {
                first = null;
                candidate.Clear();
                return candidate;
            }

            candidate = second;
            if (candidate != null && candidate.Capacity >= capacity)
            {
                second = null;
                candidate.Clear();
                return candidate;
            }

            return new StringBuilder(capacity);
        }

        public static void Release(StringBuilder builder)
        {
            if (builder.Capacity > MaxRetainedCapacity)
                return;

            if (first == null)
                first = builder;
            else if (second == null)
                second = builder;
        }
    }
}

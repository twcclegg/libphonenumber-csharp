using System;
using System.Collections;
using System.Collections.Generic;

namespace PhoneNumbers
{
    public class SortedSet<T> : ISet<T>
    {
        private readonly SortedDictionary<T, object> items;

        public SortedSet()
        {
            items = new SortedDictionary<T, object>();
        }

        public SortedSet(IEnumerable<T> collection) : this()
        {
            foreach (var item in collection)
            {
                Add(item);
            }
        }

        public int Count => items.Count;

        public bool IsReadOnly => false;

        public bool Add(T item)
        {
            try
            {
                items.Add(item, null);
            }
            catch (ArgumentException)
            {
                return false;
            }

            return true;
        }

        public void Clear()
        {
            foreach (var key in items.Keys)
            {
                items.Remove(key);
            }
        }

        public bool Contains(T item)
        {
            return items.ContainsKey(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            items.Keys.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return items.Keys.GetEnumerator();
        }

        public bool Remove(T item)
        {
            return items.Remove(item);
        }

        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Checks whether this Tree has all elements in common with IEnumerable other
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        [System.Security.SecuritySafeCritical]
        public bool SetEquals(SortedSet<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            using (var mine = GetEnumerator())
            using (var theirs = other.GetEnumerator())
            {
                var mineEnded = !mine.MoveNext();
                var theirsEnded = !theirs.MoveNext();
                while (!mineEnded && !theirsEnded)
                {
                    if (!mine.Current.Equals(theirs.Current))
                    {
                        return false;
                    }

                    mineEnded = !mine.MoveNext();
                    theirsEnded = !theirs.MoveNext();
                }

                return mineEnded && theirsEnded;
            }
        }
    }
}

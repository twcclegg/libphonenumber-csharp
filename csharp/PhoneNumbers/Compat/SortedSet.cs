using System;
using System.Collections;
using System.Collections.Generic;

namespace PhoneNumbers
{
    public class SortedSet<T> : ISet<T>
    {
        private SortedDictionary<T, object> items;

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

        public int Count
        {
            get => items.Count;
        }

        public bool IsReadOnly
        {
            get => false;
        }

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
            items.Clear();
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
    }
}
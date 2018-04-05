using System.Collections;
using System.Collections.Generic;

namespace PhoneNumbers
{
    internal interface ISet<T> : ICollection<T>, IEnumerable<T>, IEnumerable
    {
        new bool Add(T item);
    }
}
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Caspar.Api;

namespace Caspar.Container
{
    public class ConcurrentHashSet<T> : ConcurrentDictionary<T, T>
    {
        public static ConcurrentHashSet<T> Singleton => Singleton<ConcurrentHashSet<T>>.Instance;

        public bool Contains(T element)
        {
            return this.TryGetValue(element, out element);
        }
        public int RemoveWhere(Predicate<T> match)
        {
            int count = 0;
            foreach (var e in this.Values)
            {
                if (match.Invoke(e) == true)
                {
                    if (TryRemove(e, out var old) == true)
                    {
                        count += 1;
                    }
                }
            }
            return count;
        }
    }
}

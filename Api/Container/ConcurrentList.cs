using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Caspar.Api;

namespace Caspar.Container
{
    public class ConcurrentList<T> where T : new()
    {
        public static List<T> Singleton => Singleton<List<T>>.Instance;
        System.Collections.Generic.List<T> elements = new System.Collections.Generic.List<T>();

        public delegate void ConcurrentActionCallback(System.Collections.Generic.List<T> container);
        public void ConcurrentAction(ConcurrentActionCallback action)
        {
            lock (this)
            {
                action.Invoke(elements);
            }
        }

        public List<T> Clone()
        {
            var dupe = new List<T>();
            dupe.AddRange(elements);
            return dupe;
        }

        public R ConcurrentAction<R>(Func<System.Collections.Generic.List<T>, R> action)
        {
            lock (this)
            {
                return action(elements);
            }
        }

        public int Count => elements.Count;

        public T Add(T element)
        {
            lock (this)
            {
                elements.Remove(element);
                elements.Add(element);
            }
            return element;
        }

        public T Find(Predicate<T> match)
        {
            lock (this)
            {
                foreach (var e in elements)
                {
                    if (match.Invoke(e) == true)
                    {
                        return e;
                    }
                }
            }
            return default(T);
        }

        public T Remove(T element)
        {
            lock (this)
            {
                elements.Remove(element);
                return element;
            }
        }

        public bool Contains(T element)
        {
            lock (this)
            {
                return elements.Contains(element);
            }
        }

        public T[] ToArray()
        {
            lock (this)
            {
                return elements.ToArray();
            }
        }

        public T First()
        {
            lock (this)
            {
                return elements.First();
            }
        }

    }
}

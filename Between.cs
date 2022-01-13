using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Caspar
{
    public class Between<T>
    {
        internal struct Key : IComparable<Key>//, IComparable<int>
        {
            public Key(int from, int to)
            {
                From = from;
                To = to;
            }
            public int From { get; }
            public int To { get; }

            int IComparable<Key>.CompareTo(Key a)
            {
                if (From == To)
                {
                    if (a.From <= From && a.To > From)
                    {
                        return 0;
                    }
                }

                if (a.From >= From && a.To < To)
                {
                    return 0;
                }
                if (a.To <= From)
                {
                    return 1;
                }

                if (a.From >= To)
                {
                    return -1;
                }

                return 0;
            }
            //int IComparable<int>.CompareTo(int obj)
            //{

            //    if (From <= obj && obj <= To)
            //    {
            //        return 0;
            //    }

            //    if (obj < From)
            //    {
            //        return 1;
            //    }
            //    return -1;
            //}
        }

        public void Add(int from, int to, T value)
        {
            elements.Add(new Key(from, to), value);
        }

        private int from;
        public void Add(int to, T value)
        {
            elements.Add(new Key(from, to), value);
            from = to;
        }
        public bool TryGetValue(int key, out T value)
        {
            value = default(T);
            return elements.TryGetValue(new Key(key, key), out value);
        }

        public T Get(int key)
        {
            T value = default(T);
            elements.TryGetValue(new Key(key, key), out value);
            return value;
        }
        public T Last()
        {
            return elements.Last().Value;
        }
        public T First()
        {
            return elements.First().Value;
        }
        private SortedDictionary<Key, T> elements = new SortedDictionary<Key, T>();
    }
}

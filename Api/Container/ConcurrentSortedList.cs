using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Caspar.Api;

namespace Caspar.Container
{
    public class ConcurrentSortedList<K, T> where T : class, new()
    {
        protected System.Collections.Generic.SortedList<K, T> elements = new System.Collections.Generic.SortedList<K, T>();
        public int Count => elements.Count;
        public static ConcurrentSortedList<K, T> Singleton => Singleton<ConcurrentSortedList<K, T>>.Instance;
        public virtual T Get(K uid)
        {
            lock (this)
            {
                T element = null;
                if (elements.TryGetValue(uid, out element) == false)
                {
                    return null;
                }
                return element;
            }
        }


        public delegate T CreateCallback();

        public virtual T Create(K uid, CreateCallback callback)
        {
            T element = null;
            lock (this)
            {
                element = Get(uid);
                if (element == null)
                {
                    element = callback?.Invoke();
                    if (element == null) { return null; }
                    Add(uid, element);
                }
            }
            return element;
        }

        public T this[K index]
        {
            get
            {
                return GetOrCreate(index);
            }

            set
            {
                Add(index, value);
            }
        }

        public virtual T GetOrCreate(K uid, CreateCallback callback = null)
        {
            T element = null;
            lock (this)
            {
                element = Get(uid);
                if (element == null)
                {
                    callback ??= () => { return new T(); };
                    element = Create(uid, callback);
                }
                return element;
            }
        }
        public virtual T Add(K uid, T element)
        {
            lock (this)
            {
                elements.Remove(uid, out var old);
                elements.Add(uid, element);
                return element;
            }

        }

        public virtual T Pop(K uid)
        {
            lock (this)
            {
                T element = null;
                elements.Remove(uid, out element);
                return element;
            }


        }
        public virtual bool Remove(K uid)
        {
            return Pop(uid) != null;
        }

        public System.Collections.Generic.KeyValuePair<K, T>[] ToArray()
        {
            lock (this)
            {
                return elements.ToArray();
            }
        }

        public delegate void ConcurrentActionCallback(System.Collections.Generic.SortedList<K, T> container);
        public void ConcurrentAction(ConcurrentActionCallback action)
        {
            lock (this)
            {
                action.Invoke(elements);
            }
        }

        //
        // 요약:
        //     Returns an enumerator that iterates through the collection.
        //
        // 반환 값:
        //     An enumerator that can be used to iterate through the collection.
        //public IEnumerator<KeyValuePair<K, T>> GetEnumerator()
        //{
        //    return elements.GetEnumerator();
        //}

        //public IEnumerator GetEnumerator()
        //{
        //    return elements.GetEnumerator();
        //}

    }
}

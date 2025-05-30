using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static Caspar.Api;

namespace Caspar.Container
{

    public static class DictionaryExtension
    {

        public static TValue Get<TKey, TValue>(this Dictionary<TKey, TValue> container, TKey key)
        {
            container.TryGetValue(key, out TValue value);
            return value;
        }


        public static void AddOrUpdate<TKey, TValue>(this Dictionary<TKey, TValue> container, TKey key, TValue value)
        {
            container.Remove(key);
            container.Add(key, value);
        }


        public static TValue GetOrCreate<TKey, TValue>(this Dictionary<TKey, TValue> container, TKey key, Func<TValue> callback = null) where TValue : new()
        {
            container.TryGetValue(key, out TValue value);
            if (value == null)
            {
                if (callback != null)
                {
                    value = callback.Invoke();
                }
                else
                {
                    value = new TValue();
                }
                container.Add(key, value);
            }
            return value;
        }

        public static TValue GetOrCreate<TKey, TValue>(this SortedDictionary<TKey, TValue> container, TKey key, Func<TValue> callback = null) where TValue : new()
        {
            container.TryGetValue(key, out TValue value);
            if (value == null)
            {
                if (callback != null)
                {
                    value = callback.Invoke();
                }
                else
                {
                    value = new TValue();
                }
                container.Add(key, value);
            }
            return value;
        }

        public static TValue Remove<TKey, TValue>(this Dictionary<TKey, TValue> container, TKey key)
        {
            container.TryGetValue(key, out TValue value);
            container.Remove(key);
            return value;
        }

        public static T Get<K, T>(this System.Collections.Concurrent.ConcurrentDictionary<K, T> container, K key)
        {
            container.TryGetValue(key, out T element);
            return element;
        }

        public static T Add<K, T>(this System.Collections.Concurrent.ConcurrentDictionary<K, T> container, K key, T element)
        {
            container.TryAdd(key, element);
            return element;
        }

        public static T AddOrUpdate<K, T>(this System.Collections.Concurrent.ConcurrentDictionary<K, T> container, K key, T element)
        {
            element = container.AddOrUpdate(key, element, (k, v) => { return element; });
            return element;
        }

        public static bool Update<K, T>(this System.Collections.Concurrent.ConcurrentDictionary<K, T> container, K key, T element)
        {
            return container.TryUpdate(key, element, container.Get(key));
        }

        public static T Add<T>(this ConcurrentHashSet<T> container, T element)
        {
            container.TryAdd(element, element);
            return element;
        }

        public static T AddOrUpdate<T>(this ConcurrentHashSet<T> container, T element)
        {
            container.AddOrUpdate(element, element);
            return element;
        }

        public static bool Remove<T>(this ConcurrentHashSet<T> container, T element)
        {
            return container.TryRemove(element, out element);
        }

        public static T GetOrCreate<K, T>(this System.Collections.Concurrent.ConcurrentDictionary<K, T> container, K uid, Func<T> callback = null) where T : new()
        {
            T element = container.Get(uid);
            if (element != null)
            {
                return element;
            }

            return container.Create(uid, callback);

        }

        public static T Create<K, T>(this System.Collections.Concurrent.ConcurrentDictionary<K, T> container, K uid, Func<T> callback = null) where T : new()
        {
            T element = container.Get(uid);
            if (element != null)
            {
                return element;
            }


            callback ??= () => { return new T(); };
            element = callback.Invoke();

            if (container.TryAdd(uid, element) == true)
            {
                return element;
            }
            return container.Get(uid);
        }

        public static T Pop<K, T>(this System.Collections.Concurrent.ConcurrentDictionary<K, T> container, K uid)
        {
            container.TryRemove(uid, out T element);
            return element;
        }

        public static T Remove<K, T>(this System.Collections.Concurrent.ConcurrentDictionary<K, T> container, K uid)
        {
            container.TryRemove(uid, out T element);
            return element;
        }

    }


    public static class SortExtension
    {
        //  Sorts an IList<T> in place.
        public static void Sort<T>(this IList<T> list, Comparison<T> comparison)
        {
            ArrayList.Adapter((IList)list).Sort(Comparer<T>.Create(comparison));
        }

        // Sorts in IList<T> in place, when T is IComparable<T>
        public static void Sort<T>(this IList<T> list) where T : IComparable<T>
        {
            Sort(list, (l, r) => l.CompareTo(r));
        }
    }
}

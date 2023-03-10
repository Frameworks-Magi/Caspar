using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Framework.Caspar
{
    public class Dice
    {
        static public int Roll(int from, int to)
        {
            if (from == to) { return roll(); }
            return System.Random.Shared.Next(from, to);
        }

        static public double Roll(double from, double to)
        {
            return System.Random.Shared.NextDouble() * (to - from) + from;
        }

        static protected int roll()
        {
            return System.Random.Shared.Next();
        }

        static public int Roll(int max)
        {
            return System.Random.Shared.Next(max);
        }

        public static double Roll()
        {
            return System.Random.Shared.NextDouble();
        }
        public interface IBuket<T>
        {
            void Insert(T value, int per);
            void Shuffle();
            void Clear();
            int Count { get; }
            T Pick();
        }

        public sealed class PopBucket<T> : IBuket<T>
        {

            private List<Tuple<T, int>> orignal = new List<Tuple<T, int>>();
            private ThreadLocal<HashSet<Tuple<T, int>>> picked =
                new ThreadLocal<HashSet<Tuple<T, int>>>(() => { return new HashSet<Tuple<T, int>>(); });
            private ThreadLocal<SortedDictionary<int, Tuple<T, int>>> candidates = new ThreadLocal<SortedDictionary<int, Tuple<T, int>>>(() => { return new SortedDictionary<int, Tuple<T, int>>(); });
            private ThreadLocal<int> MaxPER = new ThreadLocal<int>();
            public int Count { get { return orignal.Count; } }

            public void Shuffle()
            {
                picked.Value.Clear();
                candidates.Value.Clear();

                MaxPER.Value = 0;

                var array = orignal.ToArray();

                array.Shuffle();

                foreach (var e in array)
                {
                    if (e.Item2 == 0) { return; }
                    MaxPER.Value += e.Item2;
                    candidates.Value.Add(MaxPER.Value, e);

                }

            }
            public void Insert(T value, int per)
            {

                if (per == 0) { return; }
                orignal.Add(new Tuple<T, int>(value, per));

                //MaxPER += per;
                //var tuple = new Tuple<T, int>(value, MaxPER);
                //candidates.Value.Add(MaxPER, tuple);

            }

            public void Clear()
            {
                orignal.Clear();
                picked.Value.Clear();
                candidates.Value.Clear();
            }

            public T Pick()
            {

                if (candidates.Value.Count == 0) { return default(T); }
                var dice = global::Framework.Caspar.Dice.Roll(0, MaxPER.Value);
                var pick = candidates.Value.First(e => e.Key >= dice).Value;

                picked.Value.Add(pick);

                candidates.Value.Clear();
                MaxPER.Value = 0;

                foreach (var e in orignal)
                {
                    if (picked.Value.Contains(e) == true) { continue; }
                    MaxPER.Value += e.Item2;
                    candidates.Value.Add(MaxPER.Value, e);
                }

                return pick.Item1;


            }

        }
        public sealed class Bucket<T> : IBuket<T>
        {
            public class Slot
            {
                public T Value;
                public int PER;
            }

            private SortedDictionary<int, Tuple<T, int>> origin = new SortedDictionary<int, Tuple<T, int>>();
            public int MaxPER { get; set; }

            public int Count { get { return origin.Count; } }
            public void Insert(T value, int per)
            {

                if (per == 0) { return; }
                MaxPER += per;
                var item = new Tuple<T, int>(value, per);
                origin.Add(MaxPER, item);

            }
            public T Pick()
            {

                if (origin.Count == 0) { return default(T); }
                var dice = global::Framework.Caspar.Dice.Roll(0, MaxPER);

                var item = origin;

                var tuple = item.First(e => e.Key >= dice).Value;
                return tuple.Item1;

            }

            public void Shuffle()
            {

                var temp = origin.ToArray();
                temp.Shuffle();

                SortedDictionary<int, Tuple<T, int>> other = new SortedDictionary<int, Tuple<T, int>>();

                var per = 0;
                foreach (var e in temp)
                {
                    per += e.Value.Item2;
                    other.Add(per, e.Value);
                }
                origin = other;

            }
            public void Clear()
            {
                origin.Clear();
            }

        }

        public sealed class Fixed<T> : IBuket<T>
        {
            private ThreadLocal<int> Index = new ThreadLocal<int>();
            private List<T> origins = new List<T>();
            public void Shuffle()
            {
                Index.Value = 0;
            }
            public int Count { get { return origins.Count; } }

            public void Insert(T value, int per)
            {
                origins.Add(value);
            }
            public void Clear()
            {
                origins.Clear();
            }

            public T Pick()
            {
                if (Index.Value >= origins.Count) { return default(T); }
                return origins[Index.Value++];
            }

        }
    }
}

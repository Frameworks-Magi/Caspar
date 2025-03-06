using System;
using System.Collections.Generic;

namespace Caspar
{
    public class Recycler
    {
        public interface IRecyclable
        {
            int Id { get; }
            void Restore();
            void Empty();
        }

        protected System.Collections.Concurrent.ConcurrentDictionary<int, IRecyclable> trashes = new System.Collections.Concurrent.ConcurrentDictionary<int, IRecyclable>();
        protected System.Collections.Concurrent.ConcurrentQueue<IRecyclable> lines = new System.Collections.Concurrent.ConcurrentQueue<IRecyclable>();
        public void Add(IRecyclable value)
        {
            trashes.TryAdd(value.Id, value);
            lines.Enqueue(value);
        }

        public void Remove(IRecyclable recycle)
        {
            trashes.TryGetValue(recycle.Id, out IRecyclable trash);
            trash?.Empty();
        }

        public void Remove(int id)
        {
            trashes.TryGetValue(id, out IRecyclable trash);
            trash?.Empty();
        }


        public void Update()
        {
            if (lines.Count < 10) { return; }
            if (lines.TryDequeue(out IRecyclable trash))
            {
                if (trashes.TryRemove(trash.Id, out trash) == true)
                {
                    trash.Empty();
                }
            }
        }

        public IRecyclable Restore(int id)
        {
            if (trashes.TryRemove(id, out IRecyclable trash) == true)
            {
                trash.Restore();
                return trash;
            }
            return null;
        }
    }
}

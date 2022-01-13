using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Caspar
{

  public class ObjectPool<T> where T : new()
  {
    static private ConcurrentBag<T> objects = new ConcurrentBag<T>();
  
    static public T Alloc() {
      T item;
      if (objects.TryTake(out item)) return item;
      return New<T>.Instantiate;
    }

    static public void Dealloc(T item) {
      objects.Add(item);
    }
  }


}

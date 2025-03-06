using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caspar
{

    //public class PopBucket<B, T> : IBuket<B, T>
    //{
    //	Dictionary<B, IBuket<B, T>> buckets = new Dictionary<B, IBuket<B, T>>();
    //	public virtual void Insert(T value, int per, B bucket)
    //	{
    //		if (per == 0) { return; }

    //		IBuket<B, T> _bucket;
    //		if (buckets.TryGetValue(bucket, out _bucket) == false) {
    //			_bucket = new NestPopBucket<B, T>();
    //			buckets.Add(bucket, _bucket);
    //		}
    //		_bucket.Insert(value, per, bucket);
    //	}

    //	public virtual T Dice(B bucket)
    //	{

    //		IBuket<B, T> _bucket;
    //		if (buckets.TryGetValue(bucket, out _bucket) == false)
    //		{
    //			return default(T);
    //		}
    //		return _bucket.Dice(bucket);

    //	}

    //	public virtual void Shuffle(B bucket)
    //	{

    //		IBuket<B, T> _bucket;
    //		if (buckets.TryGetValue(bucket, out _bucket) == true)
    //		{
    //			_bucket.Shuffle(bucket);
    //		}

    //	}

    //	public virtual void Clear()
    //	{

    //		buckets.Clear();

    //	}



    //}

    //public class Bucket<B, T> : IBuket<B, T>
    //{

    //	Dictionary<B, IBuket<B, T>> buckets = new Dictionary<B, IBuket<B, T>>();
    //	public virtual void Insert(T value, int per, B bucket)
    //	{
    //		if (per == 0) { return; }

    //		IBuket<B, T> _bucket;
    //		if (buckets.TryGetValue(bucket, out _bucket) == false) {
    //			_bucket = new NestBucket<B, T>();
    //			buckets.Add(bucket, _bucket);
    //		}
    //		_bucket.Insert(value, per, bucket);

    //	}

    //	public virtual T Dice(B bucket)
    //	{

    //		IBuket<B, T> _bucket;
    //		if (buckets.TryGetValue(bucket, out _bucket) == false) {
    //			return default(T);
    //		}
    //		return _bucket.Dice();

    //	}

    //	public virtual void Shuffle(B bucket)
    //	{

    //		IBuket<B, T> _bucket;
    //		if (buckets.TryGetValue(bucket, out _bucket) == true) {
    //			_bucket.Shuffle();
    //		}

    //	}

    //	public virtual void Clear()
    //	{

    //		buckets.Clear();

    //	}

    //}
}

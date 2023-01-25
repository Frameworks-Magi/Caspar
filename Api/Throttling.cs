using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.Caspar
{
    static public partial class Api
    {
        public class Throttling
        {
            Dictionary<int, DateTime> intervals = new();
            public bool IsValid(int key, int milliseconds)
            {
                if (intervals.ContainsKey(key) == true)
                {
                    if (intervals[key] > DateTime.UtcNow)
                    {
                        return false;
                    }
                    intervals.Remove(key);
                    intervals.Add(key, DateTime.UtcNow.AddMilliseconds(milliseconds));
                    return true;
                }
                intervals.Add(key, DateTime.UtcNow.AddMilliseconds(milliseconds));
                return true;

            }
        }
    }
}

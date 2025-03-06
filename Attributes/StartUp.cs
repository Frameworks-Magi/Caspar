using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caspar.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class StartUp : Attribute
    {
        private int priority;
        public int Priority { get { return priority; } }
        public StartUp(int priority)
        {
            this.priority = priority;
        }
    }
}

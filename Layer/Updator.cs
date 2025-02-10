using Amazon.SQS.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Framework.Caspar.Api;

namespace Framework.Caspar
{
    public partial class Layer
    {


        internal static object lockObj = new Object();
        static ConcurrentQueue<Entity> queue = null;



        public static BlockingCollection<ConcurrentQueue<Entity>> Entities = new();
        public static BlockingCollection<Layer> WaitLayers = new();


    }
}

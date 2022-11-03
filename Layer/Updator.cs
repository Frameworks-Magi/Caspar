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

        internal static object mainLock = new Object();

        internal static int __state = 0;
        internal static int __handled = 0;

        public static double TotalMS = 0;

        public static void Process()
        {

            //Interlocked.Exchange();
            Console.WriteLine($"ManagedThreadId: #{System.Threading.Thread.CurrentThread.ManagedThreadId} Ready");
            while (true)
            {

                try
                {
                    while (queue.Count > 0)
                    {
                        if (!queue.TryDequeue(out var e))
                        {
                            continue;
                        }

                        e.ToRun();


                        // foreach (var item in e.messages)
                        // {
                        //     item();
                        // }


                        e.ToIdle();
                        // if (e.IsPost())
                        // {
                        //     if (e.ToWait())
                        //     {

                        //     }
                        // }
                        // //   e.Process();
                        Interlocked.Increment(ref __handled);
                        if (Interlocked.Increment(ref TotalHandled) == 500)
                        {
                            Console.WriteLine($"Layer P-B {(DateTime.UtcNow - Framework.Caspar.Layer.BeginQ).TotalMilliseconds}ms");
                        }
                        //         Console.WriteLine($"ManagedThreadId: #{System.Threading.Thread.CurrentThread.ManagedThreadId} Handled");
                        //          Task.Delay(10).Wait();


                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }




                lock (lockObj)
                {
                    if (Interlocked.CompareExchange(ref __handled, int.MaxValue, __state) == __state)
                    {
                        lock (mainLock)
                        {
                            TotalMS += (DateTime.UtcNow - BeginQ).TotalMilliseconds;
                            //Console.WriteLine($"Total Handled {__state} in {(DateTime.UtcNow - BeginQ).TotalMilliseconds}ms");
                            Monitor.Pulse(mainLock);
                        }
                    }
                    Monitor.Wait(lockObj);
                }
                //    Console.WriteLine($"ManagedThreadId: #{System.Threading.Thread.CurrentThread.ManagedThreadId} Waked Up");


            }

        }

        public static BlockingCollection<ConcurrentQueue<Entity>> Entities = new();
        public static BlockingCollection<Layer> WaitLayers = new();
        public static void UpdateInit()
        {
            queue = new ConcurrentQueue<Entity>();

            for (int i = 0; i < 16; ++i)
            {
                var t = new Thread(Process);
                t.Priority = System.Threading.ThreadPriority.Highest;
                //      t.IsThreadPoolThread = false;
                t.Start();
            }

            // t = new Thread(() =>
            // {
            //     while (true)
            //     {

            //         //         Console.WriteLine($"try task entities {Entities.Count}");
            //         var temp = Entities.Take();


            //         //        Console.WriteLine($"enter mainLock");
            //         lock (mainLock)
            //         {
            //             if (temp.Count > 0)
            //             {
            //                 //             Console.WriteLine($"enter lockObj");
            //                 lock (lockObj)
            //                 {
            //                     Interlocked.Exchange(ref __state, temp.Count);
            //                     Interlocked.Exchange(ref __handled, 0);
            //                     // if (__state == 0)
            //                     {
            //                         //                  Console.WriteLine($"__state == {__state}");
            //                     }
            //                     //                 Console.WriteLine($"pulse lockObj");
            //                     queue = temp;
            //                     Monitor.PulseAll(lockObj);
            //                 }
            //             }
            //             else
            //             {
            //                 Console.WriteLine("q is 00");
            //             }
            //             //         Console.WriteLine($"wait mainLock");
            //             Monitor.Wait(mainLock);
            //         }
            //     }
            // });

            // t.Start();

            var t2 = new Thread(() =>
            {
                while (true)
                {

                    var layer = WaitLayers.Take();
                    //layer.waitProcessEntities
                    ConcurrentQueue<Entity> q = null;
                    lock (layer)
                    {
                        if (layer.waitEntities.Count == 0)
                        {
                            Console.WriteLine("q is 0");
                        }
                        else
                        {

                        }

                        q = layer.waitEntities;
                        layer.waitEntities = new ConcurrentQueue<Entity>();

                    }

                    if (layer.ToRun())
                    {
                        layer.OnUpdate();

                        lock (mainLock)
                        {
                            if (q.Count > 0)
                            {
                                //             Console.WriteLine($"enter lockObj");
                                lock (lockObj)
                                {
                                    Interlocked.Exchange(ref __state, q.Count);
                                    Interlocked.Exchange(ref __handled, 0);
                                    // if (__state == 0)
                                    {
                                        //                  Console.WriteLine($"__state == {__state}");
                                    }
                                    //                 Console.WriteLine($"pulse lockObj");
                                    queue = q;
                                    //            Framework.Caspar.Layer.BeginQ = DateTime.UtcNow;
                                    Monitor.PulseAll(lockObj);
                                }
                            }
                            else
                            {
                                Console.WriteLine("q is 00");
                            }
                            //         Console.WriteLine($"wait mainLock");
                            Monitor.Wait(mainLock);
                        }

                        //    if (q.Count > 0)
                        //    {
                        //        Entities.Add(q);
                        //    }
                        //    else
                        //    {
                        //        Console.WriteLine("q is 000");
                        //    }
                    }
                    else
                    {
                        Console.WriteLine("layer can't be toRun");

                    }

                    //     Task.Delay(1).Wait();
                    layer.ToIdle();
                    if (layer.IsPost())
                    {
                        if (layer.ToWait())
                        {
                            WaitLayers.Add(layer);
                        }
                    }
                }
            });

            t2.Start();



        }


        public static DateTime BeginQ = DateTime.UtcNow;
        public static void InsertOne()
        {
            var q = new ConcurrentQueue<Entity>();

            var entity = new Entity();
            for (int i = 0; i < 4000; i++)
            {
                q.Enqueue(entity);
            }

            BeginQ = DateTime.UtcNow;
            Entities.Add(q);
        }

    }
}

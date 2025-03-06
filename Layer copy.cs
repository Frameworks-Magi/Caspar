// using Amazon.SQS.Model;
// using System;
// using System.Collections.Concurrent;
// using System.Collections.Generic;
// using System.Diagnostics;
// using System.Linq;
// using System.Runtime.CompilerServices;
// using System.Text;
// using System.Threading;
// using System.Threading.Tasks;
// using static Caspar.Api;

// namespace Caspar
// {
//     public partial class Layer
//     {

//         //public static int MaxLoop { get; set; } = 1000000;
//         public static int MaxLoop { get; set; } = 15;
//         public static long CurrentStrand { get => CurrentEntity.Value.UID; }

//         internal ConcurrentDictionary<int, ConcurrentQueue<Entity>> waitProcessEntities = new ConcurrentDictionary<int, ConcurrentQueue<Entity>>();
//         internal ConcurrentDictionary<int, ConcurrentQueue<Entity>> waitCloseEntities = new ConcurrentDictionary<int, ConcurrentQueue<Entity>>();
//         internal static System.Threading.Tasks.ParallelOptions options = new System.Threading.Tasks.ParallelOptions() { MaxDegreeOfParallelism = global::Caspar.Api.ThreadCount };

//         internal static BlockingCollection<Layer> Layers = new();

//         internal object pulse = new object();
//         public Layer()
//         {
//             if (options.MaxDegreeOfParallelism != global::Caspar.Api.ThreadCount)
//             {
//                 options = new System.Threading.Tasks.ParallelOptions() { MaxDegreeOfParallelism = global::Caspar.Api.ThreadCount };
//             }

//             for (int i = 0; i < global::Caspar.Api.ThreadCount; ++i)
//             {
//                 waitProcessEntities.TryAdd(i, new ConcurrentQueue<Entity>());
//                 waitCloseEntities.TryAdd(i, new ConcurrentQueue<Entity>());
//             }

//             for (int i = 0; i < global::Caspar.Api.ThreadCount; ++i)
//             {
//                 var t = new Thread((index) =>
//                 {

//                     while (true)
//                     {
//                         lock (pulse)
//                         {
//                             Monitor.Wait(pulse);
//                             Logger.Info($"{index} begin");
//                             process(waitProcessEntities[(int)index]);
//                             Logger.Info($"{index} end");
//                             //        Monitor.Pulse(pulse);
//                         }
//                     }

//                 });
//                 t.Start(i);
//             }




//             global::Caspar.Api.Add(this);
//         }

//         public virtual void OnUpdate() { }
//         public static ThreadLocal<Entity> CurrentEntity = new ThreadLocal<Entity>();
//         public static ThreadLocal<long> FromDelegateUID = new ThreadLocal<long>();
//         internal virtual bool Run()
//         {
//             bool flag = false;

//             // try
//             // {
//             //     OnUpdate();
//             // }
//             // catch (Exception e)
//             // {
//             //     Logger.Error($"{e}");
//             // }

//             // try
//             // {
//             //     flag |= ProcessEntityClose();
//             // }
//             // catch (Exception e)
//             // {
//             //     Logger.Error($"{e}");
//             // }

//             try
//             {
//                 flag |= ProcessEntityMessage();
//             }
//             catch (Exception e)
//             {
//                 Logger.Error($"{e}");
//             }

//             return flag;
//         }


//         public double HowLong { get; set; }

//         private void process(ConcurrentQueue<Entity> container)
//         {
//             Logger.Info($"begin process {container.Count}");

//             int max = container.Count;
//             int remainTask = 0;
//             while (max > 0)
//             {
//                 --max;
//                 if (container.TryDequeue(out var entity) == false)
//                 {
//                     break;
//                 }



//                 entity.interrupted = false;
//                 if (entity.ToRun())
//                 {
//                     // var sw = Stopwatch.StartNew();
//                     // var fn = Stopwatch.StartNew();
//                     CurrentEntity.Value = entity;
//                     FromDelegateUID.Value = 0;

//                     try
//                     {
//                         // sw.Restart();
//                         // for (int c = 0; entity.continuations.Count > 0 && c < MaxLoop && entity.interrupted == false && entity.Strand == kv.Key; ++c)
//                         // {
//                         //     Action callback = null;
//                         //     if (entity.continuations.TryDequeue(out callback) == false) { break; }

//                         //     try
//                         //     {
//                         //         //   System.Threading.SynchronizationContext.SetSynchronizationContext(new Entity.SynchronizationContext() { Entity = entity });
//                         //         fn.Restart();
//                         //         callback();
//                         //         if (fn.ElapsedMilliseconds > 300)
//                         //         {
//                         //             // Logger.Warning($"too long method. continuations {fn.ElapsedMilliseconds}ms");
//                         //         }
//                         //         if (sw.ElapsedMilliseconds > 99)
//                         //         {
//                         //             entity.interrupted = true;
//                         //         }
//                         //     }
//                         //     catch (Exception e)
//                         //     {
//                         //         entity.OnException(e);

//                         //     }
//                         // }
//                         // sw.Restart();
//                         //for (int c = 0; entity.messages.Count > 0 && c < MaxLoop && entity.interrupted == false && entity.Strand == kv.Key && entity.locks.Count == 0 && entity.continuations.Count == 0; ++c)
//                         for (int c = 0; entity.messages.Count > 0 && c < MaxLoop && entity.interrupted == false && entity.locks.Count == 0 && entity.continuations.Count == 0; ++c)
//                         {
//                             Action callback = null;
//                             if (entity.messages.TryDequeue(out callback) == false) { break; }

//                             try
//                             {
//                                 //    System.Threading.SynchronizationContext.SetSynchronizationContext(new Entity.SynchronizationContext() { Entity = entity });
//                                 //   fn.Restart();
//                                 DateTime now = DateTime.UtcNow;
//                                 callback();
//                                 // if (fn.ElapsedMilliseconds > 300)
//                                 // {
//                                 var elapse = (DateTime.UtcNow - now).TotalMilliseconds;
//                                 HowLong += elapse;
//                                 //         Logger.Warning($"too long method. messages {elapse}ms, {HowLong}ms");
//                                 // }

//                                 // if (sw.ElapsedMilliseconds > 99)
//                                 // {
//                                 //     entity.interrupted = true;
//                                 // }
//                             }
//                             catch (Exception e)
//                             {
//                                 entity.OnException(e);

//                             }
//                         }
//                         // sw.Restart();

//                         // for (int c = 0; entity.asynchronouslies.Count > 0 && c < MaxLoop && entity.interrupted == false && entity.Strand == kv.Key; ++c)
//                         // {
//                         //     if (entity.asynchronouslies.TryDequeue(out var callback) == false) { break; }

//                         //     try
//                         //     {
//                         //         //  System.Threading.SynchronizationContext.SetSynchronizationContext(new Entity.SynchronizationContext() { Entity = entity });
//                         //         fn.Restart();
//                         //         callback();
//                         //         if (fn.ElapsedMilliseconds > 300)
//                         //         {
//                         //             Logger.Warning($"too long method. asynchronouslies {fn.ElapsedMilliseconds}ms");
//                         //         }
//                         //         if (sw.ElapsedMilliseconds > 99)
//                         //         {
//                         //             entity.interrupted = true;
//                         //         }
//                         //     }
//                         //     catch (Exception e)
//                         //     {
//                         //         entity.OnException(e);

//                         //     }
//                         // }
//                     }
//                     catch (Exception e)
//                     {
//                         Caspar.Api.Logger.Info(e);
//                     }
//                     finally
//                     {
//                         CurrentEntity.Value = null;
//                         FromDelegateUID.Value = 0;
//                     }

//                     entity.interrupted = false;
//                     entity.ToIdle();
//                     if (entity.IsPost())
//                     {
//                         if (entity.ToWait())
//                         {
//                             Interlocked.Increment(ref remainTask);
//                             Post(entity);
//                         }
//                     }
//                 }
//             }

//             Logger.Info($"end process {container.Count}");

//         }
//         private bool ProcessEntityMessage()
//         {
//             int remainTask = 0;
//             HowLong = 0;

//             double total = 0;

//             DateTime pem = DateTime.UtcNow;

//             //   var partitioner = Partitioner.Create(0, waitProcessEntities.Count);


//             //Logger.Info("Single");
//             Logger.Info("Multi");

//             lock (pulse)
//             {

//                 Logger.Info($"begin pulse main");
//                 Monitor.PulseAll(pulse);
//                 //         Monitor.Wait(pulse);
//                 //Logger.Info($"end pulse main");
//             }

//             lock (pulse)
//             {
//                 Logger.Info($"end pulse main");
//             }

//             //foreach (var kv in waitProcessEntities)
//             // System.Threading.Tasks.Parallel.ForEach(waitProcessEntities, options, (kv) =>
//             // {
//             //     DateTime obo = DateTime.UtcNow;
//             //     double _obo = 0;
//             //     int max = kv.Value.Count;
//             //     var container = kv.Value;

//             //     while (max > 0)
//             //     {
//             //         --max;
//             //         if (container.TryDequeue(out var entity) == false)
//             //         {
//             //             break;
//             //         }



//             //         entity.interrupted = false;
//             //         if (entity.ToRun())
//             //         {
//             //             // var sw = Stopwatch.StartNew();
//             //             // var fn = Stopwatch.StartNew();
//             //             CurrentEntity.Value = entity;
//             //             FromDelegateUID.Value = 0;

//             //             try
//             //             {
//             //                 // sw.Restart();
//             //                 // for (int c = 0; entity.continuations.Count > 0 && c < MaxLoop && entity.interrupted == false && entity.Strand == kv.Key; ++c)
//             //                 // {
//             //                 //     Action callback = null;
//             //                 //     if (entity.continuations.TryDequeue(out callback) == false) { break; }

//             //                 //     try
//             //                 //     {
//             //                 //         //   System.Threading.SynchronizationContext.SetSynchronizationContext(new Entity.SynchronizationContext() { Entity = entity });
//             //                 //         fn.Restart();
//             //                 //         callback();
//             //                 //         if (fn.ElapsedMilliseconds > 300)
//             //                 //         {
//             //                 //             // Logger.Warning($"too long method. continuations {fn.ElapsedMilliseconds}ms");
//             //                 //         }
//             //                 //         if (sw.ElapsedMilliseconds > 99)
//             //                 //         {
//             //                 //             entity.interrupted = true;
//             //                 //         }
//             //                 //     }
//             //                 //     catch (Exception e)
//             //                 //     {
//             //                 //         entity.OnException(e);

//             //                 //     }
//             //                 // }
//             //                 // sw.Restart();
//             //                 for (int c = 0; entity.messages.Count > 0 && c < MaxLoop && entity.interrupted == false && entity.Strand == kv.Key && entity.locks.Count == 0 && entity.continuations.Count == 0; ++c)
//             //                 {
//             //                     Action callback = null;
//             //                     if (entity.messages.TryDequeue(out callback) == false) { break; }

//             //                     try
//             //                     {
//             //                         //    System.Threading.SynchronizationContext.SetSynchronizationContext(new Entity.SynchronizationContext() { Entity = entity });
//             //                         //   fn.Restart();
//             //                         DateTime now = DateTime.UtcNow;
//             //                         callback();
//             //                         // if (fn.ElapsedMilliseconds > 300)
//             //                         // {
//             //                         var elapse = (DateTime.UtcNow - now).TotalMilliseconds;
//             //                         HowLong += elapse;
//             //                         //         Logger.Warning($"too long method. messages {elapse}ms, {HowLong}ms");
//             //                         // }

//             //                         // if (sw.ElapsedMilliseconds > 99)
//             //                         // {
//             //                         //     entity.interrupted = true;
//             //                         // }
//             //                     }
//             //                     catch (Exception e)
//             //                     {
//             //                         entity.OnException(e);

//             //                     }
//             //                 }
//             //                 // sw.Restart();

//             //                 // for (int c = 0; entity.asynchronouslies.Count > 0 && c < MaxLoop && entity.interrupted == false && entity.Strand == kv.Key; ++c)
//             //                 // {
//             //                 //     if (entity.asynchronouslies.TryDequeue(out var callback) == false) { break; }

//             //                 //     try
//             //                 //     {
//             //                 //         //  System.Threading.SynchronizationContext.SetSynchronizationContext(new Entity.SynchronizationContext() { Entity = entity });
//             //                 //         fn.Restart();
//             //                 //         callback();
//             //                 //         if (fn.ElapsedMilliseconds > 300)
//             //                 //         {
//             //                 //             Logger.Warning($"too long method. asynchronouslies {fn.ElapsedMilliseconds}ms");
//             //                 //         }
//             //                 //         if (sw.ElapsedMilliseconds > 99)
//             //                 //         {
//             //                 //             entity.interrupted = true;
//             //                 //         }
//             //                 //     }
//             //                 //     catch (Exception e)
//             //                 //     {
//             //                 //         entity.OnException(e);

//             //                 //     }
//             //                 // }
//             //             }
//             //             catch (Exception e)
//             //             {
//             //                 Caspar.Api.Logger.Info(e);
//             //             }
//             //             finally
//             //             {
//             //                 CurrentEntity.Value = null;
//             //                 FromDelegateUID.Value = 0;
//             //             }

//             //             entity.interrupted = false;
//             //             entity.ToIdle();
//             //             if (entity.IsPost())
//             //             {
//             //                 if (entity.ToWait())
//             //                 {
//             //                     Interlocked.Increment(ref remainTask);
//             //                     Post(entity);
//             //                 }
//             //             }
//             //         }
//             //     }

//             //     if (container.Count > 0)
//             //     {
//             //         Interlocked.Increment(ref remainTask);
//             //     }
//             //     _obo = (DateTime.UtcNow - obo).TotalMilliseconds;
//             //     total += _obo;
//             //     Logger.Info($"OBO: {_obo}ms - {Thread.CurrentThread.ManagedThreadId}");

//             // });

//             Logger.Info($"PPPPPEM: {(DateTime.UtcNow - pem).TotalMilliseconds}ms, {total}ms");


//             // System.Threading.Tasks.Parallel.ForEach(waitProcessEntities, options, (kv) =>
//             // {
//             //     int max = kv.Value.Count;
//             //     var container = kv.Value;

//             //     while (max > 0)
//             //     {
//             //         --max;
//             //         if (container.TryDequeue(out var entity) == false)
//             //         {
//             //             break;
//             //         }


//             //         entity.interrupted = false;
//             //         if (entity.ToRun())
//             //         {
//             //             var sw = Stopwatch.StartNew();
//             //             var fn = Stopwatch.StartNew();
//             //             CurrentEntity.Value = entity;
//             //             FromDelegateUID.Value = 0;

//             //             try
//             //             {
//             //                 sw.Restart();
//             //                 for (int c = 0; entity.continuations.Count > 0 && c < MaxLoop && entity.interrupted == false && entity.Strand == kv.Key; ++c)
//             //                 {
//             //                     Action callback = null;
//             //                     if (entity.continuations.TryDequeue(out callback) == false) { break; }

//             //                     try
//             //                     {
//             //                         //   System.Threading.SynchronizationContext.SetSynchronizationContext(new Entity.SynchronizationContext() { Entity = entity });
//             //                         fn.Restart();
//             //                         callback();
//             //                         if (fn.ElapsedMilliseconds > 300)
//             //                         {
//             //                             // Logger.Warning($"too long method. continuations {fn.ElapsedMilliseconds}ms");
//             //                         }
//             //                         if (sw.ElapsedMilliseconds > 99)
//             //                         {
//             //                             entity.interrupted = true;
//             //                         }
//             //                     }
//             //                     catch (Exception e)
//             //                     {
//             //                         entity.OnException(e);

//             //                     }
//             //                 }
//             //                 sw.Restart();
//             //                 for (int c = 0; entity.messages.Count > 0 && c < MaxLoop && entity.interrupted == false && entity.Strand == kv.Key && entity.locks.Count == 0 && entity.continuations.Count == 0; ++c)
//             //                 {
//             //                     Action callback = null;
//             //                     if (entity.messages.TryDequeue(out callback) == false) { break; }

//             //                     try
//             //                     {
//             //                         //    System.Threading.SynchronizationContext.SetSynchronizationContext(new Entity.SynchronizationContext() { Entity = entity });
//             //                         //          fn.Restart();
//             //                         callback();
//             //                         // if (fn.ElapsedMilliseconds > 300)
//             //                         // {
//             //                         //     Logger.Warning($"too long method. messages {fn.ElapsedMilliseconds}ms");
//             //                         // }

//             //                         // if (sw.ElapsedMilliseconds > 99)
//             //                         // {
//             //                         //     entity.interrupted = true;
//             //                         // }
//             //                     }
//             //                     catch (Exception e)
//             //                     {
//             //                         entity.OnException(e);

//             //                     }
//             //                 }
//             //                 sw.Restart();

//             //                 for (int c = 0; entity.asynchronouslies.Count > 0 && c < MaxLoop && entity.interrupted == false && entity.Strand == kv.Key; ++c)
//             //                 {
//             //                     if (entity.asynchronouslies.TryDequeue(out var callback) == false) { break; }

//             //                     try
//             //                     {
//             //                         //  System.Threading.SynchronizationContext.SetSynchronizationContext(new Entity.SynchronizationContext() { Entity = entity });
//             //                         fn.Restart();
//             //                         callback();
//             //                         if (fn.ElapsedMilliseconds > 300)
//             //                         {
//             //                             Logger.Warning($"too long method. asynchronouslies {fn.ElapsedMilliseconds}ms");
//             //                         }
//             //                         if (sw.ElapsedMilliseconds > 99)
//             //                         {
//             //                             entity.interrupted = true;
//             //                         }
//             //                     }
//             //                     catch (Exception e)
//             //                     {
//             //                         entity.OnException(e);

//             //                     }
//             //                 }
//             //             }
//             //             catch (Exception e)
//             //             {
//             //                 Caspar.Api.Logger.Info(e);
//             //             }
//             //             finally
//             //             {
//             //                 CurrentEntity.Value = null;
//             //                 FromDelegateUID.Value = 0;
//             //             }

//             //             entity.interrupted = false;
//             //             entity.ToIdle();
//             //             if (entity.IsPost())
//             //             {
//             //                 if (entity.ToWait())
//             //                 {
//             //                     Interlocked.Increment(ref remainTask);
//             //                     Post(entity);
//             //                 }
//             //             }
//             //         }
//             //     }

//             //     if (container.Count > 0)
//             //     {
//             //         Interlocked.Increment(ref remainTask);
//             //     }
//             // });
//             return remainTask > 0;
//         }

//         private bool ProcessEntityClose()
//         {
//             int remainTask = 0;
//             System.Threading.Tasks.Parallel.ForEach(waitCloseEntities, options, (tasks) =>
//             {
//                 Entity task = null;
//                 int max = tasks.Value.Count;
//                 while (max > 0)
//                 {
//                     --max;
//                     if (tasks.Value.TryDequeue(out task) == false)
//                     {
//                         break;
//                     }

//                     if (task.Strand != tasks.Key)
//                     {
//                         Close(task);
//                         continue;
//                     }

//                     CurrentEntity.Value = task;
//                     FromDelegateUID.Value = 0;

//                     try
//                     {
//                         Action callback = null;
//                         while (task.messages.TryDequeue(out callback) == true)
//                         {
//                             try
//                             {
//                                 callback();
//                             }
//                             catch (Exception e)
//                             {
//                                 task.OnException(e);
//                             }
//                         }

//                     }
//                     catch
//                     {

//                     }

//                     try
//                     {
//                         _ = task.OnClose();
//                     }
//                     catch (Exception e)
//                     {
//                         Caspar.Api.Logger.Error(e);
//                     }
//                     finally
//                     {
//                         CurrentEntity.Value = null;
//                         FromDelegateUID.Value = 0;
//                     }

//                 }

//                 if (tasks.Value.Count > 0)
//                 {
//                     Interlocked.Increment(ref remainTask);
//                 }


//             });

//             return remainTask > 0;
//             //return remainTask;
//         }



//         internal enum State
//         {
//             IDLE = 0,
//             WAIT,
//             RUN,
//         }

//         int post = 0;

//         internal bool IsPost()
//         {
//             return post > 0;
//         }

//         internal bool ToRun()
//         {
//             Interlocked.Exchange(ref post, 0);
//             if (Interlocked.CompareExchange(ref state, (int)State.RUN, (int)State.WAIT) == (int)State.WAIT)
//             {
//                 return true;
//             }
//             return false;
//         }

//         public DateTime WaitAt { get; set; }
//         internal bool ToWait()
//         {
//             Interlocked.Increment(ref post);
//             if (Interlocked.CompareExchange(ref state, (int)State.WAIT, (int)State.IDLE) == (int)State.IDLE)
//             {
//                 WaitAt = DateTime.UtcNow;
//                 return true;
//             }

//             return false;
//         }
//         internal bool ToIdle()
//         {
//             if (Interlocked.CompareExchange(ref state, (int)State.IDLE, (int)State.RUN) == (int)State.RUN)
//             {
//                 return true;
//             }
//             return false;
//         }

//         public void Post()
//         {
//             if (ToWait())
//             {
//                 Layers.Add(this);
//             }
//         }
//         internal void Post(Entity e)
//         {

//             ConcurrentQueue<Entity> tasks = null;

//             if (waitProcessEntities.TryGetValue(e.Strand, out tasks) == false)
//             {
//                 return;
//             }

//             tasks.Enqueue(e);
//             // if (ToWait())
//             // {
//             //     Layers.Add(this);
//             // }
//         }
//         private int state = 0;

//         internal void Close(Entity entity)
//         {
//             ConcurrentQueue<Entity> tasks = null;
//             if (waitCloseEntities.TryGetValue(entity.Strand, out tasks) == false)
//             {
//                 return;
//             }
//             tasks.Enqueue(entity);
//             if (ToWait())
//             {
//                 Layers.Add(this);
//             }
//         }

//         internal void Close()
//         {
//         }


//     }


// }

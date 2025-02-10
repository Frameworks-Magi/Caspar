using Amazon.SQS.Model;
using Framework.Caspar.Container;
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

        //public static int MaxLoop { get; set; } = 1000000;

        public double MS { get; set; }
        static public Entity g_e { get; set; }
        public static int MaxLoop { get; set; } = 100000;
        public static long CurrentStrand { get => CurrentEntity.Value.UID; }

        internal ConcurrentDictionary<int, ConcurrentQueue<Entity>> waitProcessEntities = new ConcurrentDictionary<int, ConcurrentQueue<Entity>>();
        internal ConcurrentQueue<Entity> waitEntities = new ConcurrentQueue<Entity>();
        internal ConcurrentDictionary<int, ConcurrentQueue<Entity>> waitCloseEntities = new ConcurrentDictionary<int, ConcurrentQueue<Entity>>();
        internal static System.Threading.Tasks.ParallelOptions options = new System.Threading.Tasks.ParallelOptions() { MaxDegreeOfParallelism = global::Framework.Caspar.Api.ThreadCount };

        internal static BlockingCollection<Layer> Layers = new();
        internal BlockingCollection<(ConcurrentQueue<Entity>, int, DateTime)> Queued = new();
        internal BlockingCollection<bool> Releaser = new();

        public static int TotalHandled = 0;
        internal int TotalQueued = 0;
        public Layer()
        {
            global::Framework.Caspar.Api.ThreadCount = 16;
            if (options.MaxDegreeOfParallelism != global::Framework.Caspar.Api.ThreadCount)
            {
                options = new System.Threading.Tasks.ParallelOptions() { MaxDegreeOfParallelism = global::Framework.Caspar.Api.ThreadCount };
            }

            for (int i = 0; i < global::Framework.Caspar.Api.ThreadCount; ++i)
            {
                waitProcessEntities.TryAdd(i, new ConcurrentQueue<Entity>());
                waitCloseEntities.TryAdd(i, new ConcurrentQueue<Entity>());
            }

            for (int i = 0; i < global::Framework.Caspar.Api.ThreadCount; ++i)
            {
                var t = new Thread(() =>
                {

                    while (true)
                    {
                        var p = Queued.Take();
                        process(p.Item1, p.Item2);
                    }


                });
                t.Start();
            }
            global::Framework.Caspar.Api.Add(this);
        }

        public virtual void OnUpdate() { }
        public static ThreadLocal<Entity> CurrentEntity = new ThreadLocal<Entity>();
        public static ThreadLocal<long> FromDelegateUID = new ThreadLocal<long>();
        internal virtual bool Run()
        {
            bool flag = false;

            try
            {
                OnUpdate();
            }
            catch (Exception e)
            {
                Logger.Error($"{e}");
            }

            try
            {
                flag |= ProcessEntityClose();
            }
            catch (Exception e)
            {
                Logger.Error($"{e}");
            }

            try
            {
                flag |= ProcessEntityMessage();
            }
            catch (Exception e)
            {
                Logger.Error($"{e}");
            }

            return flag;
        }

        private void process(ConcurrentQueue<Entity> container, int max)
        {
            for (int i = 0; i < max; ++i)
            {
                if (container.TryDequeue(out var entity) == false)
                {
                    Logger.Error($"container false {container.Count}, i:{i}, max:{max}");
                    break;
                }
                entity.interrupted = false;
                if (entity.ToRun())
                {
                    try
                    {
                        CurrentEntity.Value = entity;
                        FromDelegateUID.Value = 0;
                        //   Framework.Caspar.Database.Session.CurrentSession.Value = entity.sessions.FirstOrDefault();
                        SynchronizationContext.SetSynchronizationContext(entity);
                        for (int c = 0; entity.continuations.Count > 0 && c < MaxLoop && entity.interrupted == false; ++c)
                        {
                            Action callback = null;
                            if (entity.continuations.TryDequeue(out callback) == false) { break; }

                            try
                            {
                                callback();
                            }
                            catch (Exception e)
                            {
                                entity.OnException(e);

                            }
                        }
                        for (int c = 0; entity.messages.Count > 0 && c < MaxLoop && entity.interrupted == false && entity.locks.Count == 0 && entity.continuations.Count == 0; ++c)
                        {
                            Action callback = null;
                            if (entity.messages.TryDequeue(out callback) == false) { break; }

                            try
                            {
                                callback();
                            }
                            catch (Exception e)
                            {
                                entity.OnException(e);

                            }
                        }
                        for (int c = 0; entity.asynchronouslies.Count > 0 && c < MaxLoop && entity.interrupted == false; ++c)
                        {
                            if (entity.asynchronouslies.TryDequeue(out var callback) == false) { break; }

                            try
                            {
                                callback();
                            }
                            catch (Exception e)
                            {
                                entity.OnException(e);

                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Framework.Caspar.Api.Logger.Info(e);
                    }
                    finally
                    {
                        CurrentEntity.Value = null;
                        FromDelegateUID.Value = 0;
                        SynchronizationContext.SetSynchronizationContext(null);
                    }

                    entity.interrupted = false;
                    entity.ToIdle();
                    if (entity.IsPost())
                    {
                        if (entity.ToWait())
                        {
                            Post(entity);
                        }
                    }
                }

                if (Interlocked.Decrement(ref TotalQueued) == 0)
                {

                    Releaser.Add(true);
                }
            }
        }


        private bool ProcessEntityMessage()
        {
            int totalHandled = 0;
            int remainTask = 0;

            TotalQueued = 0;
            bool flags = false;


            int[] maxs = new int[global::Framework.Caspar.Api.ThreadCount];

            //-------------------------------------
            for (int i = 0; i < global::Framework.Caspar.Api.ThreadCount; ++i)
            {
                int max = waitProcessEntities[i].Count;
                TotalQueued += max;
                maxs[i] = max;
            }

            totalHandled = TotalQueued;

            if (TotalQueued > 0)
            {
                flags = true;
            }

            for (int i = 0; i < global::Framework.Caspar.Api.ThreadCount; ++i)
            {
                Queued.Add((waitProcessEntities[i], maxs[i], DateTime.UtcNow));
            }

            // foreach (var kv in waitProcessEntities)
            // {
            //     int max = kv.Value.Count;

            //     if (max > 0)
            //     {
            //         Interlocked.Add(ref TotalQueued, max);
            //         flags = true;
            //         Queued.Add((kv.Value, max, DateTime.UtcNow));
            //     }
            // }

            if (flags == true)
            {
                Releaser.Take();
            }

            //   Logger.Info($"TotalHandled {totalHandled}");


            foreach (var kv in waitProcessEntities)
            {
                int max = kv.Value.Count;
                if (max > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private bool ProcessEntityClose()
        {
            int remainTask = 0;
            System.Threading.Tasks.Parallel.ForEach(waitCloseEntities, options, (tasks) =>
            {
                Entity task = null;
                int max = tasks.Value.Count;
                while (max > 0)
                {
                    --max;
                    if (tasks.Value.TryDequeue(out task) == false)
                    {
                        break;
                    }

                    if (task.Strand != tasks.Key)
                    {
                        Close(task);
                        continue;
                    }

                    CurrentEntity.Value = task;
                    FromDelegateUID.Value = 0;

                    try
                    {
                        Action callback = null;
                        while (task.messages.TryDequeue(out callback) == true)
                        {
                            try
                            {
                                callback();
                            }
                            catch (Exception e)
                            {
                                task.OnException(e);
                            }
                        }

                    }
                    catch
                    {

                    }

                    try
                    {
                        _ = task.OnClose();
                    }
                    catch (Exception e)
                    {
                        Framework.Caspar.Api.Logger.Error(e);
                    }
                    finally
                    {
                        CurrentEntity.Value = null;
                        FromDelegateUID.Value = 0;
                    }

                }

                if (tasks.Value.Count > 0)
                {
                    Interlocked.Increment(ref remainTask);
                }


            });


            return remainTask > 0;
            //return remainTask;
        }



        internal enum State
        {
            IDLE = 0,
            WAIT,
            RUN,
        }

        int post = 0;

        internal bool IsPost()
        {
            return post > 0;
        }

        internal bool ToRun()
        {
            Interlocked.Exchange(ref post, 0);
            if (Interlocked.CompareExchange(ref state, (int)State.RUN, (int)State.WAIT) == (int)State.WAIT)
            {
                return true;
            }
            return false;
        }

        public DateTime WaitAt { get; set; }
        internal bool ToWait()
        {
            Interlocked.Increment(ref post);
            if (Interlocked.CompareExchange(ref state, (int)State.WAIT, (int)State.IDLE) == (int)State.IDLE)
            {
                WaitAt = DateTime.UtcNow;
                return true;
            }

            return false;
        }
        internal bool ToIdle()
        {
            if (Interlocked.CompareExchange(ref state, (int)State.IDLE, (int)State.RUN) == (int)State.RUN)
            {
                return true;
            }
            return false;
        }
        internal void Post(Entity e)
        {
            ConcurrentQueue<Entity> tasks = null;
            if (waitProcessEntities.TryGetValue(e.Strand, out tasks) == false)
            {
                return;
            }
            tasks.Enqueue(e);
            if (ToWait())
            {
                e.PostAt = DateTime.UtcNow;
                // WaitLayers.Add(this);
                Layers.Add(this);
            }
        }
        private int state = 0;

        internal void Close(Entity entity)
        {
            ConcurrentQueue<Entity> tasks = null;
            if (waitCloseEntities.TryGetValue(entity.Strand, out tasks) == false)
            {
                return;
            }
            tasks.Enqueue(entity);
            if (ToWait())
            {
                Layers.Add(this);
            }
        }

        internal void Close()
        {
        }


    }


}

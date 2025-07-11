using Amazon.SQS.Model;
using Caspar.Container;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Caspar.Api;

namespace Caspar
{
    public partial class Layer
    {

        //public static int MaxLoop { get; set; } = 1000000;

        public double MS { get; set; }
        static public Frame g_e { get; set; }
        public static int MaxLoop { get; set; } = 100000;
        public static long CurrentStrand { get => CurrentEntity.Value.UID; }

        //internal ConcurrentDictionary<int, ConcurrentQueue<Frame>> waitProcessEntities = new ConcurrentDictionary<int, ConcurrentQueue<Frame>>();
        internal ConcurrentQueue<Frame>[] waitProcessEntities = new ConcurrentQueue<Frame>[0];
        internal ConcurrentQueue<Frame> waitEntities = new ConcurrentQueue<Frame>();
        internal ConcurrentDictionary<int, ConcurrentQueue<Frame>> waitCloseEntities = new ConcurrentDictionary<int, ConcurrentQueue<Frame>>();
        internal static System.Threading.Tasks.ParallelOptions options = new System.Threading.Tasks.ParallelOptions() { MaxDegreeOfParallelism = global::Caspar.Api.ThreadCount };
        internal int[] maxs = new int[0];
        internal static BlockingCollection<Layer> Layers = new();
        internal static BlockingCollection<Layer>[] Queued = new BlockingCollection<Layer>[TotalWorkers];
        internal BlockingCollection<bool> Releaser = new();
        static internal int TotalWorkers { get; set; } = 1;

        public Layer()
        {
            waitProcessEntities = new ConcurrentQueue<Frame>[TotalWorkers];
            for (int i = 0; i < TotalWorkers; ++i)
            {
                //waitProcessEntities.TryAdd(i, new ConcurrentQueue<Frame>());
                waitProcessEntities[i] = new ConcurrentQueue<Frame>();
                waitCloseEntities.TryAdd(i, new ConcurrentQueue<Frame>());
            }

            maxs = new int[TotalWorkers];
            global::Caspar.Api.Add(this);
        }

        public virtual void OnUpdate() { }
        public static ThreadLocal<Frame> CurrentEntity = new ThreadLocal<Frame>();
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

        internal void process(int index)
        {
            var container = waitProcessEntities[index];
            var max = maxs[index];

            if (container.Count < max)
            {
                Logger.Error($"container count {container.Count} < max {max}, index:{index}");
            }

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
                        //   Caspar.Database.Session.CurrentSession.Value = entity.sessions.FirstOrDefault();
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
                        Caspar.Api.Logger.Info(e);
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
            }
            Releaser.Add(true);
        }


        private bool ProcessEntityMessage()
        {
            int totalMessage = 0;
            int totalQueued = 0;

            for (int i = 0; i < Layer.TotalWorkers; ++i)
            {
                maxs[i] = waitProcessEntities[i].Count;
                if (maxs[i] > 0)
                {
                    totalQueued += 1;
                }
                totalMessage += maxs[i];
            }

            if (totalMessage == 0)
            {
                foreach (var kv in waitProcessEntities)
                {
                    if (kv.Count > 0) { return true; }
                }
                return false;
            }


            for (int i = 0; i < Layer.TotalWorkers; ++i)
            {
                if (maxs[i] == 0) { continue; }
                Queued[i].Add(this);  // 🔥 여러 워커에게 동일한 Layer 전달
            }


            for (int i = 0; i < totalQueued; ++i)
            {
                Releaser.Take();
            }

            foreach (var kv in waitProcessEntities)
            {
                if (kv.Count > 0) { return true; }
            }
            return false;
        }

        private bool ProcessEntityClose()
        {
            int remainTask = 0;
            System.Threading.Tasks.Parallel.ForEach(waitCloseEntities, options, (tasks) =>
            {
                Frame task = null;
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
                        Caspar.Api.Logger.Error(e);
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
        internal void Post(Frame e)
        {
            var tasks = waitProcessEntities[e.Strand];
            tasks.Enqueue(e);
            if (ToWait())
            {
                e.PostAt = DateTime.UtcNow;
                Layers.Add(this);
            }
        }
        private int state = 0;

        internal void Close(Frame entity)
        {
            ConcurrentQueue<Frame> tasks = null;
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

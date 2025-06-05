using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Caspar.Api;

namespace Caspar
{
    public static partial class Extension
    {
        public static Layer.Awaitable Strand(this System.Threading.Tasks.Task task, Layer.Frame entity)
        {
            return new Layer.Awaitable(task, entity);
        }

        public static Layer.Awaitable Lock(this global::Caspar.Strand strand, Layer.Frame entity)
        {
            return new Layer.Awaitable(strand.task, entity, true);
        }
        public static Layer.Awaitable Strand(this System.Threading.Tasks.Task task)
        {
            return new Layer.Awaitable(task);
        }

        public static Layer.Awaitable Lock(this System.Threading.Tasks.Task task, Layer.Frame entity)
        {
            return new Layer.Awaitable(task, entity, true);
        }

        public static Layer.Awaitable Lock(this System.Threading.Tasks.Task task)
        {
            return new Layer.Awaitable(task, true);
        }

        public static Layer.Awaitable<T> Strand<T>(this System.Threading.Tasks.Task<T> task)
        {
            return new Layer.Awaitable<T>(task);
        }
        public static Layer.Awaitable<T> Strand<T>(this System.Threading.Tasks.Task<T> task, Layer.Frame entity)
        {
            return new Layer.Awaitable<T>(task, entity);
        }

        public static Layer.Awaitable<T> Lock<T>(this System.Threading.Tasks.Task<T> task)
        {
            return new Layer.Awaitable<T>(task, true);
        }

        public static Layer.Awaitable<T> Lock<T>(this System.Threading.Tasks.Task<T> task, Layer.Frame entity)
        {
            return new Layer.Awaitable<T>(task, entity, true);
        }

    }

    public partial class Layer
    {
        public struct Awaitable
        {
            Awaiter awaitable { get; set; }

            public Awaitable(System.Threading.Tasks.Task task, bool locked = false)
            {
                awaitable = new Awaiter(task, locked);
            }
            public Awaitable(System.Threading.Tasks.Task task, Frame entity, bool locked = false)
            {
                awaitable = new Awaiter(task, entity, locked);
            }

            public Awaiter GetAwaiter() { return awaitable; }

            public struct Awaiter : INotifyCompletion
            {
                System.Threading.Tasks.Task task { get; set; }
                Frame entity { get; set; }
                public Awaiter(System.Threading.Tasks.Task task, bool locked = false)
                {
                    entity = global::Caspar.Layer.CurrentEntity.Value;
                    this.task = task;
                    if (entity == null)
                    {
                        Logger.Error(new StackTrace());
                        return;
                    }

                    if (locked == true)
                    {
                        entity.Lock(task);
                    }
                }

                public Awaiter(System.Threading.Tasks.Task task, Frame entity, bool locked = false)
                {
                    this.entity = entity;
                    this.task = task;
                    if (entity == null)
                    {
                        Logger.Error("Use Strand. but has not Task.");
                        Logger.Error(new StackTrace());
                        return;
                    }
                    if (locked == true)
                    {
                        entity.Lock(task);
                    }
                }

                public void OnCompleted(Action continuation)
                {
                    // ContinueWith sets the scheduler to use for the continuation action
                    var currentEntity = entity;
                    var currentTask = task;

                    task.ContinueWith(x =>
                    {
                        try
                        {
                            if (currentEntity == null)
                            {
                                continuation();
                            }
                            else
                            {
                                currentEntity.PostContinuation(currentTask, continuation);
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e);
                        }
                        finally
                        {

                        }

                    });
                }

                public bool IsCompleted { get { return task.IsCompleted; } }
                public void GetResult()
                {
                    entity?.Unlock(this.task);
                    if (this.task.Exception != null && this.task.Exception.InnerException != null)
                    {
                        Logger.Error(this.task.Exception.InnerException);
                        throw this.task.Exception.InnerException;
                    }
                }
            }
        }

        public struct Awaitable<T>
        {
            Awaiter awaitable { get; set; }

            public Awaitable(System.Threading.Tasks.Task<T> task, bool locked = false)
            {
                awaitable = new Awaiter(task, locked);
            }
            public Awaitable(System.Threading.Tasks.Task<T> task, Frame entity, bool locked = false)
            {
                awaitable = new Awaiter(task, entity, locked);
            }
            public Awaiter GetAwaiter() { return awaitable; }

            public struct Awaiter : INotifyCompletion
            {
                System.Threading.Tasks.Task<T> task { get; set; }
                Frame entity { get; set; }
                public Awaiter(System.Threading.Tasks.Task<T> task, bool locked = false)
                {
                    this.task = task;
                    entity = global::Caspar.Layer.CurrentEntity.Value;
                    if (entity == null)
                    {
                        Logger.Error(new StackTrace());
                        return;
                    }

                    if (locked == true)
                    {
                        entity.Lock(task);
                    }

                }

                public Awaiter(System.Threading.Tasks.Task<T> task, Frame entity, bool locked)
                {
                    this.entity = entity;
                    this.task = task;
                    if (entity == null)
                    {
                        Logger.Error(new StackTrace());
                        return;
                    }
                    if (locked == true)
                    {
                        entity.Lock(task);
                    }
                }

                public void OnCompleted(Action continuation)
                {
                    // ContinueWith sets the scheduler to use for the continuation action
                    var currentEntity = entity;
                    var currentTask = task;
                    try
                    {
                        task.ContinueWith(x =>
                        {
                            try
                            {
                                if (currentEntity == null)
                                {
                                    continuation();
                                }
                                else
                                {
                                    currentEntity.PostContinuation(currentTask, continuation);
                                }
                            }
                            catch (Exception e)
                            {
                                Logger.Error(e);
                            }
                            finally
                            {
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }

                public bool IsCompleted { get { return task.IsCompleted; } }
                public T GetResult()
                {
                    entity?.Unlock(this.task);
                    if (this.task.Exception != null && this.task.Exception.InnerException != null)
                    {
                        Logger.Error(this.task.Exception.InnerException);
                        throw this.task.Exception.InnerException;
                    }
                    return task.Result;
                }
            }
        }
    }



}

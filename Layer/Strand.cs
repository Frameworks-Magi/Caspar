using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static Framework.Caspar.Api;

namespace Framework.Caspar
{

    public struct AsyncStrandTaskMethodBuilder
    {
        public Strand Task
        {
            get
            {
                return new Strand(builder.Task);
            }
        }

        private AsyncTaskMethodBuilder builder;
        public static AsyncStrandTaskMethodBuilder Create()
        {
            return new AsyncStrandTaskMethodBuilder() { builder = AsyncTaskMethodBuilder.Create() };
        }

        public void SetException(Exception exception)
        {
            builder.SetException(exception);
        }

        public void SetResult()
        {
            builder.SetResult();

        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            builder.AwaitOnCompleted(ref awaiter, ref stateMachine);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
        }
        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            builder.SetStateMachine(stateMachine);

        }
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            builder.Start(ref stateMachine);
        }

    }


    public struct AsyncLockTaskMethodBuilder
    {
        public Lock Task
        {
            get
            {
                return new Lock(builder.Task);
            }
        }

        private AsyncTaskMethodBuilder builder;
        public static AsyncLockTaskMethodBuilder Create()
        {
            return new AsyncLockTaskMethodBuilder() { builder = AsyncTaskMethodBuilder.Create() };
        }

        public void SetException(Exception exception)
        {
            builder.SetException(exception);
        }

        public void SetResult()
        {
            builder.SetResult();

        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            builder.AwaitOnCompleted(ref awaiter, ref stateMachine);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
        }
        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            builder.SetStateMachine(stateMachine);

        }
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            builder.Start(ref stateMachine);
        }

    }

    public struct AsyncStrandTaskMethodBuilder<T>
    {
        public Strand<T> Task
        {
            get
            {
                return new Strand<T>(builder.Task);
            }
        }

        private AsyncTaskMethodBuilder<T> builder;
        public static AsyncStrandTaskMethodBuilder<T> Create()
        {
            return new AsyncStrandTaskMethodBuilder<T>() { builder = AsyncTaskMethodBuilder<T>.Create() };
        }

        public void SetException(Exception exception)
        {
            builder.SetException(exception);
        }

        public void SetResult(T ret)
        {
            builder.SetResult(ret);

        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            builder.AwaitOnCompleted(ref awaiter, ref stateMachine);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
        }
        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            builder.SetStateMachine(stateMachine);

        }
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            builder.Start(ref stateMachine);
        }

    }


    public struct AsyncLockTaskMethodBuilder<T>
    {
        public Lock<T> Task
        {
            get
            {
                return new Lock<T>(builder.Task);
            }
        }

        private AsyncTaskMethodBuilder<T> builder;
        public static AsyncLockTaskMethodBuilder<T> Create()
        {
            return new AsyncLockTaskMethodBuilder<T>() { builder = AsyncTaskMethodBuilder<T>.Create() };
        }

        public void SetException(Exception exception)
        {
            builder.SetException(exception);
        }

        public void SetResult(T ret)
        {
            builder.SetResult(ret);

        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            builder.AwaitOnCompleted(ref awaiter, ref stateMachine);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
        }
        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            builder.SetStateMachine(stateMachine);

        }
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            builder.Start(ref stateMachine);
        }

    }

    [AsyncMethodBuilder(typeof(AsyncStrandTaskMethodBuilder))]
    public struct Strand
    {

        internal System.Threading.Tasks.Task task { get; set; }
        internal bool locked { get; set; }
        public Strand(System.Threading.Tasks.Task task)
        {
            this.locked = false;
            this.task = task;
        }
        public Layer.Awaitable.Awaiter GetAwaiter()
        {
            return new Layer.Awaitable.Awaiter(task);
        }

        public Strand Lock()
        {
            locked = true;
            return this;
        }
    }

    [AsyncMethodBuilder(typeof(AsyncStrandTaskMethodBuilder<>))]
    public struct Strand<T>
    {

        internal System.Threading.Tasks.Task<T> task { get; set; }
        internal bool locked { get; set; }
        public Strand(System.Threading.Tasks.Task<T> task)
        {
            this.task = task;
            this.locked = false;
        }
        public Layer.Awaitable<T>.Awaiter GetAwaiter()
        {
            return new Layer.Awaitable<T>.Awaiter(task, locked);
        }

        public Strand<T> Lock()
        {
            locked = true;
            return this;
        }
    }

    [AsyncMethodBuilder(typeof(AsyncLockTaskMethodBuilder))]
    public struct Lock
    {
        System.Threading.Tasks.Task task { get; set; }
        global::Framework.Caspar.Layer.Entity with { get; set; }
        public Lock(System.Threading.Tasks.Task task)
        {
            this.task = task;
            this.with = null;
        }
        public Layer.Awaitable.Awaiter GetAwaiter()
        {
            if (with != null)
            {
                return new Layer.Awaitable.Awaiter(task, with, true);
            }
            else
            {
                return new Layer.Awaitable.Awaiter(task, true);
            }

        }

        public Lock With(global::Framework.Caspar.Layer.Entity entity)
        {
            with = entity;
            return this;
        }

    }

    [AsyncMethodBuilder(typeof(AsyncLockTaskMethodBuilder<>))]
    public struct Lock<T>
    {

        System.Threading.Tasks.Task<T> task { get; set; }
        global::Framework.Caspar.Layer.Entity with { get; set; }

        public Lock(System.Threading.Tasks.Task<T> task)
        {
            this.task = task;
            this.with = null;
        }
        public Layer.Awaitable<T>.Awaiter GetAwaiter()
        {
            if (with != null)
            {
                return new Layer.Awaitable<T>.Awaiter(task, with, true);
            }
            else
            {
                return new Layer.Awaitable<T>.Awaiter(task, true);
            }

        }
        public Lock<T> With(global::Framework.Caspar.Layer.Entity entity)
        {
            with = entity;
            return this;
        }
    }

}
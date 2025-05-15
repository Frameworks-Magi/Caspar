using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static Caspar.Api;
using MySqlConnector;

namespace Caspar.Database
{

    public interface IParameterizable
    {
        void Clear();
        void AddWithValue(string name, object value);
    }
    public interface ICommandable : IParameterizable
    {
        int ExecuteNonQuery() { return 0; }
        async Task<int> ExecuteNonQueryAsync() { await Task.CompletedTask; return 0; }
        System.Data.Common.DbDataReader ExecuteReader() { return null; }
        async Task<System.Data.Common.DbDataReader> ExecuteReaderAsync() { await Task.CompletedTask; return null; }
        object ExecuteScalar() { return null; }
        async Task<object> ExecuteScalarAsync() { await Task.CompletedTask; return null; }
        MySqlParameterCollection Parameters { get; }
        void Prepare() { }
        string CommandText { get; set; }
        System.Data.CommandType CommandType { get; set; }
        bool IsTransaction { get { return false; } }


    }
    public interface IConnection : IDisposable
    {
        void Initialize();
        void BeginTransaction();
        void Commit();
        Task CommitAsync();
        void Rollback();
        Task RollbackAsync();
        Task<IConnection> Open(CancellationToken token = default);
        void Close();
        void CopyFrom(IConnection value);
        IConnection Create();
        ICommandable CreateCommand() { return null; }
        int IsPoolable() { return 0; }
        bool Ping() { return false; }

    }

    public class Session : SynchronizationContext, IDisposable
    {

        public class Closer
        {

            protected static ConcurrentQueue<(Session, long)> Connections = new();

            internal static long ExpireAt { get; set; } = DateTime.UtcNow.AddMinutes(1).Ticks;
            internal static long Interval { get; set; } = 5;

            public static void Add(Session session)
            {
                Connections.Enqueue((session, ExpireAt));
            }
            public static void Update()
            {
                long now = DateTime.UtcNow.Ticks;
                if (Caspar.Api.Config.Deploy != "PD")
                {
                    ExpireAt = DateTime.UtcNow.AddSeconds(600).Ticks;
                }
                else
                {
                    ExpireAt = DateTime.UtcNow.AddSeconds(Interval).Ticks;
                }

                while (Connections.Count > 0)
                {
                    try
                    {
                        if (Connections.TryPeek(out var item) == false)
                        {
                            break;
                        }

                        if (item.Item2 > now) { break; }

                        if (Connections.TryDequeue(out item) == false)
                        {
                            break;
                        }
                        if (item.Item1.IsDisposed == true) { continue; }

                        Logger.Info($"Session is not disposed.");
                        item.Item1.Log();
                        item.Item1.Rollback();
                        item.Item1.Dispose();
                    }
                    catch
                    {

                    }

                }
            }

        }
        public class RollbackException : System.Exception
        {
            public int ErrorCode { get; set; }
        }

        public string Trace = string.Empty;

        public static Amazon.DynamoDBv2.AmazonDynamoDBClient DynamoDB
        {
            get
            {
                Driver.Databases.TryGetValue("DynamoDB", out var connection);
                return (connection as global::Caspar.Database.NoSql.DynamoDB).GetClient();
            }
        }

        public static global::Caspar.Database.NoSql.Redis Redis
        {
            get
            {
                Driver.Databases.TryGetValue("Redis", out var connection);
                return (connection as Database.NoSql.Redis);
            }
        }

        public static ThreadLocal<Session> CurrentSession = new ThreadLocal<Session>();

        private IConnection _connection = null;

        public static async ValueTask<IConnection> GetConnection(string name)
        {
            var session = Session.CurrentSession.Value;
            if (session == null) { return null; }
            var connection = await session.GetConnection(name, true);
            if (connection == null) { return null; }
            return connection;
        }

        public static IConnection Connection
        {
            get
            {
                var session = Session.CurrentSession.Value;
                return session._connection;
            }
        }

        public static ICommandable CreateCommandable()
        {
            return Session.Connection.CreateCommand();
        }

        public static async ValueTask<ICommandable> GetCommandable(string name)
        {
            var session = Session.CurrentSession.Value;
            if (session == null)
            {
                Logger.Error($"Session is null Session UID: {session.UID} ThreadId: {Thread.CurrentThread.ManagedThreadId}");
                return null;
            }
            var connection = await session.GetConnection(name);
            if (connection == null)
            {
                Logger.Error($"Connection is null Session UID: {session.UID} ThreadId: {Thread.CurrentThread.ManagedThreadId}");
                return null;
            }
            return connection.CreateCommand();
        }


        public static async ValueTask<Session> Create(string name, bool transaction = true)
        {
            var session = new Session();
            var connection = await session.GetConnection(name);
            if (connection == null)
            {
                Logger.Error($"Connection2 is null Session UID: {session.UID} ThreadId: {Thread.CurrentThread.ManagedThreadId}");
                return null;
            }
            if (transaction == true)
            {
                session.BeginTransaction();
            }
            return session;
        }

        public static Session Create()
        {
            var session = new Session();
            return session;
        }
        private SynchronizationContext parentContext = null;

        public Session()
        {
            parentContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(this);
            CurrentSession.Value = this;

            if (Layer.CurrentEntity.Value == null)
            {
                UID = global::Caspar.Api.UniqueKey;
                Closer.Add(this);
            }
            else
            {
                UID = Layer.CurrentEntity.Value.UID;
                frame = Layer.CurrentEntity.Value;
                Layer.CurrentEntity.Value.Add(this);
            }
        }


        public override void Post(SendOrPostCallback d, object? state)
        {
            if (frame != null)
            {
                frame.PostMessage(() =>
                {
                    SynchronizationContext.SetSynchronizationContext(this);
                    Session.CurrentSession.Value = this;
                    Layer.CurrentEntity.Value = frame;
                    d(state);
                });
            }
            else
            {
                Logger.Info($"Post Message from Session but frame is null");
                ThreadPool.QueueUserWorkItem(static s =>
                {
                    Logger.Info($"Post Message processing from Session but frame is null");
                    var tuple = s as Tuple<Session, SendOrPostCallback, object>;
                    var context = tuple.Item1;
                    SynchronizationContext.SetSynchronizationContext(context);
                    Session.CurrentSession.Value = context;
                    tuple.Item2(tuple.Item3);
                }, new Tuple<Session, SendOrPostCallback, object>(this, d, state));
            }
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            Logger.Info($"Send Message from Session");
            SynchronizationContext.SetSynchronizationContext(this);
            Session.CurrentSession.Value = this;
            Layer.CurrentEntity.Value = frame;
            base.Send(d, state);
        }

        internal Layer.Frame frame { get; set; }

        public Session(Layer.Frame entity)
        {
            //   Command = async () => { await ValueTask.CompletedTask; };
            UID = entity.UID;
            frame = entity;
            entity.Add(this);

        }

        public void BeginTransaction()
        {
            foreach (var e in _connections.Values)
            {
                try
                {
                    e.BeginTransaction();
                }
                catch
                {

                }
            }
        }


        public void Rollback()
        {
            foreach (var e in _connections.Values)
            {
                try
                {
                    e.Rollback();
                }
                catch
                {

                }
            }
        }

        public async Task RollbackAsync()
        {
            foreach (var e in _connections.Values)
            {
                try
                {
                    await e.RollbackAsync();
                }
                catch
                {

                }
            }
        }

        public void Commit()
        {
            foreach (var e in _connections.Values)
            {
                try
                {
                    e.Commit();
                }
                catch
                {

                }
            }
        }

        public async Task CommitAsync()
        {
            foreach (var e in _connections.Values)
            {
                try
                {
                    await e.CommitAsync();
                }
                catch
                {

                }
            }
        }

        internal void Close()
        {
            try
            {
                foreach (var e in _connections.Values)
                {
                    try
                    {
                        e.Dispose();
                    }
                    catch
                    {

                    }
                }
                _connections.Clear();
            }
            catch
            {

            }

            try
            {
                TCS?.SetResult();
                TCS = null;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

        }
        private int _disposed = 0;
        public bool IsDisposed => _disposed == 1;

        internal void Log()
        {
            foreach (var connection in _connections.Values)
            {
                if (connection is Management.Relational.MySql)
                {
                    var conn = connection as Management.Relational.MySql;
                    if (conn.Command != null)
                    {
                        Logger.Error($"[Dispose Session] {conn.Command.CommandText}");
                    }
                }
            }
        }
        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
            {
                return;
            }
            SynchronizationContext.SetSynchronizationContext(parentContext);
            Session.CurrentSession.Value = null;
            parentContext = null;

            try
            {
                frame?.Remove(this);
            }
            catch
            {

            }

            frame = null;

            try
            {
                Rollback();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            try
            {
                Close();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }


        }


        internal TaskCompletionSource TCS { get; set; } = null;

        public static ConcurrentQueue<Session> Timeouts = new();
        private Dictionary<string, IConnection> _connections = new();

        public async ValueTask<IConnection> GetConnection(string name, bool open = true)
        {
            try
            {
                if (this.IsDisposed == true)
                {
                    Logger.Error($"GetConnection but Session is disposed. UID: {UID} ThreadId: {Thread.CurrentThread.ManagedThreadId}");
                    return null;
                }
                if (_connections.TryGetValue(name, out var connection) == true)
                {
                    return connection;
                }
                if (Driver.Databases.TryGetValue(name, out connection) == false)
                {
                    Logger.Error($"Database {name} is not configuration");
                    return null;
                }

                connection = connection.Create();
                _connections.Add(name, connection);
                _connection ??= connection;
                await connection.Open(this.CancellationToken);
                return connection;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public Amazon.DynamoDBv2.AmazonDynamoDBClient GetDynamoDB(string db = "DynamoDB")
        {
            Driver.Databases.TryGetValue(db, out var connection);
            return (connection as global::Caspar.Database.NoSql.DynamoDB).GetClient();
        }

        // public static Amazon.DynamoDBv2.AmazonDynamoDBClient GetDynamoDBClient(string db)
        // {
        //     Driver.Databases.TryGetValue(db, out var connection);
        //     return (connection as Caspar.Database.NoSql.DynamoDB).GetClient();
        // }

        public dynamic ResultSet { get; set; } = Singleton<Caspar.Database.ResultSet>.Instance;
        public long RecordsAffected { get; set; }
        public Action ResponseCallBack { get; set; }
        public virtual string Host { get; }

        //  internal Func<Task> Command { get; set; }
        public int Error { get; protected set; }
        public System.Exception Exception { get; protected set; }
        public int Strand { get; set; }
        public long UID { get; set; }
        public DateTime Timeout { get; internal set; }
        internal protected System.Threading.CancellationTokenSource CTS { get; private set; } = new CancellationTokenSource();
        public System.Threading.CancellationToken CancellationToken => CTS.Token;


        internal protected virtual void SetResult(int result)
        {
            this.Error = result;
            TCS?.SetResult();
        }

        internal protected virtual void SetException(Exception e)
        {
            this.Exception = e;
            this.Error = -1;// e.HResult;
            TCS?.SetException(e);

        }

        public virtual IEnumerable<string> GetHost()
        {
            yield break;
        }

    }
}

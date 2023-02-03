using Framework.Caspar;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Framework.Caspar.Api;
using Framework.Caspar.Container;
using MySqlConnector;

namespace Framework.Caspar.Database
{
    public interface IQueryable
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


    }
    public interface IConnection : IDisposable
    {
        void Initialize();
        void BeginTransaction();
        void Commit();
        void Rollback();
        Task<IConnection> Open(CancellationToken token = default, bool transaction = true);
        void Close();
        void CopyFrom(IConnection value);
        IConnection Create();
        IQueryable CreateCommand() { return null; }
        int IsPoolable() { return 0; }
        bool Ping() { return false; }

    }

    public class Session : IDisposable
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
                ExpireAt = DateTime.UtcNow.AddSeconds(Interval).Ticks;

                while (Connections.Count > 0)
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

                    Logger.Info("Session is not disposed.");
                    item.Item1.Dispose();
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
                return (connection as global::Framework.Caspar.Database.NoSql.DynamoDB).GetClient();
            }
        }

        public static global::Framework.Caspar.Database.NoSql.Redis Redis
        {
            get
            {
                Driver.Databases.TryGetValue("Redis", out var connection);
                return (connection as Database.NoSql.Redis);
            }
        }

        public Session()
        {
            Command = async () => { await ValueTask.CompletedTask; };
            if (Layer.CurrentEntity.Value == null)
            {
                UID = global::Framework.Caspar.Api.UniqueKey;
            }
            else
            {
                UID = Layer.CurrentEntity.Value.UID;
            }

            Closer.Add(this);

        }


        public void Rollback()
        {
            foreach (var e in connections)
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

        public void Commit()
        {
            foreach (var e in connections)
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

        internal void Close()
        {
            try
            {
                foreach (var e in connections)
                {
                    try
                    {
                        e.Dispose();
                    }
                    catch
                    {

                    }
                }
                connections.Clear();
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
        private int disposed = 0;
        public bool IsDisposed => disposed == 1;
        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref disposed, 1, 0) != 0)
            {
                return;
            }
            try
            {
                if (AutoCommit == true)
                {
                    Commit();
                }
                else
                {
                    Rollback();
                }
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

        List<IConnection> connections { get; set; } = new List<IConnection>();
        public bool AutoCommit { get; set; } = true;
        //static ConcurrentDictionary<string, ConcurrentBag<IConnection>> connections = new();

        internal TaskCompletionSource TCS { get; set; } = null;

        // static async public Task<Session> Create(bool autoCommit = true)
        // {
        //     var session = new Session() { autoCommit = autoCommit };
        //     session.TCS = new();
        //     while (true)
        //     {
        //         try
        //         {
        //             if (progresses.TryGetValue(session.UID, out var old) == true)
        //             {
        //                 await old.TCS.Task;
        //             }

        //             if (progresses.TryAdd(session.UID, session) == true) { break; }
        //         }
        //         catch (Exception ex)
        //         {
        //             Logger.Error(ex);
        //         }
        //     }
        //     return session;
        // }

        //   public static ConcurrentDictionary<string, BlockingCollection<IConnection>> sessions = new();

        // static async internal Task<IConnection> GetConnection(string db, CancellationToken token, bool transaction = true)
        // {





        //     // //connections.GetOrCreate(db).Add()
        //     // return await Task.Run(async () =>
        //     // {
        //     //     var sw = System.Diagnostics.Stopwatch.StartNew();
        //     //     IConnection session;
        //     //     if (Driver.Databases.TryGetValue(db, out session) == true)
        //     //     {
        //     //         session = session.Create();
        //     //         await session.Open(token, transaction);
        //     //         return session;
        //     //     }
        //     //     return null;
        //     // });
        // }

        public static ConcurrentQueue<Session> Timeouts = new();

        public async Task<IConnection> GetConnection(string name, bool open = true, bool transaction = true)
        {
            try
            {
                // if (IsValid == false) { return null; }
                // if (Driver.Connections.TryGetValue(name, out var queue) == false)
                // {
                //     return null;
                // }

                if (Driver.Databases.TryGetValue(name, out var connection) == false)
                {
                    Logger.Error($"Database {name} is not configuration");
                    return null;
                }

                // if (connection.IsPoolable() > 0)
                // {
                //     if (Driver.ConnectionPools[name].TryDequeue(out connection) == true)
                //     {
                //         var mysql = (connection as Framework.Caspar.Database.Management.Relational.MySql);
                //         if (mysql != null)
                //         {
                //             mysql.disposed = 0;
                //         }
                //         connections.Add(connection);
                //         Logger.Info($"Connection allocate from pool {name}, {Driver.ConnectionPools[name].Count}");
                //         return connection;
                //     }
                // }

                connection = connection.Create();
                await connection.Open(this.CancellationToken, transaction);
                connections.Add(connection);
                return connection;
                // // //             CTS.CancelAfter(1000);
                // // var session = queue.Take();

                // return await Task.Run(async () =>
                // {
                //     try
                //     {
                //         //        CTS.CancelAfter(1000);
                //         //     var sw = System.Diagnostics.Stopwatch.StartNew();
                //         IConnection connection;
                //         connection = session.Create();
                //         await connection.Open(this.CancellationToken, transaction);
                //         connections.Add(connection);
                //         return connection;
                //     }
                //     catch (Exception e)
                //     {
                //         Logger.Error(e);
                //         return null;
                //     }

                // });
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }

            // var ret = await GetConnection(name, this.CancellationToken, transaction);
            // connections.Add(ret);
            // return ret;

        }

        public Amazon.DynamoDBv2.AmazonDynamoDBClient GetDynamoDB(string db = "DynamoDB")
        {
            Driver.Databases.TryGetValue(db, out var connection);
            return (connection as global::Framework.Caspar.Database.NoSql.DynamoDB).GetClient();
        }

        // public static Amazon.DynamoDBv2.AmazonDynamoDBClient GetDynamoDBClient(string db)
        // {
        //     Driver.Databases.TryGetValue(db, out var connection);
        //     return (connection as Framework.Caspar.Database.NoSql.DynamoDB).GetClient();
        // }

        public dynamic ResultSet { get; set; } = Singleton<Caspar.Database.ResultSet>.Instance;
        public long RecordsAffected { get; set; }
        public Action ResponseCallBack { get; set; }
        public virtual string Host { get; }

        internal Func<Task> Command { get; set; }
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
            //if (TCS == null)
            //{
            //    task?.PostMessage(ResponseCallBack);
            //    return;
            //}
            TCS?.SetResult();
        }

        internal protected virtual void SetException(Exception e)
        {
            this.Exception = e;
            this.Error = -1;// e.HResult;
            TCS?.SetException(e);

        }


        // public virtual async Task ExecuteAsync()
        // {
        //     if (TCS != null) { throw new Exception("ExecuteAsync twice"); }
        //     if (Trace.IsNullOrEmpty() == true && Layer.CurrentTask.Value != null)
        //     {
        //         Trace = Layer.CurrentTask.Value.GetType().FullName;
        //     }
        //     Driver.sessions.TryGetValue(UID, out ConcurrentQueue<Session> queue);
        //     if (queue == null)
        //     {
        //         queue = new ConcurrentQueue<Session>();
        //         Driver.sessions.TryAdd(UID, queue);
        //     }
        //     {
        //         queue.Enqueue(this);
        //     }

        //     TCS = new global::System.Threading.Tasks.TaskCompletionSource();
        //     await TCS.Task;
        // }


        public virtual IEnumerable<string> GetHost()
        {
            yield break;
        }

    }
}

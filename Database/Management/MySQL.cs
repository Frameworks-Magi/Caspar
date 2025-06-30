using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MySql.Data;
using MySqlConnector;
//using MySql.Data.MySqlClient;
using System.Threading;
using System.Data;
using System.Collections;
using static Caspar.Api;
using Amazon;
using System.Data.SqlClient;
using System.Data.Odbc;

namespace Caspar.Database.Management.Relational
{
    public sealed class MySql : IConnection
    {
        public sealed class Queryable : ICommandable
        {
            public bool IsTransaction { get { return Command.Transaction != null; } }
            public int ExecuteNonQuery()
            {

                var sw = System.Diagnostics.Stopwatch.StartNew();
                var ret = Command.ExecuteNonQuery();
                long ms = sw.ElapsedMilliseconds;
                if (ms > global::Caspar.Extensions.Database.SlowQueryMilliseconds)
                {
                    Logger.Info($"{Command.CommandText} - {ms}ms");
                }
                return ret;
            }
            public System.Data.Common.DbDataReader ExecuteReader()
            {

                var sw = System.Diagnostics.Stopwatch.StartNew();
                var ret = Command.ExecuteReader();
                long ms = sw.ElapsedMilliseconds;
                if (ms > global::Caspar.Extensions.Database.SlowQueryMilliseconds)
                {
                    Logger.Info($"{Command.CommandText} - {ms}ms");
                }
                return ret;

            }
            public object ExecuteScalar()
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var ret = Command.ExecuteScalar();
                long ms = sw.ElapsedMilliseconds;
                if (ms > global::Caspar.Extensions.Database.SlowQueryMilliseconds)
                {
                    Logger.Info($"{Command.CommandText} - {ms}ms");
                }
                return ret;
            }



            public async Task<int> ExecuteNonQueryAsync()
            {
                // var query = new ExecuteNonQuery();
                // query.command = Command;
                // Queries.Add(query);
                // return await query.TCS.Task;
                return await Task.Run(() =>
                {
                    return Command.ExecuteNonQuery();
                });

            }
            public async Task<System.Data.Common.DbDataReader> ExecuteReaderAsync()
            {
                return await Task.Run(() =>
                {
                    return Command.ExecuteReader();
                });
                //return await Command.ExecuteReaderAsync();

                // return await Task.Run(() =>
                // {
                //     try
                //     {
                //         var sw = System.Diagnostics.Stopwatch.StartNew();
                //         var ret = Command.ExecuteReader();
                //         long ms = sw.ElapsedMilliseconds;
                //         if (ms > global::Caspar.Extensions.Database.SlowQueryMilliseconds)
                //         {
                //             Logger.Info($"{Command.CommandText} - {ms}ms");
                //         }
                //         return ret;
                //     }
                //     catch
                //     {
                //         throw;
                //     }
                // });
            }
            public async Task<object> ExecuteScalarAsync()
            {
                return await Task.Run(() =>
                {
                    return Command.ExecuteScalar();
                });
                //return await Command.ExecuteScalarAsync();

                // return await Task.Run(() =>
                // {
                //     try
                //     {
                //         var sw = System.Diagnostics.Stopwatch.StartNew();
                //         var ret = Command.ExecuteScalar();
                //         long ms = sw.ElapsedMilliseconds;
                //         if (ms > global::Caspar.Extensions.Database.SlowQueryMilliseconds)
                //         {
                //             Logger.Info($"{Command.CommandText} - {ms}ms");
                //         }
                //         return ret;
                //     }
                //     catch
                //     {
                //         throw;
                //     }

                // });
            }
            public void AddWithValue(string name, object value)
            {
                Command.Parameters.AddWithValue(name, value);
            }
            public void Clear()
            {
                Command.Parameters.Clear();
            }

            public MySqlCommand Command { get; internal set; }
            public MySqlParameterCollection Parameters => Command.Parameters;
            public void Prepare() { Command.Prepare(); }
            public string CommandText { get { return Command.CommandText; } set { Command.CommandText = value; } }
            public System.Data.CommandType CommandType { get { return Command.CommandType; } set { Command.CommandType = value; } }
        }



        //  public class Session : Driver.Session {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Pw { get; set; }
        public string Ip { get; set; }
        public string Port { get; set; }
        public string Db { get; set; }
        public MySqlConnection Connection { get; set; }
        public MySqlTransaction Transaction { get; set; }
        public MySqlCommand Command { get; set; }
        private string connectionStringValue;
        internal int MaxSession { get; set; } = 0;

        //public static async Task<MySql> Session(string db)
        //{
        //    return await GetSession("Game", true, false);
        //}

        //public int IsPoolable() { return MaxSession; }
        public int IsPoolable() { return MaxSession; }
        public bool Ping()
        {
            if (Connection == null)
            {
                return false;
            }
            else
            {
                return Connection.Ping();
            }
        }
        public IConnection Create()
        {
            var session = new MySql();
            session.connectionStringValue = connectionStringValue;
            session.IAM = IAM;
            session.Id = Id;
            session.Pw = Pw;
            session.Ip = Ip;
            session.Port = Port;
            session.Db = Db;
            session.Name = Name;
            session.MaxSession = MaxSession;
            return session;
        }


        public ICommandable CreateCommand()
        {
            if (Command == null)
            {
                Command = Connection.CreateCommand();
                Command.Transaction = Transaction;
            }
            Command.CommandType = CommandType.Text;
            Command.CommandText = "";
            Command.Parameters.Clear();
            return new Queryable() { Command = Command };
        }


        public void BeginTransaction()
        {
            if (Transaction == null)
            {
                Transaction = Connection.BeginTransaction();

            }

            if (Command != null)
            {
                Command.Transaction = Transaction;
            }
        }

        public async Task CommitAsync()
        {
            await Transaction?.CommitAsync();
            Transaction = null;
            if (Command != null)
            {
                Command.Transaction = null;
            }
        }

        public void Commit()
        {
            Transaction?.Commit();
            Transaction = null;
            if (Command != null)
            {
                Command.Transaction = null;
            }
        }

        public void Rollback()
        {
            Transaction?.Rollback();
            Transaction = null;
            if (Command != null)
            {
                Command.Transaction = null;
            }
        }

        public async Task RollbackAsync()
        {
            await Transaction?.RollbackAsync();
            Transaction = null;
            if (Command != null)
            {
                Command.Transaction = null;
            }
        }

        public bool IAM { get; set; } = false;
        //  internal DateTime InitializedAt { get; set; } = DateTime.UtcNow;


        public void Initialize()
        {

            if (Database.Driver.Databases.TryGetValue(Name, out var session) == false)
            {
                return;
            }
            if (session != null && session is MySql)
            {
                {
                    var connectionString = new MySqlConnectionStringBuilder();
                    connectionString.UserID = Id;
                    connectionString.Server = Ip;
                    connectionString.Port = Convert.ToUInt32(Port);
                    connectionString.Database = Db;

                    connectionString.Pooling = true;
                    connectionString.MinimumPoolSize = 2;
                    connectionString.MaximumPoolSize = (uint)MaxSession;

                    if (connectionString.MaximumPoolSize > Api.MaxSession)
                    {
                        connectionString.MaximumPoolSize = (uint)Api.MaxSession;
                    }

                    if (connectionString.MaximumPoolSize < connectionString.MinimumPoolSize)
                    {
                        connectionString.MaximumPoolSize = connectionString.MinimumPoolSize;
                    }

                    // if (MaxSession > 32)
                    // {
                    //     connectionString.MaximumPoolSize = (uint)MaxSession;
                    // }
                    // else
                    // {
                    //     connectionString.MaximumPoolSize = 32;
                    // }


                    if (Caspar.Api.ServerType == "Agent")
                    {
                        connectionString.MinimumPoolSize = 1;
                        connectionString.MaximumPoolSize = 8;
                    }
                    Logger.Info($"Database Session Initialize {Name} MaximumPoolSize is {connectionString.MaximumPoolSize}");

                    connectionString.ConnectionIdleTimeout = 60;
                    connectionString.ConnectionTimeout = 60;

                    connectionString.AllowZeroDateTime = true;
                    connectionString.CharacterSet = "utf8";
                    connectionStringValue = connectionString.ToString();//.GetConnectionString(true);
                }
            }
        }


        public async Task<IConnection> Open(CancellationToken token = default)
        {
            try
            {
                if (Connection == null)
                {
                    Connection = new MySqlConnection(connectionStringValue);
                    Connection.ProvidePasswordCallback = (context) =>
                    {
                        if (IAM == true)
                        {
                            var awsCredentials = new Amazon.Runtime.BasicAWSCredentials((string)global::Caspar.Api.Config.AWS.Access.KeyId, (string)global::Caspar.Api.Config.AWS.Access.SecretAccessKey);
                            var pwd = Amazon.RDS.Util.RDSAuthTokenGenerator.GenerateAuthToken(awsCredentials, Ip, 3306, Id);
                            // Logger.Info("mysql ProvidePasswordCallback");
                            // Logger.Info($"connectionStringValue: {connectionStringValue}");
                            // Logger.Info($"password: {pwd}");
                            return pwd;
                        }
                        else
                        {
                            return Pw;
                        }
                    };

                    if (Connection.State != ConnectionState.Open)
                    {
                        await Task.Run(() =>
                                           {
                                               Connection.Open();
                                           });
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                Connection?.Close();
                Connection?.Dispose();
                Connection = null;
                Dispose();
                throw;
            }
            return this;
        }

        public void Close()
        {
            try
            {
                Rollback();
            }
            catch (Exception ex)
            {
                global::Caspar.Api.Logger.Info("Driver Level Rollback Exception " + ex);
            }

        }
        private int disposed = 0;
        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref disposed, 1, 0) != 0)
            {
                return;
            }
            Close();

            Command?.Dispose();
            Command = null;

            Transaction?.Dispose();
            Transaction = null;

            Connection?.Close();
            Connection?.Dispose();
            Connection = null;
        }


        public void CopyFrom(IConnection value)
        {

            var rhs = value as MySql;
            if (rhs == null) { return; }

            Name = rhs.Name;
            Id = rhs.Id;
            Pw = rhs.Pw;
            Ip = rhs.Ip;
            Port = rhs.Port;
            Db = rhs.Db;

        }

    }
}

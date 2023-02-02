using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Threading;
using System.Data;
using System.Collections;
using static Framework.Caspar.Api;
using Amazon;

namespace Framework.Caspar.Database.Management.Relational
{


    public sealed class MySql : IConnection
    {
        public sealed class Commandable : ICommand
        {
            public int ExecuteNonQuery()
            {

                var sw = System.Diagnostics.Stopwatch.StartNew();
                var ret = Command.ExecuteNonQuery();
                long ms = sw.ElapsedMilliseconds;
                if (ms > global::Framework.Caspar.Extensions.Database.SlowQueryMilliseconds)
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
                if (ms > global::Framework.Caspar.Extensions.Database.SlowQueryMilliseconds)
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
                if (ms > global::Framework.Caspar.Extensions.Database.SlowQueryMilliseconds)
                {
                    Logger.Info($"{Command.CommandText} - {ms}ms");
                }
                return ret;
            }
            public async Task<int> ExecuteNonQueryAsync()
            {
                return await Task.Run(() =>
                {
                    try
                    {
                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        var ret = Command.ExecuteNonQuery();
                        long ms = sw.ElapsedMilliseconds;
                        if (ms > global::Framework.Caspar.Extensions.Database.SlowQueryMilliseconds)
                        {
                            Logger.Info($"{Command.CommandText} - {ms}ms");
                        }
                        return ret;
                    }
                    catch
                    {
                        throw;
                    }

                });
            }
            public async Task<System.Data.Common.DbDataReader> ExecuteReaderAsync()
            {
                return await Task.Run(() =>
                {
                    try
                    {
                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        var ret = Command.ExecuteReader();
                        long ms = sw.ElapsedMilliseconds;
                        if (ms > global::Framework.Caspar.Extensions.Database.SlowQueryMilliseconds)
                        {
                            Logger.Info($"{Command.CommandText} - {ms}ms");
                        }
                        return ret;
                    }
                    catch
                    {
                        throw;
                    }
                });
            }
            public async Task<object> ExecuteScalarAsync()
            {
                return await Task.Run(() =>
                {
                    try
                    {
                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        var ret = Command.ExecuteScalar();
                        long ms = sw.ElapsedMilliseconds;
                        if (ms > global::Framework.Caspar.Extensions.Database.SlowQueryMilliseconds)
                        {
                            Logger.Info($"{Command.CommandText} - {ms}ms");
                        }
                        return ret;
                    }
                    catch
                    {
                        throw;
                    }

                });
            }
            public MySqlCommand Command { get; internal set; }
            public global::MySql.Data.MySqlClient.MySqlParameterCollection Parameters => Command.Parameters;
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
            session.MaxSession = IsPoolable();
            return session;
        }


        //      public async Task<MySqlCommand> CreateCommand(string text, CommandType type, CancellationToken token = default)
        //{
        //          if (Connection == null)
        //          {
        //              await Open(token);
        //          }

        //          if (Command == null)
        //          {
        //              Command = Connection.CreateCommand();
        //              Command.Transaction = Transaction;
        //          }

        //          Command.CommandText = text;
        //          Command.CommandType = type;
        //          Command.Parameters.Clear();



        //          return Command;
        //}

        public ICommand CreateCommand()
        {
            if (Command == null)
            {
                Command = Connection.CreateCommand();
                Command.Transaction = Transaction;
            }

            Command.CommandType = CommandType.Text;
            Command.CommandText = "";
            Command.Parameters.Clear();
            return new Commandable() { Command = Command };
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

        public bool IAM { get; set; } = false;
        internal DateTime InitializedAt { get; set; } = DateTime.UtcNow;

        public void Initialize()
        {

            if (Database.Driver.Databases.TryGetValue(Name, out var session) == false)
            {
                return;
            }
            if (session != null && session is MySql)
            {
                connectionStringValue = (session as MySql).connectionStringValue;
                if ((session as MySql).InitializedAt > DateTime.UtcNow) { return; }
                lock (session)
                {
                    var connectionString = new MySqlConnectionStringBuilder();
                    connectionString.UserID = Id;
                    connectionString.Password = Pw;

                    try
                    {
                        if (IAM == true)
                        {
                            var awsCredentials = new Amazon.Runtime.BasicAWSCredentials((string)global::Framework.Caspar.Api.Config.AWS.Access.KeyId, (string)global::Framework.Caspar.Api.Config.AWS.Access.SecretAccessKey);
                            var pwd = Amazon.RDS.Util.RDSAuthTokenGenerator.GenerateAuthToken(awsCredentials, Ip, 3306, Id);
                            connectionString.SslMode = MySqlSslMode.Required;
                            connectionString.Password = pwd;
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                    }

                    connectionString.Server = Ip;
                    connectionString.Port = Convert.ToUInt32(Port);
                    connectionString.Database = Db;
                    connectionString.Pooling = false;
                    connectionString.AllowZeroDateTime = true;
                    connectionString.CharacterSet = "utf8";
                    connectionString.CheckParameters = false;

                    (session as MySql).connectionStringValue = connectionString.GetConnectionString(true);
                    (session as MySql).InitializedAt = DateTime.UtcNow.AddMinutes(10);
                }
                connectionStringValue = (session as MySql).connectionStringValue;
            }

        }

        public void Dispose()
        {

            Command?.Dispose();
            Command = null;

            Transaction?.Dispose();
            Transaction = null;

            // return or close
            int max = IsPoolable();

            if (Connection != null && max > 0 && Driver.ConnectionPools[Name].Count < max)
            {
                Driver.ConnectionPools[Name].Enqueue(this);
                return;
            }
            Connection?.Close();
            Connection?.Dispose();
            Connection = null;

            //GC.SuppressFinalize(this);
        }
        public async Task<IConnection> Open(CancellationToken token = default, bool transaction = true)
        {
            try
            {
                if (Connection == null)
                {
                    Initialize();
                    Connection = new MySqlConnection(connectionStringValue);
                    await Connection.OpenAsync();
                    // await Task.Run(async () =>
                    // {
                    //     int max = 1;
                    //     while (true)
                    //     {
                    //         try
                    //         {

                    //             //Logger.Info($"Opened: {connectionStringValue}");

                    //             //Session.Closer.Add(this);
                    //             return;
                    //         }
                    //         catch (Exception e)
                    //         {
                    //             Logger.Info($"Error: {connectionStringValue}");
                    //             //   Logger.Error(e);
                    //             max -= 1;
                    //             Close();
                    //             Dispose();
                    //             await Task.Delay(100);
                    //             try
                    //             {
                    //                 if (IAM == true)
                    //                 {
                    //                     Initialize();
                    //                 }
                    //             }
                    //             catch (Exception ex)
                    //             {
                    //                 Logger.Error(ex);
                    //             }
                    //             if (max < 0) { throw; }
                    //         }
                    //     }
                    // });
                }

                if (transaction == true)
                {
                    BeginTransaction();
                }
            }
            catch
            {
                Logger.Error(connectionStringValue);
                Connection?.Close();
                Connection?.Dispose();
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
                global::Framework.Caspar.Api.Logger.Info("Driver Level Rollback Exception " + ex);
            }

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

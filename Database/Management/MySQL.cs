﻿using System;
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
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    var ret = Command.ExecuteNonQuery();
                    long ms = sw.ElapsedMilliseconds;
                    if (ms > global::Framework.Caspar.Extensions.Database.SlowQueryMilliseconds)
                    {
                        Logger.Info($"{Command.CommandText} - {ms}ms");
                    }
                    return ret;
                });
            }
            public async Task<System.Data.Common.DbDataReader> ExecuteReaderAsync()
            {
                return await Task.Run(() =>
                {

                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    var ret = Command.ExecuteReader();
                    long ms = sw.ElapsedMilliseconds;
                    if (ms > global::Framework.Caspar.Extensions.Database.SlowQueryMilliseconds)
                    {
                        Logger.Info($"{Command.CommandText} - {ms}ms");
                    }
                    return ret;
                });
            }
            public async Task<object> ExecuteScalarAsync()
            {
                return await Task.Run(() =>
                {
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    var ret = Command.ExecuteScalar();
                    long ms = sw.ElapsedMilliseconds;
                    if (ms > global::Framework.Caspar.Extensions.Database.SlowQueryMilliseconds)
                    {
                        Logger.Info($"{Command.CommandText} - {ms}ms");
                    }
                    return ret;
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

        //public static async Task<MySql> Session(string db)
        //{
        //    return await GetSession("Game", true, false);
        //}

        public IConnection Create()
        {
            var session = new MySql();
            session.connectionStringValue = connectionStringValue;
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

        public void Initialize()
        {
            MySqlConnectionStringBuilder connectionString = new MySqlConnectionStringBuilder();

            connectionString.UserID = Id;
            connectionString.Password = Pw;
            connectionString.Server = Ip;
            connectionString.Port = Convert.ToUInt32(Port);
            connectionString.Database = Db;
            connectionString.Pooling = true;
            connectionString.AllowZeroDateTime = true;
            connectionString.CharacterSet = "utf8";
            connectionString.CheckParameters = false;
            connectionString.UseCompression = true;
            connectionString.ConnectionTimeout = 10;
            connectionString.MinimumPoolSize = 1;
            connectionString.MaximumPoolSize = (uint)Api.MaxSession;
            connectionString.SslMode = MySqlSslMode.Required;
            connectionStringValue = connectionString.GetConnectionString(true);

        }

        public void Dispose()
        {
            Command?.Dispose();
            Command = null;

            Transaction?.Dispose();
            Transaction = null;

            Connection?.Dispose();
            Connection = null;

            GC.SuppressFinalize(this);

        }
        public async Task<IConnection> Open(CancellationToken token = default, bool transaction = true)
        {
            int max = 3;
            while (true)
            {
                try
                {
                    if (Connection == null)
                    {
                        await Task.Run(() =>
                        {
                            Connection = new MySqlConnection(connectionStringValue);
                            Connection.Open();
                        });

                    }

                    if (transaction == true)
                    {
                        BeginTransaction();
                    }
                }
                catch
                {
                    await Task.Delay(100);
                    max -= 1;
                    Close();
                    Dispose();
                    if (max < 0) { throw; }
                    continue;
                }
                break;
            }
            return this;
        }
        public void Close()
        {
            try
            {
                Rollback();
                Connection?.Close();
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

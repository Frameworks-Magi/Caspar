// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;
// using System.IO;
// using MySql.Data;
// using System.Threading;
// using System.Data;
// using System.Collections;
// using static Caspar.Api;
// //using MySqlConnector;
// using MySql.Data.MySqlClient;

// namespace Caspar.Database.Management.Relational
// {
//     public sealed class Odbc : IConnection
//     {


//         public interface IQuery
//         {
//             void Execute() { }
//         }

//         public class OpenConnection : IQuery
//         {
//             public void Execute()
//             {

//             }
//         }

//         public class ExecuteNonQuery : IQuery
//         {
//             internal global::System.Threading.Tasks.TaskCompletionSource<int> TCS = new();
//             internal MySqlConnector.MySqlCommand command { get; set; }
//             public void Execute()
//             {
//                 try
//                 {
//                     var ret = command.ExecuteNonQueryAsync();


//                     //    TCS.SetResult(ret);
//                 }
//                 catch (Exception e)
//                 {
//                     //                    TCS.SetException(e);
//                 }

//             }
//         }


//         internal System.Collections.Concurrent.BlockingCollection<IQuery> Queries;
//         public void Poll()
//         {
//             Queries = new System.Collections.Concurrent.BlockingCollection<IQuery>();
//             for (int i = 0; i < 16; ++i)
//             {
//                 new Thread(() =>
//                 {
//                     while (true)
//                     {
//                         try
//                         {
//                             if (Queries.TryTake(out var action, 10000) == true)
//                             {
//                                 action.Execute();
//                             }
//                         }
//                         catch (Exception e)
//                         {
//                             Logger.Error(e);
//                         }
//                     }

//                 }).Start();
//             }

//         }
//         public sealed class Queryable : IQueryable
//         {
//             internal System.Collections.Concurrent.BlockingCollection<IQuery> Queries;
//             public int ExecuteNonQuery()
//             {

//                 var sw = System.Diagnostics.Stopwatch.StartNew();
//                 var ret = Command.ExecuteNonQuery();
//                 long ms = sw.ElapsedMilliseconds;
//                 if (ms > global::Caspar.Extensions.Database.SlowQueryMilliseconds)
//                 {
//                     Logger.Info($"{Command.CommandText} - {ms}ms");
//                 }
//                 return ret;
//             }
//             public System.Data.Common.DbDataReader ExecuteReader()
//             {

//                 var sw = System.Diagnostics.Stopwatch.StartNew();
//                 var ret = Command.ExecuteReader();
//                 long ms = sw.ElapsedMilliseconds;
//                 if (ms > global::Caspar.Extensions.Database.SlowQueryMilliseconds)
//                 {
//                     Logger.Info($"{Command.CommandText} - {ms}ms");
//                 }
//                 return ret;

//             }
//             public object ExecuteScalar()
//             {
//                 var sw = System.Diagnostics.Stopwatch.StartNew();
//                 var ret = Command.ExecuteScalar();
//                 long ms = sw.ElapsedMilliseconds;
//                 if (ms > global::Caspar.Extensions.Database.SlowQueryMilliseconds)
//                 {
//                     Logger.Info($"{Command.CommandText} - {ms}ms");
//                 }
//                 return ret;
//             }



//             public async Task<int> ExecuteNonQueryAsync()
//             {
//                 return await Command.ExecuteNonQueryAsync();
//                 // var query = new ExecuteNonQuery();
//                 // query.command = Command;
//                 // Queries.Add(query);
//                 // return await query.TCS.Task;

//                 // return await Command.ExecuteNonQueryAsync();
//             }
//             public async Task<System.Data.Common.DbDataReader> ExecuteReaderAsync()
//             {
//                 return await Task.Run(() =>
//                 {
//                     try
//                     {
//                         var sw = System.Diagnostics.Stopwatch.StartNew();
//                         var ret = Command.ExecuteReader();
//                         long ms = sw.ElapsedMilliseconds;
//                         if (ms > global::Caspar.Extensions.Database.SlowQueryMilliseconds)
//                         {
//                             Logger.Info($"{Command.CommandText} - {ms}ms");
//                         }
//                         return ret;
//                     }
//                     catch
//                     {
//                         throw;
//                     }
//                 });
//             }
//             public async Task<object> ExecuteScalarAsync()
//             {
//                 return await Task.Run(() =>
//                 {
//                     try
//                     {
//                         var sw = System.Diagnostics.Stopwatch.StartNew();
//                         var ret = Command.ExecuteScalar();
//                         long ms = sw.ElapsedMilliseconds;
//                         if (ms > global::Caspar.Extensions.Database.SlowQueryMilliseconds)
//                         {
//                             Logger.Info($"{Command.CommandText} - {ms}ms");
//                         }
//                         return ret;
//                     }
//                     catch
//                     {
//                         throw;
//                     }

//                 });
//             }
//             public MySqlConnector.MySqlCommand Command { get; internal set; }
//             //     public System.Data.Odbc.OdbcParameterCollection Parameters => Command.Parameters;

//             public MySqlParameterCollection Parameters => Command.Parameters;

//             public void Prepare() { Command.Prepare(); }
//             public string CommandText { get { return Command.CommandText; } set { Command.CommandText = value; } }
//             public System.Data.CommandType CommandType { get { return Command.CommandType; } set { Command.CommandType = value; } }
//         }

//         //  public class Session : Driver.Session {
//         public string Name { get; set; }
//         public string Id { get; set; }
//         public string Pw { get; set; }
//         public string Ip { get; set; }
//         public string Port { get; set; }
//         public string Db { get; set; }
//         public MySqlConnector.MySqlConnection Connection { get; set; }
//         public MySqlConnector.MySqlTransaction Transaction { get; set; }
//         public MySqlConnector.MySqlCommand Command { get; set; }
//         private string connectionStringValue;
//         internal int MaxSession { get; set; } = 0;

//         //public static async Task<MySql> Session(string db)
//         //{
//         //    return await GetSession("Game", true, false);
//         //}

//         //public int IsPoolable() { return MaxSession; }
//         public int IsPoolable() { return 0; }
//         public bool Ping()
//         {
//             // if (Connection == null)
//             // {
//             //     return false;
//             // }
//             // else
//             // {
//             //     return Connection.Ping();
//             // }
//             return true;
//         }
//         public IConnection Create()
//         {
//             var session = new Odbc();
//             session.Queries = Queries;
//             session.connectionStringValue = connectionStringValue;
//             session.IAM = IAM;
//             session.Id = Id;
//             session.Pw = Pw;
//             session.Ip = Ip;
//             session.Port = Port;
//             session.Db = Db;
//             session.Name = Name;
//             session.MaxSession = IsPoolable();
//             return session;
//         }


//         public IQueryable CreateCommand()
//         {
//             if (Command == null)
//             {
//                 Command = Connection.CreateCommand();
//                 Command.Transaction = Transaction;
//             }
//             Command.CommandType = CommandType.Text;
//             Command.CommandText = "";
//             Command.Parameters.Clear();
//             return new Queryable() { Command = Command, Queries = Queries };
//         }


//         public void BeginTransaction()
//         {
//             if (Transaction == null)
//             {
//                 Transaction = Connection.BeginTransaction();

//             }

//             if (Command != null)
//             {
//                 Command.Transaction = Transaction;
//             }
//         }

//         public void Commit()
//         {
//             Transaction?.Commit();
//             Transaction = null;
//             if (Command != null)
//             {
//                 Command.Transaction = null;
//             }
//         }

//         public void Rollback()
//         {
//             Transaction?.Rollback();
//             Transaction = null;
//             if (Command != null)
//             {
//                 Command.Transaction = null;
//             }
//         }

//         public bool IAM { get; set; } = false;
//         internal DateTime InitializedAt { get; set; } = DateTime.UtcNow;

//         public void Initialize()
//         {

//             if (Database.Driver.Databases.TryGetValue(Name, out var session) == false)
//             {
//                 return;
//             }
//             if (session != null && session is Odbc)
//             {
//                 connectionStringValue = (session as Odbc).connectionStringValue;
//                 if ((session as Odbc).InitializedAt > DateTime.UtcNow) { return; }
//                 lock (session)
//                 {

//                     var connectionString = new System.Data.Odbc.OdbcConnectionStringBuilder();
//                     // //    connectionString.ConnectionString
//                     // //SqlConnectionStringBuilder connectionString = new SqlConnectionStringBuilder();
//                     // connectionString.UserID = Id;
//                     // connectionString.Password = Pw;

//                     // try
//                     // {
//                     //     if (IAM == true)
//                     //     {
//                     //         var awsCredentials = new Amazon.Runtime.BasicAWSCredentials((string)global::Caspar.Api.Config.AWS.Access.KeyId, (string)global::Caspar.Api.Config.AWS.Access.SecretAccessKey);
//                     //         var pwd = Amazon.RDS.Util.RDSAuthTokenGenerator.GenerateAuthToken(awsCredentials, Ip, 3306, Id);
//                     //         connectionString.SslMode = MySqlSslMode.Required;
//                     //         connectionString.Password = pwd;
//                     //     }
//                     // }
//                     // catch (Exception e)
//                     // {
//                     //     Logger.Error(e);
//                     // }

//                     // connectionString.Server = Ip;
//                     // connectionString.Port = Convert.ToUInt32(Port);
//                     // connectionString.Database = Db;

//                     // connectionString.Pooling = false;
//                     // //   if (IAM == false && IsPoolable() > 0 && Caspar.Api.ServerType != "Agent")
//                     // // {
//                     // //     connectionString.Pooling = true;
//                     // //     connectionString.MinimumPoolSize = 8;
//                     // //     connectionString.MaximumPoolSize = 16;
//                     // // }
//                     // connectionString.AllowZeroDateTime = true;
//                     // connectionString.CharacterSet = "utf8";
//                     // connectionString.CheckParameters = false;

//                     (session as Odbc).connectionStringValue = "user id=admin;password=zmffpdltlxl123;server=qa-klaycity-cluster.cluster-cdxduq5q4jxp.ap-northeast-2.rds.amazonaws.com;port=3306;database=orbcity;pooling=False;allowzerodatetime=True;characterset=utf8";
//                     (session as Odbc).InitializedAt = DateTime.UtcNow.AddMinutes(10);
//                 }
//                 connectionStringValue = (session as Odbc).connectionStringValue;
//             }

//         }


//         public async Task<IConnection> Open(CancellationToken token = default, bool transaction = true)
//         {
//             try
//             {
//                 if (Connection == null)
//                 {
//                     Initialize();

//                     Connection = new MySqlConnection(connectionStringValue);
//                     await Connection.OpenAsync();
//                 }
//                 if (transaction == true)
//                 {
//                     BeginTransaction();
//                 }
//             }
//             catch (Exception e)
//             {
//                 Logger.Error(e);
//                 Logger.Error(connectionStringValue);
//                 Connection?.Close();
//                 Connection?.Dispose();
//                 Connection = null;
//                 Dispose();
//                 throw;
//             }
//             return this;
//         }

//         public void Close()
//         {
//             try
//             {
//                 Rollback();
//             }
//             catch (Exception ex)
//             {
//                 global::Caspar.Api.Logger.Info("Driver Level Rollback Exception " + ex);
//             }

//         }
//         private int disposed = 0;
//         public void Dispose()
//         {
//             if (Interlocked.CompareExchange(ref disposed, 1, 0) != 0)
//             {
//                 return;
//             }
//             Close();

//             Command?.Dispose();
//             Command = null;

//             Transaction?.Dispose();
//             Transaction = null;

//             Connection?.Close();
//             Connection?.Dispose();
//             Connection = null;
//         }


//         public void CopyFrom(IConnection value)
//         {

//             var rhs = value as Odbc;
//             if (rhs == null) { return; }

//             Name = rhs.Name;
//             Id = rhs.Id;
//             Pw = rhs.Pw;
//             Ip = rhs.Ip;
//             Port = rhs.Port;
//             Db = rhs.Db;

//         }

//     }
// }

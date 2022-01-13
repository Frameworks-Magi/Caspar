using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Framework.Caspar.Api;

namespace Framework.Caspar.Extensions
{
    public static class Database
    {
        public static int QueryTimeoutSec { get; internal set; } = 10000;
        public static int SlowQueryMilliseconds { get; internal set; } = 3000;


        // `key` IN (list)
        public static string ToWhereIn<T>(this string key, List<T> list)
        {
            if (list.Count == 0) { return string.Empty; }
            if (key.IsNullOrEmpty()) { return string.Empty; }

            StringBuilder subQuery = new StringBuilder($"`{key}` IN (", 256);

            int index = 0;
            while (true)
            {
                subQuery.Append($"'{list[index++]}'");
                if (list.Count == index)
                {
                    subQuery.Append(string.Intern(")"));
                    break;
                }
                subQuery.Append(string.Intern(","));
            }
            return subQuery.ToString();
        }

        // `key` IN (list)
        public static string ToWhereIn<T>(this string key, string prefix, List<T> list)
        {
            if (list.Count == 0) { return string.Empty; }
            if (prefix.IsNullOrEmpty())
            {
                return key.ToWhereIn(list);
            }
            if (key.IsNullOrEmpty()) { return string.Empty; }

            StringBuilder subQuery = new StringBuilder($"`{prefix}`.`{key}` IN (", 256);

            int index = 0;
            while (true)
            {
                subQuery.Append($"'{list[index++]}'");
                if (list.Count == index)
                {
                    subQuery.Append(string.Intern(")"));
                    break;
                }
                subQuery.Append(string.Intern(","));
            }
            return subQuery.ToString();
        }

        public static async Task<int> ExecuteNonQueryAsyncWithProfile(this MySql.Data.MySqlClient.MySqlCommand command, string message = null)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            int ret = await command.ExecuteNonQueryAsync();

            long ms = sw.ElapsedMilliseconds;
            if (ms > SlowQueryMilliseconds)
            {
                message ??= command.CommandText;
                Logger.Info($"{message} - {ms}ms");
            }
            return ret;
        }

        public static async Task<System.Data.Common.DbDataReader> ExecuteReaderAsyncWithProfile(this MySql.Data.MySqlClient.MySqlCommand command, string message = null)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var ret = await command.ExecuteReaderAsync();
            long ms = sw.ElapsedMilliseconds;
            if (ms > SlowQueryMilliseconds)
            {
                message ??= command.CommandText;
                Logger.Info($"{message} - {ms}ms");
            }
            return ret;
        }

        public static int ExecuteNonQueryWithProfile(this MySql.Data.MySqlClient.MySqlCommand command, string message = null)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            int ret = command.ExecuteNonQuery();
            long ms = sw.ElapsedMilliseconds;
            if (ms > SlowQueryMilliseconds)
            {
                message ??= command.CommandText;
                Logger.Info($"{message} - {ms}ms");
            }
            return ret;
        }

        public static System.Data.Common.DbDataReader ExecuteReaderWithProfile(this MySql.Data.MySqlClient.MySqlCommand command, string message = null)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var ret = command.ExecuteReader();
            long ms = sw.ElapsedMilliseconds;
            if (ms > SlowQueryMilliseconds)
            {
                message ??= command.CommandText;
                Logger.Info($"{message} - {ms}ms");
            }
            return ret;
        }

        public static object ExecuteScalarWithProfile(this MySql.Data.MySqlClient.MySqlCommand command, string message = null)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var ret = command.ExecuteScalar();
            long ms = sw.ElapsedMilliseconds;
            if (ms > SlowQueryMilliseconds)
            {
                message ??= command.CommandText;
                Logger.Info($"{message} - {ms}ms");
            }
            return ret;
        }

        public static async Task<object> ExecuteScalarAsyncWithProfile(this MySql.Data.MySqlClient.MySqlCommand command, string message = null)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var ret = await command.ExecuteScalarAsync();
            long ms = sw.ElapsedMilliseconds;
            if (ms > SlowQueryMilliseconds)
            {
                message ??= command.CommandText;
                Logger.Info($"{message} - {ms}ms");
            }
            return ret;
        }

        public static Caspar.Database.ResultSet ToResultSet(this MySql.Data.MySqlClient.MySqlDataReader reader, bool leaveOpen = false)
        {
            Caspar.Database.ResultSet resultRet = new();
            do
            {
                Caspar.Database.ResultSet.Cursor cursor = null;
                if (reader.FieldCount > 0)
                {
                    cursor = resultRet.AddCursor();
                }
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var row = cursor.AddRow();
                        for (int i = 0; i < reader.FieldCount; ++i)
                        {
                            row.AddColumn(reader.GetValue(i), reader.GetName(i));
                        }
                    }
                }
            }
            while (reader.NextResult());

            if (leaveOpen == false)
            {
                reader.Close();
                reader.Dispose();
            }

            return resultRet;

        }
        public static Caspar.Database.ResultSet ToResultSet(this System.Data.SqlClient.SqlDataReader reader, bool leaveOpen = false)
        {
            Caspar.Database.ResultSet resultRet = new();
            do
            {
                Caspar.Database.ResultSet.Cursor cursor = null;
                if (reader.FieldCount > 0)
                {
                    cursor = resultRet.AddCursor();
                }
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var row = cursor.AddRow();
                        for (int i = 0; i < reader.FieldCount; ++i)
                        {
                            row.AddColumn(reader.GetValue(i), reader.GetName(i));
                        }
                    }
                }
            }
            while (reader.NextResult());

            if (leaveOpen == false)
            {
                reader.Close();
                reader.Dispose();
            }

            return resultRet;

        }
        public static Caspar.Database.ResultSet ToResultSet(this System.Data.Common.DbDataReader reader, bool leaveOpen = false)
        {
            Caspar.Database.ResultSet resultRet = new();
            do
            {
                Caspar.Database.ResultSet.Cursor cursor = null;
                if (reader.FieldCount > 0)
                {
                    cursor = resultRet.AddCursor();
                }
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var row = cursor.AddRow();
                        for (int i = 0; i < reader.FieldCount; ++i)
                        {
                            row.AddColumn(reader.GetValue(i), reader.GetName(i));
                        }
                    }
                }
            }
            while (reader.NextResult());

            if (leaveOpen == false)
            {
                reader.Close();
                reader.Dispose();
            }

            return resultRet;

        }
    }
}

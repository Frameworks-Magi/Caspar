﻿using Microsoft.CSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Net.Sockets;
using Amazon;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Caspar.Container;
using Amazon.S3;
using System.Net.Http;
using Org.BouncyCastle.Asn1.Cms;

namespace Caspar
{
    static public partial class Api
    {
        public static class DynamoDBProperties
        {
            public class UTC : IPropertyConverter
            {
                public DynamoDBEntry ToEntry(object value) => (DateTime)value;

                public object FromEntry(DynamoDBEntry entry)
                {
                    var dateTime = entry.AsDateTime();
                    return dateTime.ToUniversalTime();
                }
            }
        }
        public static int MaxSession = (int)Math.Ceiling((global::Caspar.Api.ThreadCount * 1.5));
        // public static async Task QueryAsync(Func<global::Caspar.Database.Session, Task> func)
        // {
        //     var session = new global::Caspar.Database.Session();
        //     session.Command = async () => { await func(session); };
        //     session.TCS = new();

        //     try
        //     {
        //         Database.Driver.sessions.TryGetValue(session.UID, out var queue);
        //         if (queue == null)
        //         {
        //             queue = new();
        //             Database.Driver.sessions.TryAdd(session.UID, queue);
        //         }
        //         {
        //             queue.Enqueue(session);
        //         }
        //     }
        //     catch
        //     {
        //         throw;
        //     }

        //     await session.TCS.Task;
        // }

        public static void GenerateRSAKey()
        {
            using (var rsa = RSA.Create(2048))
            {
                // PKCS#1 형식으로 개인키 내보내기
                var privateKeyBytes = rsa.ExportRSAPrivateKey();
                var privateKeyPem = new StringBuilder();
                privateKeyPem.AppendLine("-----BEGIN RSA PRIVATE KEY-----");
                privateKeyPem.AppendLine(Convert.ToBase64String(privateKeyBytes, Base64FormattingOptions.InsertLineBreaks));
                privateKeyPem.AppendLine("-----END RSA PRIVATE KEY-----");

                // 공개키 내보내기
                var publicKeyBytes = rsa.ExportRSAPublicKey();
                var publicKeyPem = new StringBuilder();
                publicKeyPem.AppendLine("-----BEGIN RSA PUBLIC KEY-----");
                publicKeyPem.AppendLine(Convert.ToBase64String(publicKeyBytes, Base64FormattingOptions.InsertLineBreaks));
                publicKeyPem.AppendLine("-----END RSA PUBLIC KEY-----");

                File.WriteAllText("private_key.pem", privateKeyPem.ToString());
                File.WriteAllText("public_key.pem", publicKeyPem.ToString());
            }

            // openssl rsa -pubout -in private_key.pem -out public_key.pem
            // openssl rsa -in private_key.pem -pubout -out public_key.pem
            // openssl pkcs8 -topk8 -inform PEM -outform PEM -in public_key.pem -out public_key_pkcs8.pem -nocrypt
        }

        internal static void StartUpDatabase()
        {

            if (global::Caspar.Api.Config.Databases.MaxSession == null)
            {
                global::Caspar.Api.Config.Databases.MaxSession = global::Caspar.Api.ThreadCount;
            }

            if (global::Caspar.Api.Config.Databases.MaxSession > global::Caspar.Api.ThreadCount * 2)
            {
                global::Caspar.Api.Config.Databases.MaxSession = global::Caspar.Api.ThreadCount * 2;
            }

            if (global::Caspar.Api.OnLambda == true)
            {
                global::Caspar.Api.Config.Databases.MaxSession = 1;
            }

            Api.MaxSession = global::Caspar.Api.Config.Databases.MaxSession;


            dynamic config = global::Caspar.Api.Config;

            Logger.Info($"Database Session Max = {Caspar.Api.Config.Databases.MaxSession}");

            try
            {
                Newtonsoft.Json.Linq.JObject mysqls = config.Databases.MySql;
                foreach (dynamic e in mysqls.Children())
                {
                    dynamic json = config.Databases.MySql[e.Name];
                    if (json.Disable != null && json.Disable == true)
                    {

                    }
                    else
                    {
                        var driver = new global::Caspar.Database.Management.Relational.MySql();
                        driver.Ip = json.Ip;
                        driver.Port = json.Port;
                        driver.Id = json.Id;
                        driver.Pw = json.Pw;
                        driver.Db = json.Db;
                        driver.Name = e.Name;
                        driver.MaxSession = Api.MaxSession;
                        try
                        {
                            driver.MaxSession = json.MaxSession;
                        }
                        catch (Exception ex)
                        {
                        }
                        finally
                        {
                            Logger.Info($"Database {e.Name} Session Max = {driver.MaxSession}");
                        }

                        if (driver.MaxSession > Api.MaxSession)
                        {
                            driver.MaxSession = Api.MaxSession;
                        }

                        if (json.Crypt == true)
                        {
                            driver.Id = global::Caspar.Api.DesDecrypt(driver.Id, "magimagi");
                            driver.Pw = global::Caspar.Api.DesDecrypt(driver.Pw, "magimagi");
                        }

                        try
                        {
                            driver.IAM = (bool)json.IAM;
                        }
                        catch
                        {

                        }
                        Logger.Info($"Database Session Add {driver.Ip}");
                        Database.Driver.AddDatabase(driver.Name, driver);
                    }
                }
            }
            catch
            {

            }

            try
            {

                var session = config.Databases.MsSql;
                if (session.Disable != null && session.Disable == true)
                {

                }
                else
                {
                    var driver = new global::Caspar.Database.Management.Relational.MsSql();
                    driver.Ip = session.Ip;
                    driver.Port = session.Port;
                    driver.Id = session.Id;
                    driver.Pw = session.Pw;
                    driver.Db = session.Db;
                    driver.Name = session.Name;


                    if (session.Crypt == true)
                    {
                        driver.Id = DesDecrypt(driver.Id, "magimagi");
                        driver.Pw = DesDecrypt(driver.Pw, "magimagi");
                    }

                    Database.Driver.AddDatabase(driver.Name, driver);
                }
            }
            catch
            {

            }

            try
            {
                var session = config.Databases.Redis;
                if (session != null)
                {
                    if (session.Disable != null && session.Disable == true)
                    {

                    }
                    else
                    {
                        var driver = new global::Caspar.Database.NoSql.Redis();

                        driver.Id = session.Id;
                        driver.Pw = session.Pw;
                        driver.Db = session.Db;
                        driver.Name = session.Name;

                        if (session.Crypt == true)
                        {
                            driver.Id = DesDecrypt(driver.Id, "magimagi");
                            driver.Pw = DesDecrypt(driver.Pw, "magimagi");
                        }

                        driver.SetMaster((string)session.Master.Ip, (string)session.Master.Port);
                        try
                        {
                            foreach (var e in config.Databases.Redis.Slaves)
                            {
                                //driver.Slaves
                            }
                        }
                        catch
                        {

                        }


                        Database.Driver.AddDatabase(driver.Name, driver);
                    }
                }
                else
                {
                    Logger.Warning("Redis Session is not configured");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            try
            {
                var session = config.Databases.DynamoDB;
                if (session.Disable != null && session.Disable == true)
                {

                }
                else
                {
                    var driver = new global::Caspar.Database.NoSql.DynamoDB();

                    driver.Name = session.Name;
                    driver.AwsAccessKeyId = config.AWS.Access.KeyId;
                    driver.AwsSecretAccessKey = config.AWS.Access.SecretAccessKey;


                    driver.Endpoint = (RegionEndpoint)typeof(RegionEndpoint).GetField((string)session.RegionEndpoint).GetValue(null); ;


                    Database.Driver.AddDatabase("DynamoDB", driver);
                }

            }
            catch
            {

            }

            //if (root["Cosmos"] != null)
            //{
            //    var cosmos = root["Cosmos"];

            //    var driver = new Caspar.Database.Management.Azure.Cosmos();

            //    driver.EndPoint = cosmos.Attributes["EndPoint"].Value;
            //    driver.Name = cosmos.Attributes["Name"].Value;
            //    Caspar.Database.Management.Driver.AddSession(driver.Name, driver);

            //}


            Database.Driver.Singleton.Run();
            Caspar.Api.Add(Singleton<RedisLayer>.Instance);

        }

        internal class Seed
        {
            private static int _value { get; set; }
            internal static int Value
            {
                get { return _value; }
                set
                {
                    _value = value;
                    Sequence = ((long)_value) << 32;
                }
            }
            internal static long Sequence = 0;

            public static long UID
            {
                get
                {
                    return Interlocked.Increment(ref Sequence);
                }
            }
        }

        internal class Snowflake
        {
            internal static int Instance { get; set; } = 0;
            internal static int Service { get; set; } = 0;
            public static long Sequence = 0;
            public static int Guard = 0;
            internal static int Timestamp { get; set; } = 0;
            internal static Timer Timer { get; set; }

            // 분당 최대 1000000개의 UID 생성

            // 초당 최대 8096개의 UID 생성

            public static void StartUp()
            {
                Update();
                // Timer = new Timer((e) =>
                // {
                //     Update();
                // }, null, 0, 31000);
                Timer = new Timer((e) =>
                {
                    Update();
                }, null, 0, 1500);

            }

            public static long UID
            {
                get
                {
                    if (Guard > 8000)
                    {
                        throw new Exception("UID OverFlow");
                    }
                    var uid = Interlocked.Increment(ref Sequence);
                    Interlocked.Increment(ref Guard);
                    return uid;
                }
            }

            internal static void Update()
            {
                var now = DateTime.UtcNow;
                var epoch = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var diff = now - epoch;
                Timestamp = Convert.ToInt32(diff.TotalSeconds);

                // |서비스ID 8비트|워커ID 10비트|타임스탬프 32비트|시퀀스 13비트|
                // 8bit = 256
                // 10bit = 1024
                // 32bit = 4294967296
                // 13bit = 8192

                long temp = Service << 55;
                temp |= (long)Instance << 45;
                temp |= (long)Timestamp << 13;
                temp |= 1;
                Interlocked.Exchange(ref Sequence, temp);
                Interlocked.Exchange(ref Guard, 0);
            }
            // internal static void Update()
            // {
            //     var now = DateTime.UtcNow;
            //     var epoch = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            //     var diff = now - epoch;
            //     var current = Convert.ToInt32(diff.TotalMinutes);

            //     if (current == Timestamp)
            //     {
            //         return;
            //     }
            //     Timestamp = current;
            //     // |서비스ID 8비트|워커ID 10비트|타임스탬프 25비트|시퀀스 20비트|
            //     // 8bit = 256
            //     // 10bit = 1024
            //     // 25bit = 33554432
            //     // 20bit = 1048576

            //     long temp = Service << 55;
            //     temp |= (long)Instance << 45;
            //     temp |= (long)Timestamp << 20;
            //     temp |= 1;
            //     Interlocked.Exchange(ref Sequence, temp);
            //     Interlocked.Exchange(ref Guard, 0);
            // }
        }


        internal class RedisLayer : Caspar.Layer
        {

        }

        static public void CleanUpDatabase()
        {
            if (Database.Driver.Singleton != null)
            {
                Database.Driver.Singleton.Close();
            }
        }

        public static global::Google.Protobuf.JsonFormatter JsonFormatter { get; set; }
        public static bool OnLambda { get; set; } = false;

        public static partial class Protobuf
        {
            public delegate global::Google.Protobuf.IMessage Deserializer(int code, global::System.IO.MemoryStream stream);
            public static Deserializer DeserializeCallback { get; set; } = null;
            public static global::Google.Protobuf.IMessage Deserialize(int code, global::System.IO.MemoryStream stream)
            {
                if (DeserializeCallback != null)
                {
                    return DeserializeCallback(code, stream);
                }
                else
                {
                    Logger.Error($"Deserialize Callback is not set. Code : {code}");
                    return null;
                }
            }
        }

    }

    public static partial class Api
    {
        public static ushort TERMINAL_PORT { get; set; } = 5882;
        public static ushort GATEWAY_PORT { get; set; } = 6882;

        public class ProtobufParser<T> where T : global::Google.Protobuf.IMessage<T>
        {
#pragma warning disable RECS0108 // 제네릭 형식의 정적 필드에 대해 경고합니다.
            internal static readonly System.Reflection.PropertyInfo field = typeof(T).GetProperty("Parser", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
#pragma warning restore RECS0108 // 제네릭 형식의 정적 필드에 대해 경고합니다.
            public static readonly global::Google.Protobuf.MessageParser<T> Parser = (global::Google.Protobuf.MessageParser<T>)field.GetValue(null);
            public global::Google.Protobuf.MessageParser GetParser()
            {
                return Parser;
            }
        }

        //public delegate Action Bind(dynamic handler, dynamic notifier, int code, global::System.IO.Stream stream, Action missingBinder = null);

        //public static Bind Binder = (dynamic handler, dynamic notifier, int code, global::System.IO.Stream stream, Action missingBinder) => 
        //{
        //    return () => { };
        //};

        static ConcurrentDictionary<ushort, Socket> listeners = new();
        static ConcurrentDictionary<ushort, ListenCallback> listenCallbacks = new();
        public delegate void ListenCallback();

        public delegate void OnDisconnectCallback();
        //   static internal ConcurrentQueue<Caspar.Network.Protocol.Tcp.AsyncDisconnectCallback> OnDisconnectCallbacks = new ConcurrentQueue<Protocol.Tcp.AsyncDisconnectCallback>();


        static internal Socket Acceptor(ushort port)
        {

            if (global::Caspar.Api.IsOpen == false) return null;
            Socket socket;
            if (listeners.TryGetValue(port, out socket) == true)
            {
                return socket;
            }

            socket = new Socket(AddressFamily.InterNetwork,
              SocketType.Stream, ProtocolType.Tcp);

            //IPAddress hostIP = Dns.Resolve(IPAddress.Any.ToString()).AddressList[0];
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, port);

            try
            {
                socket.Bind(ep);
                socket.Listen(1024);

                listeners.Remove(port);
                listeners.Add(port, socket);
            }
            catch (Exception)
            {
                Logger.Error($"Bind Fail Port : {port}");
            }
            return socket;
        }

        static public void Listen(ushort port)
        {

            if (global::Caspar.Api.IsOpen == false) return;
            ListenCallback callback = null;
            if (listenCallbacks.TryGetValue(port, out callback) == true)
            {
                callback();
            }

        }

        internal static bool IsOpen
        {
            get { return isOpen; }
        }


        public static long Idx { get; set; }
        public static string ServerId { get; set; }

        static public void StartUpNetwork()
        {

        }

        static public bool Listen(ushort port, ListenCallback heartbeat)
        {

            return Listen(port, 128, heartbeat);

        }

        static public bool Listen(ushort port, ushort backlog, ListenCallback callback)
        {

            if (global::Caspar.Api.IsOpen == false) return false;

            if (listenCallbacks.ContainsKey(port) == true)
            {
                return false;
            }
            listenCallbacks.Add(port, callback);

            for (int i = 0; i < backlog; ++i)
            {
                callback();
            }
            return true;

        }

        public static void CloseListen(ushort port)
        {
            listenCallbacks.Remove(port);

            var socket = listeners.Remove(port);

            if (socket != null)
            {
                try
                {
                    socket.Shutdown(SocketShutdown.Both);
                }
                catch
                {
                }

                try
                {
                    socket.Close();
                }
                catch
                {

                }
            }
        }

        public static partial class Terminal
        {

            public static void Notify(global::Caspar.INotifier notifier, string message, bool newLine = true)
            {
                if (notifier == null) { return; }
                Protocol.Terminal.Message msg = new Protocol.Terminal.Message();
                msg.Type = Protocol.Terminal.Message.EType.Notify;

                msg.NewLine = newLine;
                msg.Command = message;

                notifier.Notify(msg);
            }

            public static void Complete(global::Caspar.INotifier notifier, string message)
            {
                if (notifier == null) { return; }
                Protocol.Terminal.Message msg = new Protocol.Terminal.Message();
                msg.Type = Protocol.Terminal.Message.EType.Complete;
                msg.NewLine = true;
                msg.Command = message;
                notifier.Notify(msg);
            }

            public static void Error(global::Caspar.INotifier notifier, string message, bool newLine = true)
            {
                if (notifier == null) { return; }
                Protocol.Terminal.Message msg = new Protocol.Terminal.Message();
                msg.Type = Protocol.Terminal.Message.EType.Error;

                msg.NewLine = newLine;
                msg.Command = message;

                notifier.Notify(msg);
            }

            public static void Command(global::Caspar.INotifier notifier, string message)
            {
                if (notifier == null) { return; }
                Protocol.Terminal.Message msg = new Protocol.Terminal.Message();
                msg.Type = Protocol.Terminal.Message.EType.Command;
                msg.Command = message;
                notifier.Notify(msg);
            }

            public static void Command(Protocol.Terminal to, string message)
            {
                if (to == null) { return; }
                Protocol.Terminal.Message msg = new Protocol.Terminal.Message();
                msg.Type = Protocol.Terminal.Message.EType.Command;
                msg.Command = message;
                to.Notify(msg);
            }



            public static string CurrentTerminal = "127.0.0.1@~ magi$ ";
            public static DateTime IsCommandable { get; set; } = DateTime.MinValue;

            public static bool Exit = false;


            public static async Task Run(List<Protocol.Terminal.Callback> callbacks)
            {
                async Task<bool> ProcessCommand(global::Caspar.INotifier notifier, Protocol.Terminal.Message msg)
                {
                    try
                    {
                        foreach (var e in callbacks)
                        {
                            var ret = await e.Invoke(notifier, msg);
                            if (ret == true) { return true; }
                        }

                        Error(notifier, $"Unknown command '{msg.Command}'");

                    }
                    catch (Exception e)
                    {
                        Notify(notifier, "catch Exception");
                        Error(notifier, e.Message);
                        return false;
                    }
                    return false;
                }

                await Task.Run(async () =>
                {

                    while (Exit == false)
                    {
                        if (global::Caspar.Api.Config.Service == true) { System.Threading.Thread.Sleep(1000); continue; }
                        if (Api.Terminal.IsCommandable > DateTime.UtcNow)
                        {
                            System.Threading.Thread.Sleep(1);
                            continue;
                        }

                        if (global::Caspar.Api.Logger.Silence == false)
                        {
                            var tokens = Api.Terminal.CurrentTerminal.Split('@');
                            if (tokens != null && tokens.Length > 1)
                            {
                                Console.Write(tokens[0]);
                                Console.Write("@");

                                tokens = tokens[1].Split(' ');

                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.Write(tokens[0]);
                                Console.Write(" ");
                                Console.ResetColor();
                                Console.Write(tokens[1]);
                                Console.Write(" ");
                            }
                            else
                            {
                                Console.Write(Api.Terminal.CurrentTerminal);
                            }
                        }

                        var cmd = Console.ReadLine();

                        if (string.IsNullOrEmpty(cmd))
                        {
                            continue;
                        }

                        if (cmd.ToLower() == "quit")
                        {
                            break;
                        }

                        try
                        {
                            var msg = new Protocol.Terminal.Message();
                            msg.Type = Protocol.Terminal.Message.EType.Command;
                            msg.Command = cmd;
                            await ProcessCommand(Singleton<Protocol.Terminal.ConsoleNotifier>.Instance, msg);
                        }
                        catch
                        {

                        }
                    }

                });
            }


        }



        static public global::Google.Protobuf.ByteString ToByteStringWithCode<T>(this T msg) where T : global::Google.Protobuf.IMessage<T>
        {
            return ToByteString(global::Caspar.Id<T>.Value, msg);
        }

        static public global::Google.Protobuf.ByteString ToByteString<T>(int code, T msg) where T : global::Google.Protobuf.IMessage
        {
            var stream = new MemoryStream(4096);
            using (BinaryWriter bw = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                bw.Write(code);
                using (var co = new global::Google.Protobuf.CodedOutputStream(stream, true))
                {
                    msg.WriteTo(co);
                }

                bw.Seek(0, SeekOrigin.Begin);
                return global::Google.Protobuf.ByteString.FromStream(stream);
            }

        }

    }

    static public partial class Api
    {
        public abstract class Singleton<T> where T : new()
        {
            private static readonly Lazy<T> _instance = new Lazy<T>(() => new T());

            [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
            public static T Instance
            {
                get
                {
                    return _instance.Value;
                }
            }
        }

        public interface ISingleton<T> where T : new()
        {
            private static Lazy<T> instance { get; set; } = new Lazy<T>(() => new T());
            [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
            public static T Instance
            {
                get
                {
                    return instance.Value;
                }
            }
        }

        public static long UniqueKey
        {
            get
            {
                //return Api.Snowflake.UID;
                return Api.Seed.UID;
            }
        }


        public static string PublicIp { get; private set; }
        public static string PrivateIp { get; private set; }

        public static string ServiceIp
        {
            get { return StandAlone == true ? PrivateIp : PublicIp; }
        }

        static internal int ThreadCount { get; set; } = Math.Min(16, Math.Max(2, Convert.ToInt32(Math.Ceiling(Environment.ProcessorCount * 0.75) * 2.0)));

        internal class Notifier : global::Caspar.INotifier
        {
            internal static Notifier Instance = new Notifier();
            public void Response<T>(T msg)
            {

            }
            public void Notify<T>(T msg)
            {
            }
            public void Serialize(Stream output) { }
        }


        public static void Add(global::Caspar.Layer layer)
        {
            lock (waitLayers)
            {
                waitLayers.Remove(layer);
                waitLayers.Add(layer);
            }

        }
        private static List<global::Caspar.Layer> layers = new List<Caspar.Layer>();
        private static List<global::Caspar.Layer> waitLayers = new List<Caspar.Layer>();
        // public class Watcher
        // {
        //     private FileSystemWatcher watcher;
        //     protected Queue<string> changed = new Queue<string>();
        //     protected Queue<string> deleted = new Queue<string>();
        //     protected Queue<string> errors = new Queue<string>();
        //     protected bool forceUpdate = false;
        //     public string Path { get; set; }
        //     public string Filter { get; set; }
        //     protected Dictionary<string, DateTime> deletedLastWriteTime = new Dictionary<string, DateTime>();
        //     protected Dictionary<string, DateTime> changedLastWriteTime = new Dictionary<string, DateTime>();


        //     protected void AddCreateOrChange(string path)
        //     {
        //         lock (this)
        //         {
        //             if (changedLastWriteTime.ContainsKey(path) == true)
        //             {
        //                 changedLastWriteTime[path] = DateTime.UtcNow;
        //             }
        //             else
        //             {
        //                 changedLastWriteTime.Add(path, DateTime.UtcNow);
        //             }
        //         }
        //     }

        //     protected void AddDeleteOrRename(string path)
        //     {
        //     }
        //     protected virtual void OnCallback(string path, bool ret) { }
        //     protected virtual void OnDeleted(object sender, FileSystemEventArgs e)
        //     {

        //         lock (this)
        //         {
        //             FileInfo fi = new FileInfo(e.FullPath);
        //             if (fi == null) { return; }

        //             Logger.Error("Deleted : " + e.FullPath + " - " + DateTime.UtcNow);

        //             lock (deletedLastWriteTime)
        //             {
        //                 if (deletedLastWriteTime.ContainsKey(e.FullPath) == true)
        //                 {
        //                     deletedLastWriteTime[e.FullPath] = DateTime.UtcNow;
        //                 }
        //                 else
        //                 {
        //                     deletedLastWriteTime.Add(e.FullPath, DateTime.UtcNow);
        //                 }
        //             }
        //         }

        //     }
        //     protected virtual void OnCreated(object sender, FileSystemEventArgs e)
        //     {
        //         FileInfo fi = new FileInfo(e.FullPath);
        //         if (fi == null) { return; }
        //         AddCreateOrChange(e.FullPath);
        //     }
        //     protected virtual void OnRenamed(object sender, RenamedEventArgs e)
        //     {
        //         lock (this)
        //         {
        //             FileInfo fi = new FileInfo(e.FullPath);
        //             if (fi == null) { return; }

        //             Logger.Error("Renamed : " + e.FullPath + " - " + DateTime.UtcNow);

        //         }
        //     }
        //     protected virtual void OnChanged(object sender, FileSystemEventArgs e)
        //     {
        //         FileInfo fi = new FileInfo(e.FullPath);
        //         if (fi == null) { return; }
        //         AddCreateOrChange(e.FullPath);
        //     }
        //     internal void Run()
        //     {
        //         if (watcher != null) { return; }
        //         watcher = new FileSystemWatcher();

        //         Directory.CreateDirectory(Path);
        //         watcher.Path = Path;
        //         /* Watch for changes in LastAccess and LastWrite times, and
        //            the renaming of files or directories. */
        //         watcher.NotifyFilter = NotifyFilters.LastWrite;
        //         // Only watch text files.
        //         watcher.Filter = Filter;

        //         // Add event handlers.
        //         watcher.Changed += new FileSystemEventHandler(OnChanged);
        //         watcher.Created += new FileSystemEventHandler(OnCreated);
        //         watcher.Deleted += new FileSystemEventHandler(OnDeleted);
        //         watcher.Renamed += new RenamedEventHandler(OnRenamed);

        //         // Begin watching.
        //         watcher.EnableRaisingEvents = true;
        //         watcher.IncludeSubdirectories = true;
        //     }
        //     public virtual void Update()
        //     {
        //     }

        //     internal void Refresh()
        //     {

        //         DateTime now = DateTime.UtcNow;
        //         lock (this)
        //         {

        //             foreach (var e in changedLastWriteTime)
        //             {
        //                 var diff = now.Subtract(e.Value).TotalMilliseconds;
        //                 if (diff >= 5000)
        //                 {
        //                     changed.Enqueue(e.Key);
        //                 }
        //             }

        //             foreach (var e in changed)
        //             {
        //                 changedLastWriteTime.Remove(e);
        //             }

        //             foreach (var e in deletedLastWriteTime)
        //             {
        //                 var diff = now.Subtract(e.Value).TotalMilliseconds;
        //                 if (diff >= 5000)
        //                 {
        //                     deleted.Enqueue(e.Key);
        //                 }
        //             }

        //             foreach (var e in deleted)
        //             {
        //                 deletedLastWriteTime.Remove(e);
        //             }

        //             if (changed.Count > 0 || deleted.Count > 0)
        //             {
        //                 Update();
        //             }

        //         }

        //     }


        //     public bool IsError()
        //     {
        //         return errors.Count > 0;
        //     }

        //     public bool IsClear()
        //     {
        //         if (changed.Count > 0 || deleted.Count > 0)
        //         {
        //             return false;
        //         }
        //         return true;
        //     }
        // }

        // static public void AddWatcher(Watcher watcher)
        // {

        //     lock (Api.watchers)
        //     {
        //         if (Api.watchers.ContainsKey(watcher.Path) == true) { return; }
        //         Api.watchers.Add(watcher.Path, watcher);
        //         watcher.Run();
        //     }
        // }

        public static string Nonce
        {
            get
            {
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    var bytes = System.Text.Encoding.UTF8.GetBytes($"{DateTime.UtcNow.Ticks}.{UniqueKey}");
                    return md5.ComputeHash(bytes).ToHex();
                }
            }
        }

        private static byte[]? _rsaPrivateKey = null;
        private static readonly Aes aes = Aes.Create();
        private static byte[]? aesKey; // 256 bits
        private static byte[]? aesIV; // 128 bits

        public static string AesEncrypt(string value)
        {
            aesKey ??= ((string)Caspar.Api.Config.Encrypt.AES.Key).FromBase64ToBytes();
            aesIV ??= ((string)Caspar.Api.Config.Encrypt.AES.IV).FromBase64ToBytes();
            using ICryptoTransform encryptor = aes.CreateEncryptor(aesKey, aesIV);
            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(value);
                    }
                    byte[] encrypted = msEncrypt.ToArray();
                    return encrypted.ToBase64String();
                }
            }
        }

        public static string AesDecrypt(string value)
        {
            aesKey ??= ((string)Caspar.Api.Config.Encrypt.AES.Key).FromBase64ToBytes();
            aesIV ??= ((string)Caspar.Api.Config.Encrypt.AES.IV).FromBase64ToBytes();
            using ICryptoTransform decryptor = aes.CreateDecryptor(aesKey, aesIV);
            byte[] buffer = value.FromBase64ToBytes();
            using (MemoryStream msDecrypt = new MemoryStream(buffer))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        string plaintext = srDecrypt.ReadToEnd();
                        return plaintext;
                    }
                }
            }
        }

        public static string RsaEncrypt(string value)
        {
            using (var rsa = System.Security.Cryptography.RSA.Create())
            {
                _rsaPrivateKey ??= ((string)Caspar.Api.Config.Encrypt.RSA).FromBase64ToBytes();
                rsa.ImportRSAPrivateKey(_rsaPrivateKey, out _);
                var encrypted = rsa.Encrypt(value.ToBytes(), System.Security.Cryptography.RSAEncryptionPadding.Pkcs1);
                return encrypted.ToBase64String();
            }
        }

        public static string RsaDecrypt(string value)
        {
            using (var rsa = System.Security.Cryptography.RSA.Create())
            {
                _rsaPrivateKey ??= ((string)Caspar.Api.Config.Encrypt.RSA).FromBase64ToBytes();
                rsa.ImportRSAPrivateKey(_rsaPrivateKey, out _);
                var decrypted = rsa.Decrypt(value.FromBase64ToBytes(), System.Security.Cryptography.RSAEncryptionPadding.Pkcs1);
                return Encoding.UTF8.GetString(decrypted);
            }
        }

        public static string DesEncrypt(string value, string key)
        {
            //키 유효성 검사
            byte[] btKey = ASCIIEncoding.ASCII.GetBytes(key);

            //키가 8Byte가 아니면 예외발생
            if (btKey.Length != 8)
            {
                throw (new Exception("Invalid key. Key length must be 8 byte."));
            }

            //소스 문자열
            var des = DES.Create();

            des.Key = btKey;
            des.IV = btKey;

            ICryptoTransform desencrypt = des.CreateEncryptor();

            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, desencrypt, CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(value);
                    }
                    byte[] encrypted = msEncrypt.ToArray();
                    return Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(encrypted);
                }
            }

        }
        public static string DesDecrypt(string value, string key)
        {
            //키 유효성 검사
            byte[] btKey = ASCIIEncoding.ASCII.GetBytes(key);

            //키가 8Byte가 아니면 예외발생
            if (btKey.Length != 8)
            {
                throw (new Exception("Invalid key. Key length must be 8 byte."));
            }

            var des = DES.Create();

            des.Key = btKey;
            des.IV = btKey;

            ICryptoTransform desdecrypt = des.CreateDecryptor();
            byte[] buffer = Microsoft.IdentityModel.Tokens.Base64UrlEncoder.DecodeBytes(value);
            using (MemoryStream msDecrypt = new MemoryStream(buffer))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, desdecrypt, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        string plaintext = srDecrypt.ReadToEnd();
                        return plaintext;
                    }
                }
            }

        }//end of func DesDecrypt


        //public static string Compress(MemoryStream stream)
        //{
        //    stream.Position = 0;
        //    MemoryStream compressedStream = new MemoryStream();
        //    using (GZipStream compressionStream = new GZipStream(compressedStream,
        //                       CompressionMode.Compress))
        //    {
        //        stream.CopyTo(compressionStream);
        //    }

        //    return Convert.ToBase64String(compressedStream.ToArray());
        //}

        public static MemoryStream Compress(Stream stream)
        {
            stream.Position = 0;
            MemoryStream compressedStream = new MemoryStream();
            using (GZipStream compressionStream = new GZipStream(compressedStream,
                               CompressionMode.Compress, true))
            {
                stream.CopyTo(compressionStream);
            }
            compressedStream.Seek(0, SeekOrigin.Begin);
            return compressedStream;
        }

        public static MemoryStream Decompress(Stream compressed)
        {
            MemoryStream original = new MemoryStream();
            using (GZipStream decompressionStream = new GZipStream(compressed, CompressionMode.Decompress, true))
            {
                decompressionStream.CopyTo(original);
            }
            original.Seek(0, SeekOrigin.Begin);
            return original;
        }


        public static MemoryStream Decompress(string base64)
        {
            byte[] array = Convert.FromBase64String(base64);
            MemoryStream compressed = new MemoryStream(array);

            MemoryStream original = new MemoryStream();
            using (GZipStream decompressionStream = new GZipStream(compressed, CompressionMode.Decompress))
            {
                decompressionStream.CopyTo(original);
            }
            return new MemoryStream(original.ToArray());
        }
        static Thread thread = null;
        static internal bool isOpen = false;

        static private HashSet<string> needAssemblies = new HashSet<string>();
        static private HashSet<string> ignoreAssemblies = new HashSet<string>();
        static private Dictionary<Type, Tuple<Type, Assembly>> assemblies = new Dictionary<Type, Tuple<Type, Assembly>>();
        //static internal Dictionary<string, Api.Watcher> watchers = new Dictionary<string, Api.Watcher>();


        protected delegate void OverrideCallback();
        private static OverrideCallback OnOverride = null;
        private static DateTime lastGCTime = DateTime.UtcNow;
        private readonly static TimeSpan forcGC = TimeSpan.FromMinutes(1);
        private static async Task LayerUpdate()
        {
            var sw = Stopwatch.StartNew();
            while (isOpen)
            {
                try
                {
                    using var cts = new CancellationTokenSource();
                    cts.CancelAfter(10000);

                    var layer = Layer.Layers.Take(cts.Token);
                    if (layer.ToRun() == true)
                    {
                        var wait = ((DateTime.UtcNow - layer.WaitAt).TotalMilliseconds);
                        //   Logger.Warning($"Layer Wait {wait}ms : {layer.GetType()}");
                        if (Layer.g_e != null)
                        {
                            //       Logger.Warning($"Entity Wait {(DateTime.UtcNow - Layer.g_e.PostAt).TotalMilliseconds}ms : {layer.GetType()}");
                        }
                        //       if (wait > 3)
                        {
                            //        Logger.Warning($"Layer Wait {wait}ms : {layer.GetType()}");
                        }
                        //    Logger.Debug($"Layer Run. {layer.GetType()}");
                        sw.Restart();
                        DateTime now = DateTime.UtcNow;
                        bool @continue = layer.Run();
                        //var elapsed = sw.ElapsedMilliseconds;
                        //    if (elapsed > 10)
                        {
                            //    Logger.Warning($"Layer Update {(DateTime.UtcNow - now).TotalMilliseconds}ms : {layer.GetType()}");
                            layer.MS += (DateTime.UtcNow - now).TotalMilliseconds;
                        }
                        //       sw.Stop();
                        layer.ToIdle();
                        if (@continue == true || layer.IsPost() == true)
                        {
                            if (layer.ToWait())
                            {
                                Layer.Layers.Add(layer);
                            }
                        }
                    }
                    else
                    {
                        Logger.Warning($"Layer can't to run. {layer.GetType()}");
                        layer.ToIdle();
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
                finally
                {
                    try
                    {
                        await Logger.Flush();
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                    }

                    try
                    {
                        if (DateTime.UtcNow - lastGCTime > forcGC)
                        {
                            lastGCTime = DateTime.UtcNow;
                            await PerformGarbageCollection();
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                    }
                }
            }
            return;
        }

        private static async Task PerformGarbageCollection()
        {
            // Generation 0, 1, 2 순차적으로 정리
            GC.Collect(0, GCCollectionMode.Optimized);
            await Task.Delay(10);

            GC.Collect(1, GCCollectionMode.Optimized);
            await Task.Delay(10);

            GC.Collect(2, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();


        }


        public static void Override()
        {
            OnOverride = () =>
            {
                void Error(string value)
                {
                    string now = string.Format("{0:yyyy-MM-dd_hh-mm-ss}.log", DateTime.UtcNow);
                    string path = Directory.GetCurrentDirectory() + "/Override/Error/";
                    Directory.CreateDirectory(path);
                    using (var file = File.CreateText(path + now))
                    {
                        file.WriteLine(value);
                        file.Flush();
                        file.Close();
                    }
                }

                CompilerParameters parameters = new CompilerParameters();
                parameters.GenerateInMemory = true;
                parameters.GenerateExecutable = false;
                parameters.IncludeDebugInformation = true;


                parameters.ReferencedAssemblies.Add("Microsoft.CSharp.dll");


                foreach (global::System.Reflection.Assembly b in AppDomain.CurrentDomain.GetAssemblies())
                {

                    foreach (var m in b.Modules)
                    {

                        try
                        {
                            var dll = global::System.IO.Path.GetExtension(m.Name);
                            if (dll == null) { continue; }
                            if (dll.ToLower() != ".dll" && dll.ToLower() != ".exe") { continue; }

                            if (global::Caspar.Attributes.Override.IsContain(m.Name.ToLower()) == true) { continue; }

                        }
                        catch (ArgumentException)
                        {
                        }
                        catch (Exception e)
                        {

                            Logger.Error("Override Exception " + e);
                            Error(e.ToString());
                            continue;
                        }

                        if (m.Name == "Microsoft.VisualStudio.HostingProcess.Utilities.dll" ||
                            m.Name == "Microsoft.VisualStudio.HostingProcess.Utilities.Sync.dll" ||
                            m.Name == "Microsoft.VisualStudio.Debugger.Runtime.dll" ||
                            m.Name == "mscorlib.resources.dll" ||
                            m.Name == "System.EnterpriseServices.Wrapper.dll" ||
                            m.Name == "(알 수 없음)" ||
                            m.Name == "<알 수 없음>" ||
                            m.Name == "<Unknown>" ||
                            m.Name == "<In Memory Module>" ||
                            m.Name == "<메모리 모듈>")
                        {
                            continue;
                        }
                        parameters.ReferencedAssemblies.Add(m.Name);
                    }

                }


                global::Caspar.Attributes.Override.AddReference(parameters);
                string overrideAssemblePath = System.IO.Path.Combine(Directory.GetCurrentDirectory());
                var files = Directory.GetFiles(System.IO.Path.Combine(overrideAssemblePath, "Override"), "*.cs", SearchOption.AllDirectories);


                try
                {
                    CSharpCodeProvider codeProvider = new CSharpCodeProvider();

                    //parameters.OutputAssembly = string.Format($"{overrideAssemblePath}/{"test"}.dll");

                    CompilerResults results = codeProvider.CompileAssemblyFromFile(parameters, files.ToArray());
                    Assembly assembly = null;

                    codeProvider.Dispose();


                    if (!results.Errors.HasErrors)
                    {
                        assembly = results.CompiledAssembly;
                        Logger.Info("Override Success");
                    }
                    else
                    {
                        Logger.Info("Override Compile Error - ");
                        string error = "";
                        for (int i = 0; i < results.Output.Count; i++)
                        {
                            error += results.Output[i];
                            error += "\r\n";
                            Logger.Info(results.Output[i]);
                        }

                        Error(error);
                    }

                    var classes = (from type in assembly.GetTypes() where type.IsClass select type);

                    foreach (var c in classes)
                    {

                        var attribute = c.GetCustomAttribute(typeof(global::Caspar.Attributes.Override), false);
                        if (attribute == null) { continue; }



                        {
                            var method = c.GetMethod("Override");
                            if (method != null)
                            {
                                Logger.Info($"Override {c.FullName}");
                                method.Invoke(null, new object[] { });
                            }

                        }



                    }

                }
                catch (Exception e)
                {
                    Logger.Error("Override Exception " + e);
                    Error(e.ToString());
                }
            };



        }

        // public class OverrideWatcher : Api.Watcher
        // {
        //     protected DateTime configLastWriteTime = DateTime.UtcNow;
        //     protected bool configChanged = false;
        //     protected string configPath = "";

        //     protected override void OnCreated(object sender, FileSystemEventArgs e)
        //     {

        //         FileInfo fi = new FileInfo(e.FullPath);
        //         if (fi == null) { return; }

        //         if (System.IO.Path.GetFileName(e.FullPath).ToLower() == "config.xml")
        //         {
        //             lock (this)
        //             {
        //                 configPath = e.FullPath;
        //                 configChanged = true;
        //                 configLastWriteTime = DateTime.UtcNow;
        //             }
        //         }
        //         else
        //         {
        //             base.OnCreated(sender, e);
        //         }
        //     }
        //     protected override void OnChanged(object sender, FileSystemEventArgs e)
        //     {

        //         FileInfo fi = new FileInfo(e.FullPath);
        //         if (fi == null) { return; }

        //         if (System.IO.Path.GetFileName(e.FullPath).ToLower() == "config.xml")
        //         {
        //             lock (this)
        //             {
        //                 configPath = e.FullPath;
        //                 configChanged = true;
        //                 configLastWriteTime = DateTime.UtcNow;
        //             }
        //         }
        //         else
        //         {
        //             base.OnChanged(sender, e);
        //         }

        //     }

        //     static HashSet<string> buildedAssemblies = new HashSet<string>();
        //     private void Update(Queue<string> data)
        //     {

        //         lock (data)
        //         {

        //             if (data.Count == 0) { return; }

        //             CompilerParameters parameters = new CompilerParameters();
        //             parameters.GenerateInMemory = true;
        //             parameters.GenerateExecutable = false;
        //             parameters.IncludeDebugInformation = true;


        //             parameters.ReferencedAssemblies.Add("Microsoft.CSharp.dll");

        //             foreach (var e in buildedAssemblies)
        //             {
        //                 parameters.ReferencedAssemblies.Add(e);
        //             }

        //             foreach (global::System.Reflection.Assembly b in AppDomain.CurrentDomain.GetAssemblies())
        //             {

        //                 foreach (var m in b.Modules)
        //                 {

        //                     try
        //                     {
        //                         var dll = global::System.IO.Path.GetExtension(m.Name);
        //                         if (dll == null) { continue; }
        //                         if (dll.ToLower() != ".dll" && dll.ToLower() != ".exe") { continue; }

        //                         if (global::Caspar.Attributes.Override.IsContain(m.Name.ToLower()) == true) { continue; }

        //                     }
        //                     catch (ArgumentException)
        //                     {
        //                     }
        //                     catch (Exception e)
        //                     {

        //                         Logger.Error("Override Exception " + e);
        //                         //Error(e.ToString());
        //                         errors.Enqueue(e.ToString());
        //                         continue;
        //                     }

        //                     if (m.Name == "Microsoft.VisualStudio.HostingProcess.Utilities.dll" ||
        //                         m.Name == "Microsoft.VisualStudio.HostingProcess.Utilities.Sync.dll" ||
        //                         m.Name == "Microsoft.VisualStudio.Debugger.Runtime.dll" ||
        //                         m.Name == "mscorlib.resources.dll" ||
        //                         m.Name == "System.EnterpriseServices.Wrapper.dll" ||
        //                         m.Name == "(알 수 없음)" ||
        //                         m.Name == "<알 수 없음>" ||
        //                         m.Name == "<Unknown>" ||
        //                         m.Name == "<In Memory Module>" ||
        //                         m.Name == "<메모리 모듈>")
        //                     {
        //                         continue;
        //                     }
        //                     parameters.ReferencedAssemblies.Add(m.Name);
        //                 }

        //             }


        //             global::Caspar.Attributes.Override.AddReference(parameters);
        //             string overrideAssemblePath = System.IO.Path.Combine(Directory.GetCurrentDirectory());
        //             var files = Directory.GetFiles(System.IO.Path.Combine(overrideAssemblePath, "Override"), "*.cs", SearchOption.AllDirectories);


        //             try
        //             {
        //                 CSharpCodeProvider codeProvider = new CSharpCodeProvider();

        //                 parameters.OutputAssembly = string.Format($"{overrideAssemblePath}/{"test"}.dll");

        //                 CompilerResults results = codeProvider.CompileAssemblyFromFile(parameters, files.ToArray());
        //                 Assembly assembly = null;

        //                 codeProvider.Dispose();


        //                 if (!results.Errors.HasErrors)
        //                 {
        //                     assembly = results.CompiledAssembly;
        //                     Logger.Info("Success");

        //                     buildedAssemblies.Remove(parameters.OutputAssembly);
        //                     buildedAssemblies.Add(parameters.OutputAssembly);
        //                 }
        //                 else
        //                 {

        //                     Logger.Info("Override Compile Error - ");
        //                     string error = "";
        //                     for (int i = 0; i < results.Output.Count; i++)
        //                     {
        //                         error += results.Output[i];
        //                         error += "\r\n";
        //                         Logger.Info(results.Output[i]);
        //                     }

        //                     //Error(error);
        //                 }

        //                 var classes = (from type in assembly.GetTypes() where type.IsClass select type);

        //                 foreach (var c in classes)
        //                 {

        //                     var attribute = c.GetCustomAttribute(typeof(global::Caspar.Attributes.Override), false);
        //                     if (attribute == null) { continue; }



        //                     {
        //                         var method = c.GetMethod("Override");
        //                         method?.Invoke(null, new object[] { });
        //                     }



        //                 }

        //             }
        //             catch (Exception e)
        //             {
        //                 Logger.Error("Override Exception " + e);
        //                 // Error(e.ToString());
        //             }
        //             data.Clear();
        //         }

        //     }


        //     public override void Update()
        //     {

        //         lock (this)
        //         {
        //             if (configChanged == true)
        //             {

        //                 if (DateTime.UtcNow.Subtract(configLastWriteTime).TotalMilliseconds >= 5000)
        //                 {
        //                     configChanged = false;
        //                     Update(configPath);
        //                 }

        //             }
        //         }

        //         Update(changed);
        //     }

        //     private void Update(string configPath)
        //     {

        //         try
        //         {

        //             XmlDocument doc = new XmlDocument();
        //             doc.Load(configPath);

        //             global::Caspar.Attributes.Override.Clear();

        //             var root = doc.DocumentElement;
        //             foreach (XmlElement e in root["Need"].ChildNodes)
        //             {
        //                 global::Caspar.Attributes.Override.AddReference(e.Attributes["Name"].Value);
        //             }
        //             foreach (XmlElement e in root["Ignore"].ChildNodes)
        //             {
        //                 global::Caspar.Attributes.Override.RemoveReference(e.Attributes["Name"].Value);
        //             }

        //         }
        //         catch (Exception e)
        //         {
        //             Logger.Error(e);
        //         }

        //     }
        // };
        // internal class OverrideConfigWatcher : Api.Watcher
        // {
        //     private void Update(Queue<string> data)
        //     {

        //         lock (data)
        //         {

        //             if (data.Count == 0) { return; }

        //             foreach (var fullPath in data)
        //             {
        //                 if (global::System.IO.Path.GetFileName(fullPath) == String.Empty)
        //                 {
        //                     continue;
        //                 }

        //                 Update(fullPath);
        //             }
        //             data.Clear();
        //         }

        //     }
        //     public override void Update()
        //     {

        //         Update(changed);
        //     }

        //     internal static void Update(string configPath)
        //     {


        //         try
        //         {

        //             XmlDocument doc = new XmlDocument();
        //             doc.Load(configPath);

        //             global::Caspar.Attributes.Override.Clear();

        //             var root = doc.DocumentElement;
        //             foreach (XmlElement e in root["Need"].ChildNodes)
        //             {
        //                 global::Caspar.Attributes.Override.AddReference(e.Attributes["Name"].Value);
        //             }
        //             foreach (XmlElement e in root["Ignore"].ChildNodes)
        //             {
        //                 global::Caspar.Attributes.Override.RemoveReference(e.Attributes["Name"].Value);
        //             }

        //         }
        //         catch (Exception e)
        //         {
        //             Logger.Error(e);
        //         }

        //     }
        // };
        // public class MetadataWatcher : global::Caspar.Api.Watcher
        // {
        //     public delegate void ReloadCallback(string path);

        //     static private Dictionary<string, ReloadCallback> WatchFiles = new Dictionary<string, ReloadCallback>();
        //     static private Dictionary<string, System.Type> WatchFilesByType = new Dictionary<string, System.Type>();
        //     static public void AddWatchFile(string path, ReloadCallback callback)
        //     {

        //         try
        //         {
        //             WatchFiles.Add(global::System.IO.Path.GetFileName(path).ToLower(), callback);
        //         }
        //         catch (Exception e)
        //         {
        //             Logger.Error(path + " " + e);
        //         }

        //     }
        //     protected void Update(Queue<string> data)
        //     {

        //         lock (data)
        //         {
        //             if (data.Count == 0) { return; }

        //             ReloadCallback callback = null;
        //             System.Type type = null;
        //             int count = data.Count;
        //             for (int i = 0; i < count; ++i)
        //             {
        //                 var path = data.Dequeue();

        //                 var filename = global::System.IO.Path.GetFileName(path);
        //                 if (WatchFiles.TryGetValue(filename, out callback) == true)
        //                 {
        //                     try
        //                     {
        //                         callback(path);
        //                         OnCallback(path, true);
        //                         Logger.Info(path + " - Success");
        //                     }
        //                     catch (IOException e)
        //                     {
        //                         global::Caspar.Api.Logger.Debug(e);
        //                         data.Enqueue(path);
        //                     }
        //                     catch (Exception e)
        //                     {
        //                         global::Caspar.Api.Logger.Debug(e);
        //                         OnCallback(path, false);
        //                     }
        //                 }
        //                 else if (WatchFilesByType.TryGetValue(filename, out type) == true)
        //                 {

        //                     try
        //                     {
        //                         foreach (var attribute in type.GetCustomAttributes(false))
        //                         {

        //                             var metadata = attribute as global::Caspar.Attributes.Metadata;
        //                             if (metadata != null)
        //                             {

        //                                 string loader = "LoadXml";

        //                                 if (metadata.type == global::Caspar.Attributes.Metadata.Type.Json)
        //                                 {
        //                                     loader = "LoadJson";
        //                                 }

        //                                 var method = typeof(Metadata).GetMethod(loader, new System.Type[] { typeof(string) });
        //                                 if (method.IsGenericMethod == true)
        //                                 {
        //                                     method = method.MakeGenericMethod(type);

        //                                 }
        //                                 Logger.Info("Load Metadata " + filename);

        //                                 method.Invoke(null, new object[] { System.IO.Path.Combine(this.Path, filename) });

        //                                 if (string.IsNullOrEmpty(metadata.Builder) == false)
        //                                 {
        //                                     method = type.GetMethod(metadata.Builder, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        //                                     if (method != null)
        //                                     {
        //                                         method.Invoke(null, null);
        //                                     }
        //                                 }

        //                             }

        //                         }
        //                     }
        //                     catch (IOException e)
        //                     {
        //                         global::Caspar.Api.Logger.Debug(e);
        //                         data.Enqueue(path);
        //                     }
        //                     catch (Exception e)
        //                     {
        //                         global::Caspar.Api.Logger.Debug(e);
        //                         OnCallback(path, false);
        //                     }

        //                 }
        //                 else
        //                 {
        //                     Logger.Error(path + " - Fail");
        //                 }
        //             }
        //         }
        //     }

        //     public override void Update()
        //     {
        //         //Update(created);
        //         Update(changed);
        //     }


        //     internal static void AddWatchFile(string path, Type c)
        //     {
        //         WatchFilesByType.Add(global::System.IO.Path.GetFileName(path), c);
        //     }
        // };

        public static string BuildVersion4Digit(string before)
        {
            var date = global::Caspar.Api.KST;
            int year = date.Year - 2000;
            int month = date.Month;
            int day = date.Day;
            int revision = 0;

            if (string.IsNullOrEmpty(before))
            {
                return $"{year.ToString("00")}.{month.ToString("00")}.{day.ToString("00")}.{revision.ToString("00")}";
            }

            var tokens = before.Split('.');
            if (tokens == null || tokens.Length != 4)
            {
                return $"{year.ToString("00")}.{month.ToString("00")}.{day.ToString("00")}.{revision.ToString("00")}";
            }

            revision = tokens[3].ToInt32();
            if (year == tokens[0].ToInt32() && month == tokens[1].ToInt32() && day == tokens[2].ToInt32())
            {
                revision += 1;
            }
            else
            {
                revision = 0;
            }

            return $"{year.ToString("00")}.{month.ToString("00")}.{day.ToString("00")}.{revision.ToString("00")}";
        }

        public static long IpToLong(string publicIp, string privateIp)
        {
            return (long)IPAddressToUInt32(publicIp) << 32 | IPAddressToUInt32(privateIp);
        }

        static public int GetWeekOfYear()
        {
            var cultureInfo = CultureInfo.CurrentCulture;

            CalendarWeekRule calendarWeekRule = cultureInfo.DateTimeFormat.CalendarWeekRule;

            DayOfWeek firstDayOfWeek = cultureInfo.DateTimeFormat.FirstDayOfWeek;

            return cultureInfo.Calendar.GetWeekOfYear(DateTime.UtcNow, calendarWeekRule, firstDayOfWeek);
        }


        public static dynamic Config { get; set; }

        public interface ILog
        {
            void Error(object msg);
            void Warning(object msg);
            void Info(object msg);
            void Verbose(object msg);
        }

        // public static class Session
        // {
        //     public static Caspar.Database.Session Create()
        //     {
        //         return new Caspar.Database.Session();
        //     }
        // }

        public static class Logger
        {
            private static int validate = 0;
            private static int sequence = 0;
            private static string filename = string.Empty;
            private static TextWriter tw = new StreamWriter(System.Console.OpenStandardOutput());
            private static bool silence { get; set; } = false;
            public static bool Silence
            {
                get { return silence; }
                set
                {
                    if (value == silence)
                    {
                        return;
                    }
                    silence = value;
                    Initialize();
                    if (silence == true)
                    {
                        Directory.CreateDirectory($"Logs");
                    }
                    _ = Validate();
                }
            }


            public enum Type
            {
                Debug,
                Error,
                Warning,
                Info,
                Verbose,
                User,
                Stage,
            }

            public static void Initialize()
            {

                if (silence == false)
                {
                    if (AllowDebugLog == true)
                    {
                        Debug = (object msg) =>
                        {
                            Interlocked.Increment(ref validate);
                            // console color dark magenta
                            System.Console.ForegroundColor = ConsoleColor.DarkMagenta;
                            System.Console.WriteLine($"[{KST.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")}][{Interlocked.Increment(ref sequence)}][Debug] {msg}");
                            System.Console.ResetColor();
                        };
                    }
                    else
                    {
                        Debug = (object msg) => { };
                    }

                    Error = (object msg, object tags) =>
                    {
                        Interlocked.Increment(ref validate);
                        // console color red
                        System.Console.ForegroundColor = ConsoleColor.Red;
                        System.Console.WriteLine($"[{KST.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")}][{Interlocked.Increment(ref sequence)}][Error] {msg} {JsonConvert.SerializeObject(tags)}");
                        System.Console.ResetColor();
                    };

                    Warning = (object msg) =>
                    {
                        // console color yellow
                        System.Console.ForegroundColor = ConsoleColor.DarkRed;
                        Interlocked.Increment(ref validate);
                        System.Console.WriteLine($"[{KST.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")}][{Interlocked.Increment(ref sequence)}][Warning] {msg}");
                        System.Console.ResetColor();
                    };

                    Info = (object msg) =>
                    {
                        Interlocked.Increment(ref validate);
                        // console color green

                        System.Console.ForegroundColor = ConsoleColor.DarkCyan;
                        System.Console.WriteLine($"[{KST.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")}][{Interlocked.Increment(ref sequence)}][Info] {msg}");
                        System.Console.ResetColor();
                    };

                    Verbose = (object msg) =>
                    {
                        Interlocked.Increment(ref validate);
                        // console color gray
                        System.Console.ForegroundColor = ConsoleColor.Magenta;
                        System.Console.WriteLine($"[{KST.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")}][{Interlocked.Increment(ref sequence)}][Verbose] {msg}");
                        System.Console.ResetColor();
                    };

                    Stage = (long idx, object msg, object tags) =>
                    {
                        Interlocked.Increment(ref validate);
                        // console color cyan
                        System.Console.ForegroundColor = ConsoleColor.Cyan;
                        System.Console.WriteLine($"[{KST.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")}][{Interlocked.Increment(ref sequence)}][Stage][{idx}] {msg} {JsonConvert.SerializeObject(tags)}");
                        System.Console.ResetColor();
                    };

                    User = (long idx, object msg, object tags) =>
                    {

                        Interlocked.Increment(ref validate);
                        // dark cyan
                        System.Console.ForegroundColor = ConsoleColor.Green;
                        System.Console.WriteLine($"[{KST.ToString("yyyy/MM/dd HH:mm:ss.fffzzz")}][{Interlocked.Increment(ref sequence)}][User][{idx}] {msg} {JsonConvert.SerializeObject(tags)}");
                        System.Console.ResetColor();

                    };
                }
                else
                {
                    uint ip = global::Caspar.Api.IPAddressToUInt32(PublicIp);
                    if (AllowDebugLog == true)
                    {
                        Debug = (object msg) =>
                        {
                            Interlocked.Increment(ref validate);
                            tw.WriteLine($"{Api.Deploy.PPRT}\\,{ip}\\,{KST.ToString("yyyy-MM-ddTHH:mm:ss.fff")}\\,{Interlocked.Increment(ref sequence)}\\,{(int)Type.Debug}\\,0\\,{msg}\\;");
                        };
                    }
                    else
                    {
                        Debug = (object msg) => { };
                    }


                    Error = (object msg, object tags) =>
                    {
                        Interlocked.Increment(ref validate);
                        tw.WriteLine($"{Api.Deploy.PPRT}\\,{ip}\\,{KST.ToString("yyyy-MM-ddTHH:mm:ss.fff")}\\,{Interlocked.Increment(ref sequence)}\\,{(int)Type.Error}\\,0\\,{msg}\\,{JsonConvert.SerializeObject(tags)}\\;");
                    };

                    Warning = (object msg) =>
                    {
                        Interlocked.Increment(ref validate);
                        tw.WriteLine($"{Api.Deploy.PPRT}\\,{ip}\\,{KST.ToString("yyyy-MM-ddTHH:mm:ss.fff")}\\,{Interlocked.Increment(ref sequence)}\\,{(int)Type.Warning}\\,0\\,{msg}\\;");
                    };

                    Info = (object msg) =>
                    {
                        Interlocked.Increment(ref validate);
                        tw.WriteLine($"{Api.Deploy.PPRT}\\,{ip}\\,{KST.ToString("yyyy-MM-ddTHH:mm:ss.fff")}\\,{Interlocked.Increment(ref sequence)}\\,{(int)Type.Info}\\,0\\,{msg}\\;");
                    };

                    Verbose = (object msg) =>
                    {
                        Interlocked.Increment(ref validate);
                        tw.WriteLine($"{Api.Deploy.PPRT}\\,{ip}\\,{KST.ToString("yyyy-MM-ddTHH:mm:ss.fff")}\\,{Interlocked.Increment(ref sequence)}\\,{(int)Type.Verbose}\\,0\\,{msg}\\;");
                    };

                    if (Config.Deploy == "PD")
                    {
                        Stage = (long idx, object msg, object tags) => { };
                    }
                    else
                    {
                        Stage = (long idx, object msg, object tags) =>
                        {
                            Interlocked.Increment(ref validate);
                            tw.WriteLine($"{Api.Deploy.PPRT}\\,{ip}\\,{KST.ToString("yyyy-MM-ddTHH:mm:ss.fff")}\\,{Interlocked.Increment(ref sequence)}\\,{(int)Type.Stage}\\,{idx}\\,{msg}\\,{JsonConvert.SerializeObject(tags)}\\;");
                        };
                    }

                    User = (long idx, object msg, object tags) =>
                    {
                        Interlocked.Increment(ref validate);
                        tw.WriteLine($"{Api.Deploy.PPRT}\\,{ip}\\,{KST.ToString("yyyy-MM-ddTHH:mm:ss.fff")}\\,{Interlocked.Increment(ref sequence)}\\,{(int)Type.User}\\,{idx}\\,{msg}\\,{JsonConvert.SerializeObject(tags)}\\;");

                    };

                }

            }
#if DEBUG
            private static bool allowDebugLog { get; set; } = true;
#else
            private static bool allowDebugLog { get; set; } = false;
#endif

            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public static bool AllowDebugLog { get { return allowDebugLog; } set { allowDebugLog = value; Initialize(); } }

            public delegate void LogCallback(object msg);
            public delegate void ErrorCallback(object msg, object tags = null);
            public delegate void UserLogCallback(long idx, object msg, object tags = null);

            private static DateTime flush { get; set; } = DateTime.UtcNow.AddMinutes(1);

            public static UserLogCallback Stage;

            public static LogCallback Debug;
            public static ErrorCallback Error;
            public static LogCallback Warning;
            public static LogCallback Info;
            public static UserLogCallback User;
            public static LogCallback Verbose;

            public static async Task Flush()
            {
                if (Silence == false) { return; }
                if (validate > 100 && StandAlone == false)
                {
                    flush = DateTime.UtcNow.AddMinutes(1);
                    await Validate();
                    return;
                }
                if (flush > DateTime.UtcNow) { return; }
                if (StandAlone == true) { return; }
                flush = DateTime.UtcNow.AddMinutes(1);
                await Validate();
            }

            private static async Task Validate()
            {
                var bfn = filename;
                var bsw = tw;

                if (silence == true)
                {
                    FileStream fs = null;
                    filename = Path.Combine($"Logs", $"{KST.Ticks}.log");
                    while (File.Exists(filename) == true)
                    {
                        await Task.Delay(10);
                        filename = Path.Combine($"Logs", $"{KST.Ticks}.log");
                    }

                    try
                    {
                        fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Read);
                    }
                    catch (Exception e)
                    {
                        fs?.Dispose();
                        Error(e);
                        return;
                    }

                    var sw = new StreamWriter(fs);
                    sw.AutoFlush = true;
                    tw = TextWriter.Synchronized(sw);
                    bsw?.Flush();
                    bsw?.Close();
                    bsw?.Dispose();
                    //System.Console.SetOut(sw);
                    //System.Console.SetError(sw);
                }
                else
                {
                    bsw?.Flush();
                }
                //else
                //{
                //    var sop = new StreamWriter(System.Console.OpenStandardOutput());
                //    sop.AutoFlush = true;
                //    System.Console.SetOut(sop);
                //    System.Console.SetError(sop);
                //}




                if (bfn.IsNullOrEmpty() == false)
                {
                    Interlocked.Exchange(ref validate, 0);
                }
            }

        }

        public static DateTime UTC
        {
            get
            {
                return DateTime.UtcNow;
            }
        }

        public static DateTime SST
        {
            get
            {
                if (global::Caspar.Api.Config.ServerStandardTime == null)
                {
                    return DateTime.UtcNow.ConvertTimeFromUtc("Korea Standard Time");
                }

                return UTC.ConvertTimeFromUtc((string)global::Caspar.Api.Config.ServerStandardTime);
            }
        }

        public static DateTime KST
        {
            get
            {
                return DateTime.UtcNow.ConvertTimeFromUtc("Korea Standard Time");
            }
        }

        public static class Deploy
        {
            public static int PXXX { get; set; }
            public static int PPXX { get; set; }
            public static int PPRX { get; set; }
            public static int PPRT { get; set; }
        }

        public static void Registration(bool seed)
        {
            if (seed == true)
            {
                return;
            }

            if (Caspar.Api.OnLambda == true)
            {
                return;
            }
            var connectionString = new MySql.Data.MySqlClient.MySqlConnectionStringBuilder();

            dynamic db = global::Caspar.Api.Config.Databases.Caspar;

            connectionString.UserID = db.Id;
            connectionString.Password = db.Pw;

            if (db.Crypt == true)
            {
                connectionString.UserID = global::Caspar.Api.DesDecrypt(connectionString.UserID, "magimagi");
                connectionString.Password = global::Caspar.Api.DesDecrypt(connectionString.Password, "magimagi");
            }
            connectionString.Server = db.Ip;
            try
            {

                if (db.IAM == true)
                {
                    var awsCredentials = new Amazon.Runtime.BasicAWSCredentials((string)global::Caspar.Api.Config.AWS.Access.KeyId, (string)global::Caspar.Api.Config.AWS.Access.SecretAccessKey);
                    var pwd = Amazon.RDS.Util.RDSAuthTokenGenerator.GenerateAuthToken(awsCredentials, RegionEndpoint.APNortheast2, connectionString.Server, 3306, connectionString.UserID);
                    connectionString.Password = pwd;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            connectionString.Port = Convert.ToUInt32(db.Port);
            connectionString.Database = db.Db;
            connectionString.Pooling = false;
            connectionString.AllowZeroDateTime = true;
            connectionString.CheckParameters = false;
            connectionString.UseCompression = false;
            connectionString.ConnectionTimeout = 30;

            var connectionStringValue = connectionString.GetConnectionString(true);
            Logger.Debug($"Registration to {connectionStringValue}");

            while (true)
            {
                try
                {

                    using var connection = new MySql.Data.MySqlClient.MySqlConnection(connectionStringValue);
                    Logger.Info($"Registration to {connectionString.Server}:{connectionString.Port}");

                    connection.Open();

                    using var command = connection.CreateCommand();

                    if (Deploy.PPRT == 0)
                    {
                        command.Parameters.Clear();
                        command.CommandText = $"SELECT id FROM `caspar`.`deploy` WHERE `provider` = @provider AND `publish` = '' AND `region` = '' AND `type` = '';";
                        command.CommandText += $"SELECT id FROM `caspar`.`deploy` WHERE `provider` = @provider AND `publish` = @publish AND `region` = '' AND `type` = '';";
                        command.CommandText += $"SELECT id FROM `caspar`.`deploy` WHERE `provider` = @provider AND `publish` = @publish AND `region` = @region AND `type` = '';";
                        command.CommandText += $"SELECT id FROM `caspar`.`deploy` WHERE `provider` = @provider AND `publish` = @publish AND `region` = @region AND `type` = @type;";

                        command.Parameters.AddWithValue("@provider", (string)global::Caspar.Api.Config.Provider);
                        command.Parameters.AddWithValue("@publish", (string)global::Caspar.Api.Config.Publish);
                        command.Parameters.AddWithValue("@region", (string)global::Caspar.Api.Config.Region);
                        command.Parameters.AddWithValue("@type", ServerType);
                        command.Parameters.AddWithValue("@ip", PublicIp);

                        Logger.Info($"Provider : {Caspar.Api.Config.Provider}");
                        Logger.Info($"Publish : {Caspar.Api.Config.Publish}");
                        Logger.Info($"Region : {Caspar.Api.Config.Region}");
                        Logger.Info($"Type : {ServerType}");
                        Logger.Info($"IP : {PublicIp}");
                        using (var reader = command.ExecuteReader())
                        {
                            try
                            {
                                reader.Read();
                                Deploy.PXXX = reader[0].ToInt32();

                                reader.NextResult();
                                reader.Read();
                                Deploy.PPXX = reader[0].ToInt32();

                                reader.NextResult();
                                reader.Read();
                                Deploy.PPRX = reader[0].ToInt32();

                                reader.NextResult();
                                reader.Read();
                                Deploy.PPRT = reader[0].ToInt32();
                            }
                            catch
                            {

                            }
                            finally
                            {
                                reader.Close();
                            }
                        }
                    }

                    if (Deploy.PPRT == 0)
                    {
                        command.Transaction = connection.BeginTransaction();
                        command.Parameters.Clear();
                        command.CommandText = $"INSERT IGNORE INTO `caspar`.`deploy` (`provider`, `publish`, `region`, `type`, `ip`) VALUES (@provider, '', '', '', @ip);";
                        command.CommandText += $"INSERT IGNORE INTO `caspar`.`deploy` (`provider`, `publish`, `region`, `type`, `ip`) VALUES (@provider, @publish, '', '', @ip);";
                        command.CommandText += $"INSERT IGNORE INTO `caspar`.`deploy` (`provider`, `publish`, `region`, `type`, `ip`) VALUES (@provider, @publish, @region, '', @ip);";
                        command.CommandText += $"INSERT IGNORE INTO `caspar`.`deploy` (`provider`, `publish`, `region`, `type`, `ip`) VALUES (@provider, @publish, @region, @type, @ip);";

                        command.Parameters.AddWithValue("@provider", (string)global::Caspar.Api.Config.Provider);
                        command.Parameters.AddWithValue("@publish", (string)global::Caspar.Api.Config.Publish);
                        command.Parameters.AddWithValue("@region", (string)global::Caspar.Api.Config.Region);
                        command.Parameters.AddWithValue("@type", ServerType);
                        command.Parameters.AddWithValue("@ip", PublicIp);
                        command.ExecuteNonQuery();
                        command.Transaction.Commit();
                    }
                    else
                    {
                        command.Transaction = connection.BeginTransaction();
                        command.Parameters.Clear();
                        command.CommandText = $"UPDATE `caspar`.`deploy` SET `ip` = @ip WHERE `provider` = @provider AND `publish` = '' AND `region` = '' AND `type` = '';";
                        command.CommandText += $"UPDATE `caspar`.`deploy` SET `ip` = @ip WHERE `provider` = @provider AND `publish` = @publish AND `region` = '' AND `type` = '';";
                        command.CommandText += $"UPDATE `caspar`.`deploy` SET `ip` = @ip WHERE `provider` = @provider AND `publish` = @publish AND `region` = @region AND `type` = '';";
                        command.CommandText += $"UPDATE `caspar`.`deploy` SET `ip` = @ip WHERE `provider` = @provider AND `publish` = @publish AND `region` = @region AND `type` = @type;";
                        command.Parameters.AddWithValue("@provider", (string)global::Caspar.Api.Config.Provider);
                        command.Parameters.AddWithValue("@publish", (string)global::Caspar.Api.Config.Publish);
                        command.Parameters.AddWithValue("@region", (string)global::Caspar.Api.Config.Region);
                        command.Parameters.AddWithValue("@type", ServerType);
                        command.Parameters.AddWithValue("@ip", PublicIp);
                        command.ExecuteNonQuery();
                        command.Transaction.Commit();
                    }

                    // command.CommandText = $"SELECT `id` FROM `caspar`.`snowflakes` WHERE `type` = @type AND `public_ip` = @publicIp AND `private_ip` = @privateIp;";
                    // command.Parameters.Clear();
                    // command.Parameters.AddWithValue("@type", ServerType);
                    // command.Parameters.AddWithValue("@publicIp", PublicIp);
                    // command.Parameters.AddWithValue("@privateIp", PrivateIp);
                    // using (var reader = command.ExecuteReader())
                    // {
                    //     try
                    //     {
                    //         if (reader.Read() == false)
                    //         {
                    //             reader.Close();
                    //             command.CommandText = $"INSERT INTO `caspar`.`snowflakes` (`type`, `public_ip`, `private_ip`) VALUES (@type, @publicIp, @privateIp);";
                    //             command.ExecuteNonQuery();
                    //         }
                    //         else
                    //         {
                    //             Api.Snowflake.Instance = reader[0].ToInt32();
                    //         }
                    //     }
                    //     catch (Exception e)
                    //     {
                    //         Logger.Error(e);
                    //     }
                    //     finally
                    //     {
                    //         reader.Close();
                    //     }

                    // }
                    // command.Parameters.Clear();
                    // command.Transaction = connection.BeginTransaction();
                    // command.CommandText = "UPDATE `caspar`.`seed` SET `value` = `value` + 10 WHERE `id` = 1;";
                    // if (command.ExecuteNonQuery() == 0)
                    // {
                    //     command.Transaction.Rollback();
                    //     continue;
                    // }
                    // command.CommandText = "SELECT `value` FROM `caspar`.`seed` WHERE `id` = 1 FOR UPDATE;";
                    // using (var reader = command.ExecuteReader())
                    // {
                    //     try
                    //     {
                    //         if (reader.Read() == true)
                    //         {
                    //             Api.Seed.Value = reader[0].ToInt32();
                    //         }
                    //     }
                    //     catch (Exception e)
                    //     {
                    //         Logger.Error(e);
                    //     }
                    //     finally
                    //     {
                    //         reader.Close();
                    //     }
                    // }

                    // command.Transaction.Commit();
                    // connection.Close();

                    // // if (Api.Snowflake.Instance == 0)
                    // // {
                    // //     continue;
                    // // }
                    // // Api.Snowflake.StartUp();
                    // if (Api.Seed.Value == 0)
                    // {
                    //     continue;
                    // }
                    if (Deploy.PPRT != 0) { break; }
                }
                catch (Exception e)
                {
                    Logger.Error(e);

                }
            }

        }

        public static async Task StartUp(string[] args, bool seed = false)
        {

            if (isOpen == true)
                return;

            if (ServerType == "Agent")
            {
                Caspar.Api.ThreadCount = 2;
                Caspar.Api.MaxSession = 3;
            }


            var setting = new global::Google.Protobuf.JsonFormatter.Settings(true);
            setting = setting.WithFormatEnumsAsIntegers(true);
            JsonFormatter = new global::Google.Protobuf.JsonFormatter(setting);

            Logger.Initialize();

            args ??= new string[] { "" };

            StartUpLocalConfiguration();

            await StartUpRemoteConfiguration(args);


            try
            {
                foreach (var e in args)
                {
                    if (e.ToLower().StartsWith("platform="))
                    {
                        Config.CloudPlatform = e.Split('=')[1];
                    }

                    if (e.ToLower().StartsWith("standalone"))
                    {
                        Config.Silence = false;
                        StandAlone = true;
                    }

                    if (e.ToLower().StartsWith("version="))
                    {
                        Api.Version = e.Split('=')[1];
                        Logger.Info($"Version : {Api.Version}");
                    }
                }

            }
            catch (Exception e)
            {
                Logger.Info($"{args}");
                Logger.Error(e);
            }



            await StartUpNetworkInterface(seed);
            OverrideConfiguration();
            global::Framework.Protobuf.Api.StartUp();
            StartUpVivox();
            StartUpAWS();
            Registration(seed);
            StartUpLayer(args);
            StartUpAttributes();
            StartUpDatabase();
        }

        public static void StartUpLocalConfiguration()
        {
            try
            {
                Logger.Info("StartUp Framework... local config");
                var caspar = File.OpenText(Path.Combine(Directory.GetCurrentDirectory(), "Caspar.json"));
                Config = JObject.Parse(caspar.ReadToEnd());
                Caspar.Api.ServerType = Config.ServerType;
                var field = typeof(RegionEndpoint).GetField((string)Config.AWS.S3.RegionEndpoint);
                RegionEndpoint endpoint = (RegionEndpoint)field?.GetValue(null) ?? throw new Exception();
                global::Caspar.CDN.S3Client = new AmazonS3Client((string)Config.AWS.S3.Key, (string)Config.AWS.S3.Secret, endpoint);
                Caspar.CDN.Domain = (string)Config.AWS.S3.Domain;
            }
            catch (System.IO.FileNotFoundException e)
            {

            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public static async Task StartUpRemoteConfiguration(string[] args)
        {
            //cdn.Container = "hal";
            global::Caspar.Api.Config = null;
            JObject json = null;

            Logger.Info("StartUp Framework... config");
            try
            {

                foreach (var e in args)
                {
                    if (e.ToLower().StartsWith("s3="))
                    {
                        var task = await global::Caspar.CDN.Get($"{e.Split('=')[1]}.json");

                        json = JObject.Parse(new StreamReader(task).ReadToEnd());
                        break;
                    }
                    else if (e.ToLower().StartsWith("json="))
                    {
                        var base64String = e.Split('=')[1];
                        base64String = base64String.FromBase64UrlDecode();
                        Logger.Info($"Merge json={base64String}");
                        json = JObject.Parse(base64String);
                        break;
                    }
                    else if (e.ToLower().StartsWith("file="))
                    {
                        json = JObject.Parse(new StreamReader(File.OpenRead(e.Split('=')[1])).ReadToEnd());
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Info($"{args}");
                Logger.Error(e);
            }

            if (json == null)
            {
                // exit program
                Environment.Exit(0);
            }
            Config = json;
        }

        public static async Task StartUpNetworkInterface(bool seed)
        {
            Logger.Info($"StartUp Framework... ip setting {(string)Config.Agent.Ip}");
            PublicIp = string.Empty;

            if (seed == true)
            {
                PublicIp = "127.0.0.1";
            }

            while (PublicIp.IsNullOrEmpty() == true)
            {
                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        var res = await httpClient.GetAsync($"{Config.Seed}");
                        var content = await res.Content.ReadAsStringAsync();
                        var ret = JObject.Parse(content);
                        Api.Seed.Value = (int)ret.GetValue("Offset");
                        PublicIp = (string)ret.GetValue("RemoteIp");
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }

                if (seed == true)
                {
                    PublicIp = "127.0.0.1";
                }
            }


            PrivateIp = "127.0.0.1";
            List<string> privates = new();
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && ni.OperationalStatus == OperationalStatus.Up)
                        {
                            privates.Add(ip.Address.ToString());
                        }
                    }
                }
            }

            if (privates.Count == 0)
            {
                PrivateIp = PublicIp;
            }
            else
            {
                foreach (var e in privates)
                {
                    // if (e == PublicIp) { continue; }
                    // if (e == "127.0.0.1") { continue; }
                    // if (e == "localhost") { continue; }
                    PrivateIp = e;
                    break;
                }
            }

            if (StandAlone == true)
            {
                Config.Silence = false;
                Config.Service = false;
                global::Caspar.Api.Config.Provider = $"{PublicIp}-{PrivateIp}";
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    {
                        foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && ni.OperationalStatus == OperationalStatus.Up)
                            {
                                global::Caspar.Api.Config.Publish = $"{ip.Address.ToString()}"; ;
                            }
                        }
                    }
                }
                global::Caspar.Api.Config.Region = "KR";
                global::Caspar.Api.ServerType = "StandAlone";
            }

            global::Caspar.Api.Logger.Silence = (bool)Config.Silence;
            Logger.Info($"Public Ip : {PublicIp}");
            Logger.Info($"PrivateIp Ip : {PrivateIp}");
            Logger.Info($"ServiceIp Ip : {ServiceIp}");

            Idx = (long)IPAddressToUInt32(PublicIp) << 32 | IPAddressToUInt32(PrivateIp);
            ServerId = Idx.ToString();
            Logger.Debug($"Idx: {Idx}");
        }

        public static void OverrideConfiguration()
        {
            try
            {
                var address = PrivateIp.Split('.');
                var platform = (JObject)Config.Override[(string)Config.CloudPlatform];

                if (platform != null && StandAlone == false)
                {
                    var @override = platform[$"0.0.0.0/0"];
                    if (@override != null)
                    {
                        Config.Merge(@override, new JsonMergeSettings() { MergeArrayHandling = MergeArrayHandling.Merge, MergeNullValueHandling = MergeNullValueHandling.Ignore });
                    }

                    @override = platform[$"{address[0]}.0.0.0/8"];
                    if (@override != null)
                    {
                        Config.Merge(@override, new JsonMergeSettings() { MergeArrayHandling = MergeArrayHandling.Merge, MergeNullValueHandling = MergeNullValueHandling.Ignore });
                    }

                    @override = platform[$"{address[0]}.{address[1]}.0.0/16"];
                    if (@override != null)
                    {
                        Config.Merge(@override, new JsonMergeSettings() { MergeArrayHandling = MergeArrayHandling.Merge, MergeNullValueHandling = MergeNullValueHandling.Ignore });
                    }

                    @override = platform[$"{address[0]}.{address[1]}.{address[2]}.0/24"];
                    if (@override != null)
                    {
                        Config.Merge(@override, new JsonMergeSettings() { MergeArrayHandling = MergeArrayHandling.Merge, MergeNullValueHandling = MergeNullValueHandling.Ignore });
                    }

                    @override = platform[$"{PrivateIp}/32"];
                    if (@override != null)
                    {
                        Config.Merge(@override, new JsonMergeSettings() { MergeArrayHandling = MergeArrayHandling.Merge, MergeNullValueHandling = MergeNullValueHandling.Ignore });
                    }
                }
                else
                {
                    Logger.Info("Cant Find Override Configs");
                }

            }
            catch (Exception e)
            {
                Logger.Error(e);
            }


            if (StandAlone == true)
            {
                try
                {
                    Config.Merge(JObject.Parse(new StreamReader(File.OpenRead("Config.json")).ReadToEnd()), new JsonMergeSettings() { MergeArrayHandling = MergeArrayHandling.Merge, MergeNullValueHandling = MergeNullValueHandling.Ignore });
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        }

        public static void StartUpVivox()
        {
            try
            {
                dynamic vivox = global::Caspar.Api.Config.Vivox;
                global::Caspar.Api.Vivox.API = vivox.API;
                global::Caspar.Api.Vivox.Domain = vivox.Domain;
                global::Caspar.Api.Vivox.Issuer = vivox.Issuer;
                global::Caspar.Api.Vivox.Secret = vivox.Secret;
                global::Caspar.Api.Vivox.Admin = vivox.Admin;
                global::Caspar.Api.Vivox.Password = vivox.Password;
            }
            catch
            {

            }

            global::Caspar.Api.Vivox.Key = Encoding.UTF8.GetBytes(global::Caspar.Api.Vivox.Secret);

        }

        private static void CreateLayerWorker(int index)
        {
            var t = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        var p = Layer.Queued[index].Take();
                        p.process(index);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                        Logger.Error(e);
                        Logger.Error(e);
                        Logger.Error(e);
                        Logger.Error(e);
                        Logger.Error(e);
                        Logger.Error(e);
                        Logger.Error(e);
                        Logger.Error(e);
                    }

                }
            });
            t.Name = $"Layer Worker {index + 1}";
            t.IsBackground = true;
            t.Start();
        }

        public static void StartUpLayer(string[] args)
        {

            ThreadPool.SetMaxThreads(ThreadCount * 4, ThreadCount * 4);
            ThreadPool.SetMinThreads(ThreadCount, ThreadCount);

            global::Caspar.Attributes.Override.StartUp();
            AppDomain.CurrentDomain.UnhandledException += App_UnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
            isOpen = true;

            foreach (var e in args)
            {
                if (e.StartsWith("NoLayer") == true)
                {
                    return;
                }
            }

            Layer.TotalWorkers = global::Caspar.Api.ThreadCount;
            Layer.Queued = new BlockingCollection<Layer>[Layer.TotalWorkers];
            for (int i = 0; i < Layer.TotalWorkers; ++i)
            {
                Layer.Queued[i] = new BlockingCollection<Layer>();
            }
            Layer.options = new ParallelOptions()
            {
                MaxDegreeOfParallelism = Layer.TotalWorkers
            };

            for (int i = 0; i < Layer.TotalWorkers; ++i)
            {
                CreateLayerWorker(i);
            }

            thread = new Thread(new ThreadStart(() => { _ = LayerUpdate(); }));
            thread.Name = "Layer Updator";
            thread.IsBackground = true;
            thread.Start();
        }

        public static void StartUpAttributes()
        {
            var classes = (from asm in AppDomain.CurrentDomain.GetAssemblies()
                           from type in asm.GetTypes()
                           where type.IsClass
                           select type);

            List<KeyValuePair<int, System.Type>> attributes = new List<KeyValuePair<int, Type>>();
            foreach (var c in classes)
            {
                try
                {
                    foreach (var attribute in c.GetCustomAttributes(false))
                    {

                        var startUp = attribute as global::Caspar.Attributes.StartUp;
                        if (startUp != null)
                        {
                            attributes.Add(new KeyValuePair<int, System.Type>(startUp.Priority, c));
                        }
                    }
                }
                catch
                {

                }
            }


            global::Caspar.Attributes.GenerateId.StartUp();
            global::Caspar.Attributes.Initialize.StartUp();

            var list = attributes.OrderBy(x => x.Key).ToList();
            foreach (var c in list)
            {
                c.Value.GetMethod("StartUp").Invoke(null, null);
            }
        }

        public static void StartUpAWS()
        {
            try
            {

                string pem = (string)Config.AWS.CloudFront.PEM;
                var bytes = Encoding.UTF8.GetBytes(pem);
                Caspar.CDN.PEM = () =>
                {
                    return new MemoryStream(bytes);
                };

                global::Caspar.CDN.CloudFront = (string)Config.AWS.CloudFront.Domain;
                global::Caspar.CDN.CFKeyId = (string)Config.AWS.CloudFront.Key;

                //    await global::Caspar.CDN.Get($"QA/Lobby/24.12.18.00", "temp");


            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            try
            {
                string keyId = (string)global::Caspar.Api.Config.AWS.Access.KeyId;
                string secretAccessKey = (string)global::Caspar.Api.Config.AWS.Access.SecretAccessKey;

                try
                {
                    JObject s3 = global::Caspar.Api.Config.AWS.S3;
                    foreach (dynamic e in s3.Children())
                    {
                        try
                        {
                            dynamic element = s3[e.Name];
                            if (element.Disable != null && element.Disable == true)
                            {
                                continue;
                            }
                            Caspar.Platform.AWS.S3.Add(e.Name, new Platform.AWS.S3()
                            {
                                KeyId = keyId,
                                SecretAccessKey = secretAccessKey,
                                Domain = (string)element.Domain,
                                Endpoint = (RegionEndpoint)typeof(RegionEndpoint).GetField((string)element.RegionEndpoint).GetValue(null)
                            });
                        }
                        catch (System.Exception ex)
                        {
                            Logger.Error(ex);
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }

                try
                {
                    JObject sqs = global::Caspar.Api.Config.AWS.SQS;
                    foreach (dynamic e in sqs.Children())
                    {
                        try
                        {
                            dynamic element = sqs[e.Name];
                            if (element.Disable != null && element.Disable == true)
                            {
                                continue;
                            }

                            Caspar.Platform.AWS.SQS.Add(e.Name, new Platform.AWS.SQS()
                            {
                                KeyId = keyId,
                                SecretAccessKey = secretAccessKey,
                                URL = (string)element.URL,
                                Endpoint = (RegionEndpoint)typeof(RegionEndpoint).GetField((string)element.RegionEndpoint).GetValue(null)
                            });
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            Platform.AWS.SES.StartUp();
        }

        public static void CleanUp()
        {
            isOpen = false;
            CleanUpDatabase();
            thread.Join();
        }

        public static string ServerType { get; set; } = "None";
        public static bool StandAlone { get; set; } = false;
        public static string Version { get; set; } = "0.0.0.0";
        private static void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {

            //e.IsTerminating = false;
            Logger.Error("App_UnhandledException " + e.ExceptionObject);

        }

        private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
            Logger.Error("OnUnobservedTaskException " + e.Exception);
        }

        private static void OnDeleted(object sender, FileSystemEventArgs e)
        {
        }

        private static void OnCreated(object sender, FileSystemEventArgs e)
        {
        }

        private static void OnRenamed(object sender, RenamedEventArgs e)
        {
        }

        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
        }


        public static long AddressToInt64(string ip, ushort port)
        {
            string[] ips = ip.Split('.');
            long id = 0;
            for (int i = 0; i < 4; ++i)
            {
                id = id << 8;
                ushort n = ips[i].ToUInt16();
                id |= n;
            }

            id = id << 16;
            id |= port;
            return id;
        }
        public static uint IPAddressToUInt32(string ip)
        {
            if (ip == null) { return 0; }
            string[] ips = ip.Split('.');
            uint id = 0;
            for (int i = 0; i < 4; ++i)
            {
                id = id << 8;
                ushort n = ips[i].ToUInt16();
                id |= n;
            }

            return id;
        }

        public static string LongToPP(long idx)
        {
            return $"{UInt32ToIPAddress((uint)(idx >> 32))}-{UInt32ToIPAddress((uint)idx)}";
        }

        public static string UInt32ToIPAddress(uint value)
        {
            return $"{value >> 24}.{(value & 0x00FF0000) >> 16}.{(value & 0x0000FF00) >> 8}.{(value & 0x000000FF)}";
        }

        public static string Int64ToIPAddress(long value)
        {
            string ip = UInt32ToIPAddress((uint)(value >> 16));
            ushort port = (ushort)value;
            return $"{ip}:{port}";
        }

        public static string ToAddress(this uint value)
        {
            return UInt32ToIPAddress(value);
        }

        public static string ToAddress(this EndPoint endPoint)
        {

            if (endPoint == null)
            {
                return "";
            }

            if ((endPoint is IPEndPoint) == false)
            {
                return "";
            }

            if ((endPoint as IPEndPoint).Address == null)
            {
                return "";
            }

            return (endPoint as IPEndPoint).Address.ToString();
        }
    }
}

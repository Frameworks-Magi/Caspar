using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Framework.Caspar;
using StackExchange.Redis;

using static Framework.Caspar.Api;


namespace Framework.Caspar.Database.NoSql
{
    public sealed class Redis : IConnection
    {

        internal static ConcurrentDictionary<int, Layer.Entity> tasks = new ConcurrentDictionary<int, Layer.Entity>();
        internal static Dictionary<string, Dictionary<Action<RedisChannel, RedisValue>, Action<RedisChannel, RedisValue>>> subscribers = new Dictionary<string, Dictionary<Action<RedisChannel, RedisValue>, Action<RedisChannel, RedisValue>>>();

        public IConnection Create()
        {
            return this;
        }

        public static void Subscribe(string channel, Action<RedisChannel, RedisValue> callback, int strand = 0)
        {

            if (subscribers.TryGetValue(channel, out Dictionary<Action<RedisChannel, RedisValue>, Action<RedisChannel, RedisValue>> callbacks) == false)
            {
                callbacks = new Dictionary<Action<RedisChannel, RedisValue>, Action<RedisChannel, RedisValue>>();
                subscribers.Add(channel, callbacks);
            }


            if (callbacks.ContainsKey(callback) == false)
            {
                var task = new Layer.Entity(Singleton<Api.RedisLayer>.Instance);
                task = tasks.GetOrAdd(strand, task);

                task.UID = strand;


                var lambda = new Action<RedisChannel, RedisValue>((c, v) =>
                {
                    task.PostMessage(() => { callback(c, v); });
                });

                callbacks.Add(callback, lambda);
                var sub = redis.GetSubscriber();
                sub.SubscribeAsync(channel, lambda);
            }


        }

        public static void Publish(string channel, RedisValue value)
        {
            redis.GetSubscriber().PublishAsync(channel, value);
        }

        public static void Unsubscribe(string channel, Action<RedisChannel, RedisValue> callback = null, CommandFlags flags = CommandFlags.None)
        {
            if (callback == null)
            {
                redis.GetSubscriber().Unsubscribe(channel, null, flags);
                return;
            }

            if (subscribers.TryGetValue(channel, out Dictionary<Action<RedisChannel, RedisValue>, Action<RedisChannel, RedisValue>> callbacks) == false)
            {
                callbacks = new Dictionary<Action<RedisChannel, RedisValue>, Action<RedisChannel, RedisValue>>();
                subscribers.Add(channel, callbacks);
            }



            callbacks.TryGetValue(callback, out Action<RedisChannel, RedisValue> wrapper);
            redis.GetSubscriber().Unsubscribe(channel, wrapper, flags);

        }
        public static void UnsubscribeAll(CommandFlags flags = CommandFlags.None)
        {
            redis.GetSubscriber().UnsubscribeAll(flags);
        }

        public string Name { get; set; }

        public class Address
        {
            public string IP { get; set; }
            public string Port { get; set; }
        }

        private (string, short) Master { get; set; } = ("", 0);
        private List<(string, short)> Slaves { get; set; } = new List<(string, short)>();
        private List<(string, short)> Readonly { get; set; } = new List<(string, short)>();

        public string Id { get; set; }
        public string Pw { get; set; }
        public string Db { get; set; }
        public Version Version { get; set; } = new Version(3, 2, 1);

        //ServiceStack.Redis.PooledRedisClientManager redisManager = null;
        //public ThreadLocal<ServiceStack.Redis.RedisClient> Connection = new ThreadLocal<ServiceStack.Redis.RedisClient>();

        static ConnectionMultiplexer redis = null;
        static ConnectionMultiplexer read = null;


        public void Initialize()
        {


            try
            {
                ConfigurationOptions config = new ConfigurationOptions
                {
                    KeepAlive = 180,
                    DefaultVersion = Version,
                    Password = Pw,
                    AllowAdmin = true,
                };

                config.EndPoints.Add(Master.Item1, Master.Item2);

                foreach (var e in Slaves)
                {
                    config.EndPoints.Add(e.Item1, e.Item2);
                }

                Logger.Info("Try Connect Redis... If connection fail check aws security.");

                redis = ConnectionMultiplexer.Connect(config);


                if (Readonly.Count > 0)
                {
                    config = new ConfigurationOptions
                    {
                        KeepAlive = 180,
                        DefaultVersion = new Version(3, 2, 1),
                        Password = Pw,
                    };

                    foreach (var e in Readonly)
                    {
                        config.EndPoints.Add(e.Item1, e.Item2);
                    }

                }
                else
                {
                    read = redis;
                }

            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

        }

        public void SetMaster(string ip, string port)
        {
            Master = (ip, port.ToInt16());
        }

        public void AddSlave(string ip, string port)
        {
            Slaves.Add((ip, port.ToInt16()));
        }

        public void AddReadOnly(string ip, string port)
        {
            Readonly.Add((ip, port.ToInt16()));
        }

        public void BeginTransaction() { }
        public void Commit() { }
        public async Task CommitAsync() { await Task.CompletedTask; }
        public void Rollback() { }
        public async Task RollbackAsync() { await Task.CompletedTask; }
        public async Task<IConnection> Open(CancellationToken token = default, bool transaction = true)
        {
            await Task.CompletedTask;
            return null;
        }
        public void Close() { }
        public void CopyFrom(IConnection value) { }

        public void Dispose() { }


        public IDatabase GetDatabase(int db, bool @readonly = false)
        {
            return @readonly ? read.GetDatabase(db) : redis.GetDatabase(db);
        }

    }
}

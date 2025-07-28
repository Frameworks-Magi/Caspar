using Caspar;
using Caspar.Container;
using Microsoft.Azure.Cosmos.Serialization.HybridRow.Schemas;
using Mysqlx.Notice;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using static Caspar.Api;
using Confluent.Kafka;
using Google.Protobuf;
using System.Text;
using Amazon.DynamoDBv2.Model;

namespace Caspar.Protocol
{
    public interface IKafka
    {
        public string ServerId { get; set; }

        void Delegate<T>(string topic, string relation, long from, long to, T msg) where T : global::Google.Protobuf.IMessage<T>;
        void Delegate(string topic, string relation, long from, long to, int code, byte[] msg);
        void Response(string topic, long responsable, int code, byte[] msg);
        Task<object> DelegateAsync<T>(string topic, string relation, long from, long to, T msg) where T : global::Google.Protobuf.IMessage<T>;

        public class Notifier : global::Caspar.INotifier
        {
            public virtual void Response<T>(T msg)
            {
                if (Responsable == 0) { return; }
                var proto = msg as global::Google.Protobuf.IMessage;
                var buffer = proto.ToByteArray();

                Delegator.Response(ServerId.ToString(), Responsable, Caspar.Id<T>.Value, buffer);
            }

            public virtual void Notify<T>(T msg)
            {
                var proto = msg as global::Google.Protobuf.IMessage;
                var buffer = proto.ToByteArray();

                var relation = Relation.Split(':');
                var from = long.Parse(relation[0]);
                var to = long.Parse(relation[1]);
                Delegator.Delegate(ServerId.ToString(), $"{to}:{from}", To, From, Caspar.Id<T>.Value, buffer);
            }

            // public void Response(int code, Stream stream)
            // {
            //     if (Message.Responsable == 0) { return; }

            //     Delegator.Delegate(Message.ServerId, Message.From, Message.To, new KafkaMessage
            //     {
            //         Message = stream.ToArray(),
            //         From = Message.To,
            //         To = Message.From,
            //         Code = code,
            //     });

            //     // Delegator.Delegate(To, From, new KafkaMessage
            //     // {
            //     //     Message = stream.ToArray(),
            //     //     From = To,
            //     //     To = From,
            //     //     Code = code,
            //     //     Sequence = 0,
            //     //     Responsable = Responsible,
            //     //     ResponseTopic = ResponseTopic
            //     // });
            // }

            public IKafka Delegator;
            public byte[] Message;
            public string Relation;
            public long From { get; set; }
            public long To { get; set; }
            public long ServerId { get; set; }
            public long Responsable { get; set; }
            public int Code { get; set; }

        }
    }

    public partial class Kafka<D> : IKafka where D : Kafka<D>.IDelegatable, new()
    {
        public static Kafka<D> Singleton { get; set; }

        public delegate D TaskGetterCallback(long uid);
        public TaskGetterCallback GetTask { get; set; }

        public interface IDelegatable
        {
            void OnDelegate(IKafka.Notifier notifier, Confluent.Kafka.ConsumeResult<string, byte[]> result);
        }
        public string ServerId { get; set; } = Api.ServerId;
        private IDelegatable dispatcher;

        public class Null : global::Caspar.INotifier
        {
            public Null(IKafka.Notifier notifier)
            {
            }

            public void Response<T>(T msg)
            {
            }

            public void Notify<T>(T msg)
            {
            }
        }

        public Kafka()
        {
            dispatcher = new D();
            Singleton = this;
        }
        // Kafka 관련 필드들
        private IProducer<string, byte[]> _producer;
        private IConsumer<string, byte[]> _consumer;
        private delegate void ResponseCallback(int code, global::System.IO.MemoryStream stream);
        private delegate void ResponseFallback();
        private ConcurrentDictionary<long, ResponseCallback> waitResponses = new();

        // Kafka 메시지 구조
        public class KafkaMessage
        {
            public long ServerId { get; set; }
            public long From { get; set; }
            public long To { get; set; }
            public int Code { get; set; }
            public long Responsable { get; set; }
            public byte[] Message { get; set; }
        }



        // public static KafkaDelegator<D> Create(string type)
        // {
        //     var d = new KafkaDelegator<D>();
        //     d.ServerId = Api.ServerId;
        //     d.Id = d.UID;
        //     return d;
        // }



        // Kafka 연결 메서드
        public virtual void Connect(string bootstrapServers, string group, List<string> topics)
        {
            // _bootstrapServers = bootstrapServers;

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                Acks = Acks.All,
                EnableIdempotence = true,
                MessageSendMaxRetries = 3,
                RetryBackoffMs = 100
            };

            _producer = new ProducerBuilder<string, byte[]>(producerConfig).Build();

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = group,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true
            };

            _consumer = new ConsumerBuilder<string, byte[]>(consumerConfig).Build();

            if (topics == null || topics.Count == 0)
            {
                topics = new List<string>();
            }

            if (topics.Contains(ServerId) == false)
            {
                topics.Add(ServerId);
            }
            if (topics.Contains($"{Caspar.Api.Config.Deploy}") == false)
            {
                topics.Add($"{Caspar.Api.Config.Deploy}");
            }

            _consumer.Subscribe(topics.ToArray());
            Task.Run(StartConsuming);
        }

        // 메시지 수신 처리
        private void StartConsuming()
        {
            string deploy = Caspar.Api.Config.Deploy;
            while (!IsClosed())
            {
                try
                {
                    var result = _consumer.Consume(1000);
                    if (result != null)
                    {
                        if (result.Topic == deploy)
                        {


                        }
                        else if (result.Topic == ServerId)
                        {
                            var notifier = new IKafka.Notifier();
                            using var reader = new BinaryReader(new MemoryStream(result.Message.Value));
                            notifier.ServerId = reader.ReadInt64();
                            notifier.From = reader.ReadInt64();
                            notifier.To = reader.ReadInt64();
                            notifier.Code = reader.ReadInt32();
                            notifier.Responsable = reader.ReadInt64();
                            notifier.Message = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
                            notifier.Relation = result.Message.Key;
                            notifier.Delegator = this;

                            if (notifier.ServerId == 0 && notifier.Responsable > 0)
                            {
                                OnResponse(notifier.Responsable, notifier.Code, notifier.Message);
                            }
                            else
                            {
                                dispatcher.OnDelegate(notifier, result);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"KafkaDelegator<{typeof(D).FullName}> Consume error: {ex.Message}");
                }
            }
        }

        public virtual void Delegate(string topic, string relation, long from, long to, int code, byte[] buffer)
        {
            using var stream = new MemoryStream(buffer.Length + 8 * 5);
            var bw = new BinaryWriter(stream);
            bw.Write(Api.Idx);
            bw.Write(from);
            bw.Write(to);
            bw.Write(code);
            bw.Write(0);
            bw.Write(buffer);
            bw.Flush();

            var kafkaMessage = new Message<string, byte[]>
            {
                Key = relation,
                Value = stream.ToArray()
            };
            _producer.Produce(topic, kafkaMessage);
        }

        public virtual void Delegate<T>(string topic, string relation, long from, long to, T msg) where T : global::Google.Protobuf.IMessage<T>
        {
            var buffer = msg.ToByteArray();
            var code = Caspar.Id<T>.Value;
            Delegate(topic, relation, from, to, code, buffer);
        }

        public virtual void Response(string topic, long responsable, int code, byte[] msg)
        {
            //   byte[] buffer = new byte[msg.Length + 8 * 5];
            using (var stream = new MemoryStream())
            {
                using (var bw = new BinaryWriter(stream))
                {
                    bw.Write((long)0);
                    bw.Write((long)0);
                    bw.Write((long)0);
                    bw.Write(code);
                    bw.Write(responsable);
                    bw.Write(msg);
                    bw.Flush();
                }
                var kafkaMessage = new Message<string, byte[]>
                {
                    Key = "response",
                    Value = stream.ToArray()
                };
                _producer.Produce(topic, kafkaMessage);
            }
        }

        public virtual void OnResponse(long responsable, int code, byte[] msg)
        {
            waitResponses.TryRemove(responsable, out var callback);
            if (callback != null)
            {
                callback(code, new MemoryStream(msg));
            }
        }

        public async Task<object> DelegateAsync<T>(string topic, string relation, long from, long to, T msg) where T : global::Google.Protobuf.IMessage<T>
        {
            var tcs = new TaskCompletionSource<object>();

            ResponseCallback callback = (code, stream) =>
            {
                var ret = Caspar.Api.Protobuf.Deserialize(code, stream);
                tcs.SetResult(ret);
            };

            var buffer = msg.ToByteArray();
            var responsable = Caspar.Api.UniqueKey;

            // message to byte[]
            using var stream = new MemoryStream(buffer.Length + 8 * 5);
            var bw = new BinaryWriter(stream);
            bw.Write(Api.Idx);
            bw.Write(from);
            bw.Write(to);
            bw.Write(Caspar.Id<T>.Value);
            bw.Write(responsable);
            bw.Write(buffer);
            bw.Flush();

            var kafkaMessage = new Message<string, byte[]>
            {
                Key = relation,
                Value = stream.ToArray()
            };

            if (waitResponses.TryAdd(responsable, callback) == false)
            {
                throw new Exception("Failed to add response handler");
            }
            _producer.Produce(topic, kafkaMessage);
            return await tcs.Task;
        }


        public bool IsClosed()
        {
            return _producer == null || _consumer == null;
        }

        public void Close()
        {
            _producer?.Dispose();
            _consumer?.Dispose();
        }
    }
}

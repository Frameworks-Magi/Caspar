using Caspar;
using Caspar.Container;
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

namespace Caspar.Protocol
{

    public interface IDelegator
    {
        public class Null : IDelegator
        {
            public long UID { get; set; }
            public ushort Port { get; set; }
        }
        public long UID { get; set; }
        public ushort Port { get; set; }

        public void Connect(string ip, ushort port) { }

        public void Close() { }
        public void Delegate(long from, long to, global::Caspar.ISerializable serializable) { }
        public void Delegate<T, R>(global::Caspar.Layer.Frame task, long to, T msg, AsyncCallback<R> callback, Action fallback = null)
           where T : global::Google.Protobuf.IMessage<T>
           where R : global::Google.Protobuf.IMessage<R>
        { }
        public void Delegate<T>(global::Caspar.Layer.Frame task, long to, T msg, AsyncCallback<T> callback, Action fallback = null)
           where T : global::Google.Protobuf.IMessage<T>
        { }
        public async Task<T> DelegateAsync<T>(long from, long to, T msg)
           where T : global::Google.Protobuf.IMessage<T>
        { await Task.CompletedTask; return default(T); }
        public async Task<T> DelegateAsync<T>(T msg)
           where T : global::Google.Protobuf.IMessage<T>
        { await Task.CompletedTask; return default(T); }
        public async Task<MemoryStream> DelegateAsync(int id, MemoryStream msg) { await Task.CompletedTask; return null; }
        public void Delegate<T>(long from, long to, T msg)
            where T : global::Google.Protobuf.IMessage<T>
        { }
        public void Delegate<T>(long to, T msg)
            where T : global::Google.Protobuf.IMessage<T>
        { }
        public void Delegate<T>(T msg)
            where T : global::Google.Protobuf.IMessage<T>
        { }
        public void Delegate(long from, long to, int code, MemoryStream stream) { }
        //void Attach(Caspar.IDelegatable from);
        //void Detach(Caspar.IDelegatable from);
        public long GetSequence() { return 0; }

        public EndPoint RemoteEndPoint
        {
            get
            {
                return null;
            }
        }

        public bool IsClosed() { return true; }
    }



    public partial class Delegator<D> : global::Caspar.Scheduler, IDelegator where D : Delegator<D>.IDelegatable, new()
    {
        public static Delegator<D> Singleton { get; set; }

        public delegate D TaskGetterCallback(long uid);
        public TaskGetterCallback GetTask { get; set; }
        public interface IDelegatable
        {
            void OnAccept(IDelegator delegator, MemoryStream stream) { }
            void OnConnect(IDelegator delegator) { }
            void OnDisconnect(IDelegator delegator) { }
            void OnDelegate(Notifier notifier, int code, MemoryStream stream);
        }

        public class Null : global::Caspar.INotifier
        {
            public Null(global::Caspar.Protocol.Delegator<D>.Notifier notifier)
            {
                Notifier = notifier;
            }
            public void Response<T>(T msg)
            {
                Notifier.Response(msg);
            }
            public void Notify<T>(T msg)
            {
                Notifier.Notify(msg);
            }
            protected global::Caspar.Protocol.Delegator<D>.Notifier Notifier;
            public long To => Notifier.To;
            public long From => Notifier.From;
        }

        public Delegator()
        //: base(Caspar.Api.Singleton<Caspar.Api.ScheduleLayer>.Instance)
        {

        }

        public EndPoint RemoteEndPoint
        {
            get
            {
                return Protocol.RemoteEndPoint;
            }
        }

        protected void heartBeat()
        {
            if (Protocol.IsEstablish() == true)
            {
                var size = sizeof(int) + sizeof(long) + sizeof(long) + sizeof(long) +
                       sizeof(int) + sizeof(long);

                var header = new MemoryStream(size);
                BinaryWriter binaryWriter = new BinaryWriter(header);
                binaryWriter.Write(size);
                binaryWriter.Write((long)ulong.MinValue); //from
                binaryWriter.Write((long)ulong.MinValue); // to
                binaryWriter.Write((long)ulong.MinValue);           // seq
                binaryWriter.Write((int)1);          // code
                binaryWriter.Write((long)UID);          // res
                binaryWriter.Flush();
                header.Seek(0, SeekOrigin.Begin);
                Protocol.Write(header);
            }
            else
            {
            }
        }

        DateTime lastHeartBeat = DateTime.UtcNow.AddSeconds(10);
        protected override void OnSchedule()
        {

            if (string.IsNullOrEmpty(this.Ip) == false)
            {
                heartBeat();
            }
            else
            {
                //Logger.Info($"string.IsNullOrEmpty(this.Ip) == false");
            }

#if !DEBUG
            if (lastHeartBeat < DateTime.UtcNow)
            {
                if (Caspar.Api.Config.HeartBeat == true)
                {
                    Protocol.Disconnect();
                    return;
                }
            }
#endif

            var now = DateTime.UtcNow.Ticks;
            while (waitTimeout.Count > 0)
            {
                var wait = waitTimeout.FirstOrDefault();
                if (wait.Item1 == 0) { break; }

                if (waitTimeout.TryPeek(out wait) == true)
                {
                    if (wait.Item1 > now) { break; }
                    if (waitResponse.TryRemove(wait.Item2, out var responder) == true)
                    {
                        responder.Item2?.Invoke();
                    }
                }

            }

        }

        internal protected override async System.Threading.Tasks.Task OnClose()
        {

            try
            {
                Disconnect();

                if (Self == false)
                {
                    Remove(Id);
                }

                Logger.Info($"OnClose Delegator<{typeof(D).FullName}> From {(global::Caspar.Api.UInt32ToIPAddress((uint)UID))} Self {Self}");
                await base.OnClose();
            }
            catch
            {

            }

        }

        public bool IsClosed()
        {
            return Protocol.IsClosed();
        }

        protected OldTcp Protocol = new OldTcp() { UseCompress = true };
        new public long UID { get { return base.UID; } set { base.UID = value; } }
        private static ConcurrentDictionary<long, Delegator<D>> delegators = new ConcurrentDictionary<long, Delegator<D>>();
        public long Id { get; private set; }

        public static Delegator<D> Create()
        {
            var d = new Delegator<D>();
            d.Id = d.UID;
            return d;
        }

        public static Delegator<D> Create(long id, bool self = false)
        {
            if (delegators.ContainsKey(id) == true)
            {
                delegators.TryGetValue(id, out Delegator<D> d);
                return d;
            }
            else
            {
                var d = new Delegator<D>();
                d.Id = id;
                d.Self = self;
                delegators.TryAdd(id, d);
                return d;
            }
        }

        public static ICollection<long> Keys
        {
            get
            {
                return delegators.Keys;
            }
        }

        public static Delegator<D>[] Values
        {
            get
            {
                return delegators.Values.ToArray();
            }
        }


        public static int Count
        {
            get
            {
                return delegators.Count;
            }
        }

        public static Delegator<D> Get(long id)
        {
            delegators.TryGetValue(id, out Delegator<D> d);
            return d;
        }

        public static Delegator<D> Add(long id, Delegator<D> delegator)
        {
            delegators.TryAdd(id, delegator);
            return delegator;
        }

        public static Delegator<D> Remove(long id)
        {
            delegators.TryRemove(id, out Delegator<D> delegator);
            return delegator;
        }

        private delegate void ResponseCallback(global::System.IO.MemoryStream stream);
        private delegate void ResponseFallback();
        private ConcurrentDictionary<long, (ResponseCallback, ResponseFallback)> waitResponse = new ConcurrentDictionary<long, (ResponseCallback, ResponseFallback)>();
        private ConcurrentQueue<(long, long)> waitTimeout = new ConcurrentQueue<(long, long)>();

        public class Serializer : global::Caspar.ISerializable
        {
            public void Serialize(Stream output)
            {
                output.Write(BitConverter.GetBytes(Length), 0, 4);
                output.Write(BitConverter.GetBytes(From), 0, 8);
                output.Write(BitConverter.GetBytes(To), 0, 8);
                output.Write(BitConverter.GetBytes(Sequence), 0, 8);
                output.Write(BitConverter.GetBytes(Code), 0, 4);
                output.Write(BitConverter.GetBytes(Responsable), 0, 8);
                Message.CopyTo(output);
            }
            public int Length
            {
                get
                {
                    if (length == 0)
                    {
                        length = (int)Message.Length + sizeof(int) + sizeof(long) + sizeof(long) + sizeof(int) + sizeof(long) + sizeof(long);
                    }
                    return length;
                }
            }

            protected int length { get; set; }
            public Stream Message;
            public int Code;
            public long From;
            public long To;
            public long Sequence;
            public long Responsable;
        }

        public class Serializer<T> : global::Caspar.ISerializable
        {
            public void Serialize(Stream output)
            {

                var proto = Message as global::Google.Protobuf.IMessage;
                output.Write(BitConverter.GetBytes(Length), 0, 4);
                output.Write(BitConverter.GetBytes(From), 0, 8);
                output.Write(BitConverter.GetBytes(To), 0, 8);
                output.Write(BitConverter.GetBytes(Sequence), 0, 8);
                output.Write(BitConverter.GetBytes(global::Caspar.Id<T>.Value), 0, 4);
                output.Write(BitConverter.GetBytes(Responsable), 0, 8);

                proto.Serialize(output, true);

            }

            public int Length
            {
                get
                {
                    if (length == 0)
                    {
                        var proto = Message as global::Google.Protobuf.IMessage;
                        length = proto.CalculateSize() + sizeof(int) + sizeof(long) + sizeof(long) + sizeof(int) + sizeof(long) + sizeof(long);
                    }
                    return length;
                }
            }
            protected int length { get; set; }
            public T Message;
            public long From;
            public long To;
            public long Sequence;
            public long Responsable;
            public int Lock;
        }

        public void Disconnect()
        {
            //lock (this)
            {
                Protocol.Disconnect();
            }
        }

        public class Notifier : global::Caspar.INotifier
        {
            public virtual void Response<T>(T msg)
            {
                if (Responsible == 0)
                {
                    Logger.Error($"Delegator Notifier Response But Responsible == {Responsible}, {typeof(T)}");
                    return;
                }
                //     lock (Delegator)
                {
                    Delegator.Delegate(To, From, new Serializer<T>() { Message = msg, From = To, To = From, Sequence = 0, Responsable = Responsible } as ISerializable);
                }

            }

            public virtual void Notify<T>(T msg)
            {
                //       lock (Delegator)
                {
                    Delegator.Delegate(To, From, new Serializer<T>() { Message = msg, From = To, To = From, Sequence = Delegator.GetSequence() } as ISerializable);
                }
            }

            public void Response(int code, Stream stream)
            {
                if (Responsible == 0) { return; }
                //        lock (Delegator)
                {
                    Delegator.Delegate(To, From, new Serializer() { Message = stream, From = To, To = From, Code = code, Sequence = 0, Responsable = Responsible });
                }
            }


            public IDelegator Delegator;
            public long UID;
            internal protected long Responsible;
            public long From;
            public long To;
        }

        //public MemoryStream ConnectStream { get; set; } = new MemoryStream();
        protected void onConnect(bool ret)
        {
            if (ret == true)
            {
                Logger.Info($"Delegator<{typeof(D).FullName}> Connected To {Protocol.IP}");
                lastHeartBeat = DateTime.UtcNow.AddSeconds(15);
                //       lock (this)
                {
                    var tokens = Ip.Split(',');

                    if (tokens.Length > 1)
                    {
                        var header = new MemoryStream(10);
                        BinaryWriter binaryWriter = new BinaryWriter(header);
                        binaryWriter.Write((int)10);
                        binaryWriter.Write((uint)global::Caspar.Api.IPAddressToUInt32(tokens[1]));
                        binaryWriter.Write(Port);
                        binaryWriter.Flush();
                        header.Seek(0, SeekOrigin.Begin);
                        Protocol.Write(header);
                    }

                    {
                        var size = sizeof(int) + sizeof(long) + sizeof(long) + sizeof(long) +
                        sizeof(uint) + sizeof(long) + sizeof(bool);

                        var header = new MemoryStream(size);
                        BinaryWriter binaryWriter = new BinaryWriter(header);
                        binaryWriter.Write(size);
                        binaryWriter.Write((long)ulong.MinValue); //from
                        binaryWriter.Write((long)ulong.MinValue); // to
                        binaryWriter.Write(recvSequence);           // seq
                        binaryWriter.Write(uint.MinValue);          // code
                        binaryWriter.Write((long)UID);          // res
                        binaryWriter.Write(Self);          // res
                                                           //binaryWriter.Write((int)ConnectStream.Length);
                                                           //binaryWriter.Write(ConnectStream.GetBuffer(), 0, (int)ConnectStream.Length);
                                                           //binaryWriter.Write((int)bytes.Length);
                                                           //binaryWriter.Write(bytes, 0, bytes.Length);
                                                           //binaryWriter.Write(port);

                        binaryWriter.Flush();
                        header.Seek(0, SeekOrigin.Begin);
                        Protocol.Write(header);

                        try
                        {
                            Singleton<D>.Instance.OnConnect(this);
                        }
                        catch (Exception)
                        {

                        }
                    }

                }
                Run(3000);
            }
            else
            {
                Logger.Error($"Delegator<{typeof(D).FullName}> Connect Fail {Protocol.IP}:{Protocol.Port}");
            }
        }

        public async Task<T> DelegateAsync<T>(long from, long to, T msg)
            where T : global::Google.Protobuf.IMessage<T>
        {

            global::System.Threading.Tasks.TaskCompletionSource<T> TCS = new TaskCompletionSource<T>();

            //       lock (this)
            {
                ResponseCallback responder = (cis) =>
                {
                    T ret = Api.ProtobufParser<T>.Parser.ParseFrom(cis);
                    TCS.SetResult(ret);
                    //task.PostMessage(() => { callback(ret); });
                };


                ResponseFallback responseFallback = null;
                responseFallback = () => { TCS.SetException(new Exception("FallBack")); };
                //responseFallback = () => { TCS.SetResult(new T()); };


                var timeout = DateTime.UtcNow.AddSeconds(30).Ticks;
                var serialzable = new Serializer<T>();
                serialzable.From = from;
                serialzable.To = to;
                serialzable.Message = msg;
                serialzable.Sequence = GetSequence();
                serialzable.Responsable = GetSequence();
                serialzable.Lock = TCS.Task.Id;
                if (waitResponse.TryAdd(serialzable.Responsable, (responder, responseFallback)) == false)
                {
                    waitTimeout.Enqueue((timeout, serialzable.Responsable));
                    Logger.Error($"{this.GetType()} waitResponse Add Fail. msg : {msg.GetType()}, json : {msg.ToJson()}");
                    //responseFallback?.Invoke();
                    throw new Exception();
                    //return default(T);
                }

                if (Protocol.Write(serialzable) == false)
                {
                    Logger.Error($"{this.GetType()} Write Fail. msg : {msg.GetType()}, json : {msg.ToJson()}");
                    waitResponse.TryRemove(serialzable.Responsable, out (ResponseCallback, ResponseFallback) failed);
                    //failed.Item2?.Invoke();
                    throw new Exception();
                }


            }
            return await TCS.Task;
        }

        public async Task<T> DelegateAsync<T>(T msg)
            where T : global::Google.Protobuf.IMessage<T>
        {
            return await DelegateAsync(UID, UID, msg);
        }

        public async Task<MemoryStream> DelegateAsync(int code, MemoryStream msg)
        {

            global::System.Threading.Tasks.TaskCompletionSource<MemoryStream> TCS = new TaskCompletionSource<MemoryStream>();
            //    lock (this)
            {
                ResponseCallback responder = (cis) =>
                {
                    TCS.SetResult(cis);
                };


                ResponseFallback responseFallback = null;
                responseFallback = () => { TCS.SetException(new Exception("FallBack")); };


                var timeout = DateTime.UtcNow.AddSeconds(30).Ticks;
                var serialzable = new Serializer();
                serialzable.From = UID;
                serialzable.To = UID;
                serialzable.Message = msg;
                serialzable.Sequence = GetSequence();
                serialzable.Responsable = GetSequence();
                serialzable.Code = code;

                if (waitResponse.TryAdd(serialzable.Responsable, (responder, responseFallback)) == false)
                {
                    waitTimeout.Enqueue((timeout, serialzable.Responsable));
                    Logger.Error($"{this.GetType()} waitResponse Add Fail.");
                    //responseFallback?.Invoke();
                    throw new Exception();
                    //return default(T);
                }

                if (Protocol.Write(serialzable) == false)
                {
                    Logger.Error($"{this.GetType()} Write Fail.");
                    waitResponse.TryRemove(serialzable.Responsable, out (ResponseCallback, ResponseFallback) failed);
                    //failed.Item2?.Invoke();
                    throw new Exception();
                }


            }
            return await TCS.Task;
        }



        public void Delegate<T>(global::Caspar.Layer.Frame task, long to, T msg, global::Caspar.AsyncCallback<T> callback, Action fallback = null)
            where T : global::Google.Protobuf.IMessage<T>
        {

            //      lock (this)
            {
                ResponseCallback responder = (cis) =>
                {
                    T ret = Api.ProtobufParser<T>.Parser.ParseFrom(cis);
                    task.PostMessage(() => { callback(ret); });
                };


                ResponseFallback responseFallback = null;
                if (fallback != null)
                {
                    responseFallback = () => { task.PostMessage(fallback); };
                }

                var timeout = DateTime.UtcNow.AddSeconds(30).Ticks;
                var serialzable = new Serializer<T>();
                serialzable.From = task.UID;
                serialzable.To = to;
                serialzable.Message = msg;
                serialzable.Sequence = GetSequence();
                serialzable.Responsable = GetSequence();
                if (waitResponse.TryAdd(serialzable.Responsable, (responder, responseFallback)) == false)
                {
                    waitTimeout.Enqueue((timeout, serialzable.Responsable));

                    Logger.Error($"{this.GetType()} waitResponse Add Fail. msg : {msg.GetType()}, json : {msg.ToJson()}");
                    responseFallback?.Invoke();
                    return;
                }

                if (Protocol.Write(serialzable) == false)
                {

                    Logger.Error($"{this.GetType()} Write Fail. msg : {msg.GetType()}, json : {msg.ToJson()}");
                    waitResponse.TryRemove(serialzable.Responsable, out (ResponseCallback, ResponseFallback) failed);
                    failed.Item2?.Invoke();
                    return;
                }
            }
        }

        protected long sequence = 0;
        public long GetSequence()
        {
            {
                return Interlocked.Increment(ref sequence);
            }
        }

        public void Delegate<T, R>(global::Caspar.Layer.Frame task, long to, T msg, global::Caspar.AsyncCallback<R> callback, Action fallback = null)
            where T : global::Google.Protobuf.IMessage<T>
            where R : global::Google.Protobuf.IMessage<R>
        {

            //       lock (this)
            {
                ResponseCallback responder = (cis) =>
                {
                    var ret = Api.ProtobufParser<R>.Parser.ParseFrom(cis);
                    task.PostMessage(() => { callback(ret); });
                };
                ResponseFallback responseFallback = null;

                if (fallback != null)
                {
                    responseFallback = () => { task.PostMessage(fallback); };
                }


                var serialzable = new Serializer<T>();
                serialzable.From = task.UID;
                serialzable.To = to;
                serialzable.Message = msg;
                serialzable.Sequence = GetSequence();
                serialzable.Responsable = GetSequence();
                var timeout = DateTime.UtcNow.AddSeconds(30).Ticks;

                if (waitResponse.TryAdd(serialzable.Responsable, (responder, responseFallback)) == false)
                {
                    waitTimeout.Enqueue((timeout, serialzable.Responsable));
                    Logger.Error($"{this.GetType()} waitResponse Add Fail. msg : {msg.GetType()}, json : {msg.ToJson()}");
                    responseFallback?.Invoke();
                    return;
                }

                if (Protocol.Write(serialzable) == false)
                {

                    Logger.Error($"{this.GetType()} Write Fail. msg : {msg.GetType()}, json : {msg.ToJson()}");
                    waitResponse.TryRemove(serialzable.Responsable, out (ResponseCallback, ResponseFallback) failed);
                    failed.Item2?.Invoke();
                    return;
                }

            }

        }

        public void Delegate(long from, long to, int code, MemoryStream stream, global::Caspar.AsyncCallback<Stream> callback, global::Caspar.AsyncCallback fallback = null)
        {
            //        lock (this)
            {

                ResponseCallback responder = (cis) =>
                {
                    callback(cis);
                };
                ResponseFallback responseFallback = () => { fallback?.Invoke(); };



                var header = new MemoryStream(sizeof(int) + sizeof(long) + +sizeof(long) + sizeof(long) + sizeof(int) + sizeof(long));
                BinaryWriter binaryWriter = new BinaryWriter(header);
                binaryWriter.Write((int)stream.Length + sizeof(int) + sizeof(long) + sizeof(long) + sizeof(long) + sizeof(int) + sizeof(long));
                binaryWriter.Write(from);
                binaryWriter.Write(to);
                binaryWriter.Write(GetSequence());
                binaryWriter.Write(code);

                var response = GetSequence();

                binaryWriter.Write((long)response);
                var timeout = DateTime.UtcNow.AddSeconds(30).Ticks;

                waitResponse.TryAdd(response, (responder, responseFallback));
                if (Protocol.Write(header, stream) == false)
                {
                    waitResponse.TryRemove(response, out (ResponseCallback, ResponseFallback) failed);
                    failed.Item2?.Invoke();
                }

            }
        }

        public virtual void Delegate<T>(long from, long to, T msg)
            where T : global::Google.Protobuf.IMessage<T>
        {
            //lock (this)
            {
                var serialzable = new Serializer<T>();
                serialzable.From = from;
                serialzable.To = to;
                serialzable.Message = msg;
                serialzable.Sequence = GetSequence();
                Protocol.Write(serialzable);
            }
        }
        public virtual void Delegate<T>(long to, T msg)
            where T : global::Google.Protobuf.IMessage<T>
        {
            Delegate<T>(UID, to, msg);
        }
        public virtual void Delegate<T>(T msg)
            where T : global::Google.Protobuf.IMessage<T>
        {
            Delegate<T>(UID, UID, msg);
        }

        public void Delegate(long from, long to, int code, MemoryStream stream)
        {
            //       lock (this)
            {
                var header = new MemoryStream(sizeof(int) + sizeof(long) + +sizeof(long) + sizeof(long) + sizeof(int) + sizeof(long));
                BinaryWriter binaryWriter = new BinaryWriter(header);
                binaryWriter.Write((int)stream.Length + sizeof(int) + sizeof(long) + sizeof(long) + sizeof(long) + sizeof(int) + sizeof(long));
                binaryWriter.Write(from);
                binaryWriter.Write(to);
                binaryWriter.Write(GetSequence());
                binaryWriter.Write(code);
                binaryWriter.Write((long)ulong.MinValue);
                Protocol.Write(header, stream);
            }
        }

        public void Delegate(long from, long to, global::Caspar.ISerializable serializable)
        {
            //       lock (this)
            {
                Protocol.Write(serializable);
            }

        }

        public static void Event(long delegatable, long to, int code, MemoryStream stream)
        {
            //        lock (delegators)
            {
                var buffer = stream.ToArray();
                foreach (var e in delegators.Values)
                {
                    e.Delegate(delegatable, to, code, new MemoryStream(buffer));
                }
            }

        }


        virtual public void OnDelegate(long seq, long res, long from, long to, int code, MemoryStream stream)
        {
            if (seq == 0)
            {
                if (waitResponse.TryRemove(res, out (ResponseCallback, ResponseFallback) responder) == true)
                {
                    responder.Item1?.Invoke(stream);
                    if (code == int.MaxValue || code == 0)
                    {
                        responder.Item2?.Invoke();
                    }
                }
                else
                {
                    Logger.Error($"Delegator can't find responser Type : {this.GetType()}, Code : {code}");
                }
                return;
            }

            var notifier = new Notifier();
            notifier.Delegator = this;
            notifier.UID = UID;
            notifier.From = from;
            notifier.To = to;
            notifier.Responsible = res;
            global::Caspar.Layer.FromDelegateUID.Value = from;
            Singleton<D>.Instance.OnDelegate(notifier, code, stream);

        }

        virtual protected void onAccept(bool ret)
        {
            if (ret == true)
            {
                Logger.Info($"Delegator<{typeof(D).FullName}> Accepted {Protocol.RemoteEndPoint.GetIp()}");
                recvSequence = 0;
                lastHeartBeat = DateTime.UtcNow.AddSeconds(15);
                Protocol.RecvBufferSize = 1024 * 1000 * 10;
                Run(3000);
            }
        }

        virtual public void Connect(string ip, ushort port)
        {
            if (this.Ip.IsNullOrEmpty() == false && this.Ip != ip && this.Port != port)
            {
                Logger.Info($"Delegator diff ip port {this.GetType().FullName}");
                Disconnect();
                this.Ip = ip;
                this.Port = port;
                return;
            }

            if (Protocol.IsClosed() == false) { return; }

            if (this.Ip.IsNullOrEmpty() == true)
            {
                Logger.Info($"First Time Try Delegator Connect To {ip}:{port} {this.GetType().FullName}");
                Protocol.OnConnect = onConnect;
                Protocol.OnRead = onRead;
                Protocol.OnDisconnect = onDisconnect;
                Protocol.RecvBufferSize = 1024 * 1000 * 10;
            }

            this.Ip = ip;
            this.Port = port;


            Logger.Info($"Try Delegator Connect To {Ip}:{Port} {this.GetType().FullName} - Id:[{Caspar.Api.PublicIp}]");
            Protocol.Connect(ip, port);

        }

        public string Ip { get; set; } = string.Empty;
        protected bool Self { get; set; } = false;
        public ushort Port { get; set; } = 0;
        public void Accept(ushort port)
        {

            Protocol.OnRead = onRead;
            Protocol.OnDisconnect = onDisconnect;
            Protocol.OnAccept = onAccept;
            Protocol.Accept(port);

        }

        public static void Listen(ushort port)
        {
            global::Caspar.Api.Listen(port, () =>
            {
                new global::Caspar.Protocol.Delegator<D>().Accept(port);
            });
        }

        public new void Close()
        {
            Remove(Id);
            base.Close();
        }


        virtual protected void onDisconnect()
        {

            if (string.IsNullOrEmpty(Ip) == true)
            {
                Logger.Info($"Server Delegator<{typeof(D).FullName}> Disconnected. UID : {UID} -> {(global::Caspar.Api.UInt32ToIPAddress((uint)(UID >> 32)))}, {(global::Caspar.Api.UInt32ToIPAddress((uint)UID))} Self {Self}");
            }
            else
            {
                Logger.Info($"Client Delegator<{typeof(D).FullName}> Disconnected From {Ip} Self {Self} - Id:[{Caspar.Api.PublicIp}]");
            }

            var waits = waitResponse.Values.ToArray();
            waitResponse.Clear();

            foreach (var e in waits)
            {
                e.Item2?.Invoke();
            }

            if (string.IsNullOrEmpty(Ip) == false)
            {
                if (IsClose() == true) { return; }
                Task.Run(async () =>
                {
                    if (global::Caspar.Api.IsOpen == false) return;
                    await Task.Delay(10000);
                    Connect(this.Ip, Port);
                });
            }
            else
            {
                Close();
            }

            try
            {

                Singleton<D>.Instance.OnDisconnect(this);
            }
            catch
            {

            }


        }


        protected long recvSequence = 0;
        private int onRead(MemoryStream transferred)
        {
            int offset = 0;
            byte[] buffer = transferred.GetBuffer();
            while ((transferred.Length - offset) > sizeof(int))
            {

                int size = BitConverter.ToInt32(buffer, offset);

                if (size < 1)
                {
                    Logger.Error($"size < 1 ,{this.GetType()}");
                    transferred.Seek(transferred.Length, SeekOrigin.Begin);
                    Protocol.Disconnect();
                    return 0;
                }

                if (size > transferred.Length - offset)
                {
                    break;
                }

                long from = BitConverter.ToInt64(buffer, offset + 4);
                long to = BitConverter.ToInt64(buffer, offset + 12);
                long seq = BitConverter.ToInt64(buffer, offset + 20);
                int code = BitConverter.ToInt32(buffer, offset + 28);
                long res = BitConverter.ToInt64(buffer, offset + 32);

                if (code == 0)
                {
                    bool self = BitConverter.ToBoolean(buffer, offset + 40);
                    {

                        if (self == false)
                        {
                            UID = res;
                            delegators.AddOrUpdate(UID, this);
                        }
                        else
                        {
                            Self = true;
                            Logger.Debug($"Self Delegator<{typeof(D).FullName}> Accepted {Protocol.RemoteEndPoint.GetIp()}");
                        }
                    }
                }
                else if (code == 1)
                {
                    lastHeartBeat = DateTime.UtcNow.AddSeconds(15);

                    // 서버가 핑에 응답해준다.
                    if (string.IsNullOrEmpty(Ip) == true)
                    {
                        heartBeat();
                    }
                }
                else
                {
                    //if (seq > recvSequence)
                    {
                        recvSequence = seq;
                        try
                        {
                            OnDelegate(seq, res, from, to, code, new MemoryStream(buffer, offset + 40, size - 40, true, true));
                        }
                        catch (Exception e)
                        {
                            Logger.Error($"Delegator {this.GetType()} OnDelegate Exception : " + e);
                        }
                    }
                    //else
                    //{
                    //    Logger.Error($"Delegator {this.GetType()} OnDelegate Sequence pass : {seq} > {recvSequence}, code : {code}");
                    //}
                }
                offset += size;
            }

            transferred.Seek(offset, SeekOrigin.Begin);
            return 0;
        }

        public global::Caspar.Layer.Frame Handler { get; set; }

    }
}

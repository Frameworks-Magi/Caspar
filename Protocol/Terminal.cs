using Caspar;
using Caspar.Container;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static Caspar.Api;

namespace Caspar.Protocol
{
    public class Terminal : global::Caspar.Scheduler, global::Caspar.INotifier
    {
        public class Message
        {
            public enum EType
            {
                Ping = 0,
                Command,
                Notify,
                Error,
                Complete,
            };
            public EType Type { get; set; }
            public string Command { get; set; }
            public bool NewLine { get; set; } = true;
            public long Address { get; set; }
            public long UID { get; set; }

        }

        public class DelegateNotifier : global::Caspar.INotifier
        {
            public DelegateNotifier(long uid)
            {
                FromUID = uid;
                ToUID = global::Caspar.Api.UniqueKey;
                HeartBeat = DateTime.UtcNow.AddSeconds(15);
                delegateNotifiers.TryAdd(ToUID, this);
                Terminal.ClearDelegateNotifier();
            }
            public void Response<T>(T msg)
            {
                HeartBeat = DateTime.UtcNow.AddSeconds(15);
                (msg as Message).UID = FromUID;
                From?.Response(msg);
                Terminal.ClearDelegateNotifier();

            }
            public void Notify<T>(T msg)
            {
                HeartBeat = DateTime.UtcNow.AddSeconds(15);
                (msg as Message).UID = ToUID;
                To?.Notify(msg);
                Terminal.ClearDelegateNotifier();
            }
            public long FromUID { get; set; }
            public long ToUID { get; set; }
            public global::Caspar.INotifier To { get; set; }
            public global::Caspar.INotifier From { get; set; }
            public DateTime HeartBeat { get; set; }
        }
        public class ConsoleNotifier : global::Caspar.INotifier
        {
            public static ConsoleNotifier Instance
            {
                get
                {
                    return Singleton<ConsoleNotifier>.Instance;
                }
            }
            public void Response<T>(T msg)
            {
                var message = msg as Terminal.Message;
                if (message.NewLine == true)
                {
                    Console.WriteLine(message.Command);
                }
                else
                {
                    Console.Write(message.Command);
                }

            }
            public void Notify<T>(T msg)
            {
                var message = msg as Terminal.Message;
                if (message.NewLine == true)
                {
                    Console.WriteLine(message.Command);
                }
                else
                {
                    Console.Write(message.Command);
                }
            }
        }
        public class DefaultNotifier : global::Caspar.INotifier
        {
            public void Response<T>(T msg)
            {
                var message = msg as Terminal.Message;
                message.UID = UID;
                if (message.Type == Message.EType.Command)
                {
                    Console.WriteLine($"DefaultNotifier Response Command. Not Allow Command in Response. '{message.Command}'");
                    message.Type = Message.EType.Notify;
                }
                From.Response(msg);
            }
            public void Notify<T>(T msg)
            {
                var message = msg as Terminal.Message;
                message.UID = UID;
                From.Notify(msg);
            }
            public long UID { get; set; }
            public global::Caspar.INotifier From { get; set; }
        }

        public class StringNotifier : global::Caspar.INotifier
        {
            public string Buffer { get; set; }

            public void Notify<T>(T msg)
            {
                var message = msg as Terminal.Message;
                Buffer += message.Command;
            }

            public void Response<T>(T msg)
            {
                var message = msg as Terminal.Message;
                Buffer += message.Command;
            }
        }

        public static void ClearDelegateNotifier()
        {
            foreach (var e in delegateNotifiers)
            {
                if (e.Value.HeartBeat < DateTime.UtcNow)
                {
                    //             delegateNotifiers.TryRemove(e.Key, out DelegateNotifier output);
                }
            }
        }
        internal static ConcurrentDictionary<long, DelegateNotifier> delegateNotifiers = new ConcurrentDictionary<long, DelegateNotifier>();

        //public static async Task<bool> DelegateCommand(Caspar.INotifier, Message msg)
        //{

        //    return true;
        //}

        Protocol.OldTcp protocol = new Protocol.OldTcp();
        public void Response<T>(T msg)
        {
            Message data = msg as Message;
            if (data == null) { return; }
            var stream = new MemoryStream(1024);

            using (BinaryWriter bw = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                byte[] bytes = data.Command.ToBytes();
                bw.Write((ushort)(sizeof(ushort) + sizeof(int) + sizeof(bool) + sizeof(long) + sizeof(long) + bytes.Length));
                bw.Write((int)data.Type);
                bw.Write(data.NewLine);
                bw.Write(data.UID);
                bw.Write(data.Address);
                bw.Write(bytes, 0, bytes.Length);
            }

            protocol.Write(stream);
        }
        public void Notify<T>(T msg)
        {
            Message data = msg as Message;
            if (data == null) { return; }
            var stream = new MemoryStream(1024);
            using (BinaryWriter bw = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                byte[] bytes = data.Command.ToBytes();
                bw.Write((ushort)(sizeof(ushort) + sizeof(int) + sizeof(bool) + sizeof(long) + sizeof(long) + bytes.Length));
                bw.Write((int)data.Type);
                bw.Write(data.NewLine);
                bw.Write(data.UID);
                bw.Write(data.Address);
                bw.Write(bytes, 0, bytes.Length);
            }
            protocol.Write(stream);

        }



        public void Disconnect() => protocol?.Disconnect();

        protected override void OnSchedule()
        {
            if (protocol.IsClosed()) { return; }

            // ping
            if (string.IsNullOrEmpty(IP) == true) { return; }
            var stream = new MemoryStream(1024);


            using (BinaryWriter bw = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                byte[] bytes = "ping".ToBytes();
                bw.Write((ushort)(sizeof(ushort) + sizeof(int) + sizeof(bool) + bytes.Length));
                bw.Write((int)0);
                bw.Write(true);
                bw.Write(bytes, 0, bytes.Length);
            }

            protocol.Write(stream);

        }

        public EndPoint RemoteEndPoint => protocol.RemoteEndPoint;

        public static Terminal Get(string name)
        {
            return Singleton<ConcurrentDictionary<string, Terminal>>.Instance.Get(name);
        }

        private static ConcurrentDictionary<long, Terminal> clients = new ConcurrentDictionary<long, Terminal>();

        public static Terminal Get(long uid)
        {
            clients.TryGetValue(uid, out Terminal e);
            return e;
        }

        internal void Accept(ushort port)
        {
            protocol.Accept(port);
            protocol.OnAccept = (ret) =>
            {

                if (ret == true)
                {
                    UID = protocol.RemoteEndPoint.ToInt64Address();
                    clients.TryRemove(UID, out Terminal older);
                    clients.TryAdd(UID, this);
                }

            };

            protocol.OnDisconnect = () =>
            {
                clients.TryRemove(UID, out Terminal older);
            };

            if (OnMessage == null)
            {
                OnMessage = async (notifier, msg) =>
                {
                    if (delegateNotifiers.TryGetValue(msg.UID, out DelegateNotifier @delegate) == true)
                    {
                        @delegate?.Response(msg);
                    }
                    else
                    {
                        Singleton<ConsoleNotifier>.Instance.Response(msg);
                    }
                    await System.Threading.Tasks.Task.CompletedTask;
                    return true;
                };
            }
        }

        public void Connect(string name, string ip, ushort port = 5882)
        {
            Name = name;
            Singleton<ConcurrentDictionary<string, Terminal>>.Instance.Add(name.ToLower(), this);
            protocol.Connect(ip, port);
            if (OnMessage == null)
            {
                OnMessage = async (notifier, msg) =>
                {
                    if (delegateNotifiers.TryGetValue(msg.UID, out DelegateNotifier @delegate) == true)
                    {
                        @delegate?.Response(msg);
                    }
                    else
                    {
                        Singleton<ConsoleNotifier>.Instance.Response(msg);
                    }
                    await System.Threading.Tasks.Task.CompletedTask;
                    return true;
                };
            }
            Run(8000);
        }

        public string Name { get; private set; }
        public bool IsConnected => !protocol.IsClosed();

        public Terminal() : base(Singleton<Layer>.Instance)
        {
            protocol.OnConnect = (ret) =>
            {
                if (ret == true)
                {
                    OnConnect?.Invoke();
                    return;
                }



            };

            protocol.OnDisconnect = () =>
            {


                if (protocol.IP != "127.0.0.1")
                {
                    global::Caspar.Api.Logger.Info($"OnDisconnect Terminal Retry {protocol.IP}");
                }

                OnDisconnect?.Invoke();
                if (string.IsNullOrEmpty(protocol.IP)) { return; }


                Task.Run(async () =>
                {
                    var ip = protocol.IP;
                    var port = protocol.Port;
                    await Task.Delay(1000);
                    protocol.Connect(protocol.IP, protocol.Port);
                });

            };
            protocol.OnRead = onRead;
        }

        public string IP => protocol.IP;
        public ushort Port => protocol.Port;

        public delegate void CallbackCompletion();

        public CallbackCompletion OnConnect { get; set; }
        public CallbackCompletion OnDisconnect { get; set; }

        public delegate Task<bool> Callback(global::Caspar.INotifier notifier, Message msg);

        public Callback OnCommand { get; set; }
        public Callback OnMessage { get; set; }


        static readonly int NewLineOffset = sizeof(ushort) + sizeof(int);
        static readonly int UIDOffset = sizeof(ushort) + sizeof(int) + sizeof(bool);
        static readonly int AddressOffset = sizeof(ushort) + sizeof(int) + sizeof(bool) + sizeof(long);
        static readonly int CommandOffset = sizeof(ushort) + sizeof(int) + sizeof(bool) + sizeof(long) + sizeof(long);

        private int onRead(MemoryStream transferred)
        {
            int offset = 0;
            byte[] buffer = transferred.GetBuffer();
            while ((transferred.Length - offset) > sizeof(ushort))
            {

                ushort size = BitConverter.ToUInt16(buffer, offset);
                if (size > transferred.Length - offset)
                {
                    break;
                }


                if (BitConverter.ToInt32(buffer, 2) == 0)
                {
                    offset += size;
                    continue;
                }

                try
                {
                    Message msg = new Message();
                    msg.Type = (Message.EType)BitConverter.ToInt32(buffer, offset + 2);
                    msg.NewLine = BitConverter.ToBoolean(buffer, offset + NewLineOffset);
                    msg.Command = Encoding.UTF8.GetString(buffer, offset + CommandOffset, size - CommandOffset);
                    msg.UID = BitConverter.ToInt64(buffer, offset + UIDOffset);
                    msg.Address = BitConverter.ToInt64(buffer, offset + AddressOffset);

                    var notifier = new global::Caspar.Protocol.Terminal.DefaultNotifier();
                    notifier.UID = msg.UID;
                    notifier.From = this;

                    if (msg.Type == Message.EType.Command)
                    {
                        PostMessage(() => { OnCommand?.Invoke(notifier, msg); });
                    }
                    else
                    {
                        OnMessage?.Invoke(this, msg);
                    }
                }
                catch (Exception e)
                {
                    global::Caspar.Api.Logger.Info(e);
                    protocol.Disconnect();
                }

                offset += size;
            }


            transferred.Seek(offset, SeekOrigin.Begin);
            return 0;
        }

    }
}

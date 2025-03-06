using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Caspar.Protocol
{
    public class Bridge
    {
        public class IPAddress
        {
            public string Ip { get; set; }
            public ushort Port { get; set; }
        }

        public IPAddress Gateway { get; set; }
        private class Tcp : global::Caspar.Protocol.OldTcp
        {
            public Tcp To { get; set; }
            protected override void Defragment(MemoryStream transferred)
            {
                To.Write(new MemoryStream(transferred.ToArray(), true));
                transferred.Seek(0, SeekOrigin.End);
            }

            protected override void flush()
            {
                if (pendings.Count == 0) { return; }
                if (state != (int)EState.Establish) { return; }
                MemoryStream output = new MemoryStream();
                Stream stream = output;


                int length = 0;
                while (pendings.Count > 0 && length < RecvBufferSize)
                {
                    var msg = pendings.Dequeue();
                    switch (msg)
                    {
                        case global::Caspar.ISerializable serializable:
                            length += serializable.Length;
                            serializable.Serialize(stream);
                            break;
                        case MemoryStream ms:
                            try
                            {
                                length += (int)ms.Length;
                                ms.CopyTo(stream);
                                //stream.Write(ms.GetBuffer(), 0, (int)ms.Length);
                            }
                            catch (Exception)
                            {
                                Disconnect();
                            }
                            break;
                        case byte[] array:
                            length += array.Length;
                            stream.Write(array, 0, array.Length);
                            break;
                        default:
                            break;
                    }
                }


                if (output.Length == 2)
                {
                    return;
                }

                sendBuffer = output.ToArray();
                output.Dispose();

                if (sendBuffer == null || sendBuffer.Length == 0)
                {
                    sendBuffer = null;
                    return;
                }

                socket.BeginSend(sendBuffer, 0, (int)sendBuffer.Length, SocketFlags.None, SendComplete, null);
            }

        }
        private Tcp From { get; set; } = new Tcp() { UseCompress = true };

        protected virtual int OnConnect(MemoryStream transferred)
        {

            byte[] buffer = transferred.GetBuffer();
            int size = BitConverter.ToInt32(buffer, 0);
            uint ip = BitConverter.ToUInt32(buffer, 4);
            ushort port = BitConverter.ToUInt16(buffer, 8);

            From.To = new Tcp();
            From.To.To = From;
            From.To.OnDisconnect = () =>
            {

                From.To.To.Disconnect();
                From.To.To = null;

            };

            From.To.Connect(global::Caspar.Api.UInt32ToIPAddress(ip), port);
            transferred.Seek(size, SeekOrigin.Begin);
            return 0;
        }

        public Bridge(ushort listen)
        {
            From.OnDisconnect = () =>
            {

                if (From.To != null)
                {
                    From.To.Disconnect();
                    From.To.To = null;
                    From.To = null;
                }
            };

            From.OnAccept = (ret) =>
            {

                if (ret == true)
                {
                    From.To = new Tcp();
                    From.To.To = From;
                    From.To.OnDisconnect = () =>
                    {
                        From.Disconnect();
                    };
                    From.To.Connect(Gateway.Ip, Gateway.Port);
                }
            };
            From.Accept(listen);
        }

        public static void Run(ushort port, IPAddress gateway)
        {
            global::Caspar.Api.Listen(port, 128, () =>
            {
                var bridge = new Bridge(port) { Gateway = gateway };
            });
        }
    }
}

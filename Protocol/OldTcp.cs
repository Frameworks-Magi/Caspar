using System;
using System.Net.Sockets;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Net;
using System.Collections;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.IO.Compression;
using System.Threading.Tasks;
using Amazon.Runtime.Internal.Util;

namespace Framework.Caspar.Protocol
{
    public class OldTcp
    {
        public Aes aesAlg = null;
        public bool UseCompress { get; set; } = false;
        public delegate int AsyncReadCallback(MemoryStream stream);
        public delegate void AsyncConnectCallback(bool ret);
        public delegate void AsyncAcceptCallback(bool ret);
        public delegate void AsyncDisconnectCallback();

        private AsyncReadCallback onRead = DefaultOnRead;
        private AsyncConnectCallback onConnect = DefaultOnConnect;
        private AsyncAcceptCallback onAccept = DefaultOnAccept;
        private AsyncDisconnectCallback onDisconnect = DefaultOnDisconnect;
        public string IP => ip;
        public ushort Port => port;
        protected string ip { get; set; }
        protected ushort port { get; set; }
        public AsyncReadCallback OnRead
        {
            set { onRead = value; }
            get { return onRead; }
        }
        public AsyncConnectCallback OnConnect
        {
            set { onConnect = value; }
            get { return onConnect; }
        }
        public AsyncAcceptCallback OnAccept
        {
            set { onAccept = value; }
            get { return onAccept; }
        }
        public AsyncDisconnectCallback OnDisconnect
        {
            set { onDisconnect = value; }
            get { return onDisconnect; }
        }
        public int SendBufferSize = 65535;
        public int RecvBufferSize
        {
            get { return recvBufferSize; }
            set
            {
                recvBufferSize = value;
                recvstream = new byte[value];
            }
        }

        private int recvBufferSize = 65535;
        protected byte[] sendBuffer = null;


        protected enum EState
        {
            Idle = 0,
            Connecting = 1,
            Establish = 2,
            Closed = 3,
        }


        protected int state = (int)EState.Idle;
        public bool Accept(ushort port)
        {
            if (socket != null)
            {
                global::Framework.Caspar.Api.Logger.Info("!!!!!!!!!!!!!!! Accept Listen Fail socket == null !!!!!!!!!!!!!!");
                return false;
            }
            if (global::Framework.Caspar.Api.IsOpen == false) return false;

            this.port = port;

            Socket listen = global::Framework.Caspar.Api.Acceptor(port);
            if (listen == null)
            {
                global::Framework.Caspar.Api.Logger.Info("!!!!!!!!!!!!!!! Accept Listen Fail listen == null !!!!!!!!!!!!!!");
                return false;
            }

            try
            {
                listen.BeginAccept(ListenComplete, listen);
                return true;
            }
            catch (Exception e)
            {
                global::Framework.Caspar.Api.Logger.Info("!!!!!!!!!!!!!!! Accept Listen Fail !!!!!!!!!!!!!!\n" + e);
            }
            return false;
        }
        public bool Connect(string ip, ushort port)
        {
            if (global::Framework.Caspar.Api.IsOpen == false) return false;
            try
            {
                lock (this)
                {
                    if (socket != null) return false;
                    this.ip = ip;
                    this.port = port;
                    socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    state = (int)EState.Connecting;
                }
                socket.BeginConnect(ip, port, ConnectComplete, null);
                return true;
            }
            catch (Exception e)
            {
                global::Framework.Caspar.Api.Logger.Debug(e);
                Disconnect();
            }
            return false;
        }

        public bool IsEstablish()
        {
            return state == (int)EState.Establish;
        }

        public virtual bool IsClosed()
        {
            lock (this)
            {
                if (socket != null) { return false; }
                return true;
            }
        }

        public void Disconnect()
        {

            lock (this)
            {
                state = (int)EState.Closed;
                if (socket == null) { return; }
                try
                {
                    socket.Close(0);
                    sendBuffer = null;
                    pendings.Clear();
                    // pendings.Clear();
                }
                catch
                {

                }
                finally
                {

                    socket = null;
                }
            }
            try
            {
                OnDisconnect?.Invoke();
            }
            catch
            {

            }
        }

        protected Queue<object> pendings = new Queue<object>();
        public bool Write(global::Framework.Caspar.ISerializable msg)
        {


            lock (this)
            {
                if (IsClosed()) return false;
                pendings.Enqueue(msg);
                if (sendBuffer != null) { return true; }
                try
                {
                    flush();
                    return true;
                }
                catch (Exception)
                {
                    //Framework.Caspar.Api.Logger.Error(e);
                    //Framework.Caspar.Api.Logger.Verbose($"Ip = {IP}, Port = {Port}");
                }

            }

            Disconnect();
            return false;
        }

        protected virtual void flush()
        {
            if (pendings.Count == 0) { return; }
            if (state != (int)EState.Establish) { return; }
            MemoryStream output = new MemoryStream();
            output.Write(BitConverter.GetBytes((int)0), 0, 4);
            output.Seek(4, SeekOrigin.Begin);
            CryptoStream csEncrypt = null;
            Stream stream = output;


            if (aesAlg != null)
            {
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                csEncrypt = new CryptoStream(stream, encryptor, CryptoStreamMode.Write);
                stream = csEncrypt;
            }

            GZipStream compressionStream = null;
            if (UseCompress)
            {
                compressionStream = new GZipStream(stream, CompressionMode.Compress, true);
                stream = compressionStream;
            }


            int length = 0;
            while (pendings.Count > 0 && length < RecvBufferSize)
            {
                var msg = pendings.Dequeue();
                switch (msg)
                {
                    case global::Framework.Caspar.ISerializable serializable:
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
                        catch (Exception e)
                        {
                            global::Framework.Caspar.Api.Logger.Debug(e);
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


            if (compressionStream != null)
            {
                compressionStream.Flush();
                compressionStream.Dispose();
            }

            if (csEncrypt != null)
            {
                csEncrypt.FlushFinalBlock();
            }




            if (output.Length == 2)
            {
                return;
            }

            output.Seek(0, SeekOrigin.Begin);
            output.Write(BitConverter.GetBytes((int)output.Length), 0, 4);
            output.Seek(0, SeekOrigin.Begin);

            sendBuffer = output.ToArray();

            if (csEncrypt != null)
            {
                csEncrypt.Dispose();
            }

            output.Dispose();

            if (sendBuffer == null || sendBuffer.Length == 0)
            {
                sendBuffer = null;
                return;
            }

            socket.BeginSend(sendBuffer, 0, (int)sendBuffer.Length, SocketFlags.None, SendComplete, null);
        }
        public bool Write(MemoryStream stream)
        {

            if (stream.Length == 0) return true;
            lock (this)
            {

                if (IsClosed()) return false;

                stream.Seek(0, SeekOrigin.Begin);
                pendings.Enqueue(stream);

                if (sendBuffer != null)
                {
                    return true;
                }

                try
                {
                    flush();
                    return true;
                }
                catch (Exception e)
                {
                }
            }

            Disconnect();
            return false;
        }
        public bool Write(MemoryStream header, MemoryStream body)
        {

            if (header.Length == 0) return true;
            lock (this)
            {

                if (IsClosed()) return false;

                header.Seek(0, SeekOrigin.Begin);
                pendings.Enqueue(header);
                if (body.Length > 0)
                {
                    body.Seek(0, SeekOrigin.Begin);
                    pendings.Enqueue(body);
                }

                if (sendBuffer != null)
                {
                    return true;
                }

                try
                {
                    flush();
                    return true;
                }
                catch (Exception e)
                {

                }
            }

            Disconnect();
            return false;
        }

        static private void DefaultOnDisconnect()
        {
        }
        static private void DefaultOnConnect(bool ret)
        {
        }
        static private void DefaultOnAccept(bool ret)
        {
        }

        static private int DefaultOnRead(MemoryStream stream)
        {
            stream.Seek(0, SeekOrigin.End);

            return 0;
        }

        private void ListenComplete(IAsyncResult ar)
        {

            Socket listen = (Socket)ar.AsyncState;

            try
            {

                lock (this)
                {
                    socket = listen.EndAccept(ar);
                    state = (int)EState.Establish;
                }
                OnAccept(true);
                socket.BeginReceive(recvstream, 0, RecvBufferSize, SocketFlags.None, RecvComplete, null);

                return;
            }
            catch (Exception e)
            {
                //Framework.Caspar.Api.Logger.Debug(e);
                state = (int)EState.Closed;
                try
                {
                    OnAccept(false);
                }
                catch
                {

                }
                finally
                {
                    Disconnect();
                }
            }
            finally
            {
                global::Framework.Caspar.Api.Listen(port);
            }




        }
        private void ConnectComplete(IAsyncResult ar)
        {

            try
            {

                lock (this)
                {
                    socket.EndConnect(ar);
                    state = (int)EState.Establish;

                }

                OnConnect(true);

                lock (this)
                {
                    if (sendBuffer == null) { flush(); }
                }

                offset = 0;
                socket.BeginReceive(recvstream, 0, RecvBufferSize, SocketFlags.None, RecvComplete, null);

                return;
            }
            catch (Exception e)
            {
                global::Framework.Caspar.Api.Logger.Debug(e);
            }
            state = (int)EState.Closed;
            OnConnect(false);
            Disconnect();

        }

        protected virtual void Defragment(MemoryStream transferred)
        {
            var buffer = transferred.GetBuffer();

            int blockSize = 0;
            int readBytes = 0;

            while (transferred.Length - readBytes > sizeof(int))
            {
                blockSize = BitConverter.ToInt32(buffer, readBytes);
                if (blockSize < 1 || blockSize > RecvBufferSize)
                {
                    //global::Framework.Caspar.Api.Logger.Error($"Recv huge packet. BlockSize : {blockSize} - {this.GetType().FullName} @{RemoteEndPoint} - [{IP}-{Port}]");
                    transferred.Seek(transferred.Length, SeekOrigin.Begin);
                    Disconnect();
                    return;
                }

                if (blockSize + readBytes > transferred.Length) { break; }

                Stream stream = new MemoryStream(buffer, readBytes + 4, blockSize - 4, true, true);
                readBytes += blockSize;

                // //BitConverter.ToInt32()


                // Memory<byte> fsdf = null;

                // //fsdf.non

                // //ArraySegment<byte> sdfsadf = (ArraySegment<byte>)fsdf;

                // System.Runtime.InteropServices.MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)fsdf, out var seg);

                // var arr = seg.Array;

                // new MemoryStream(arr, false);



                CryptoStream csEncrypt = null;
                if (aesAlg != null)
                {
                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                    csEncrypt = new CryptoStream(stream, decryptor, CryptoStreamMode.Read);
                    stream = csEncrypt;
                }

                GZipStream compressionStream = null;
                if (UseCompress)
                {
                    compressionStream = new GZipStream(stream, CompressionMode.Decompress, true);
                    stream = compressionStream;
                }


                MemoryStream result = new MemoryStream();

                stream.CopyTo(result);
                result.Seek(0, SeekOrigin.Begin);

                try
                {
                    OnRead?.Invoke(result);
                }
                catch (Exception e)
                {
                    global::Framework.Caspar.Api.Logger.Info("OnRead Exception " + e);
                }


            }

            transferred.Seek(readBytes, SeekOrigin.Begin);

        }

        //internal void Merge(MemoryStream force, Tcp protocol)
        //{
        //    if (protocol == this)
        //    {
        //        lock (this)
        //        {
        //            while (pendings.Count > 0)
        //            {
        //                sendings.Enqueue(pendings.Dequeue());
        //            }

        //            pendings.Enqueue(force);

        //            while (sendings.Count > 0)
        //            {
        //                pendings.Enqueue(sendings.Dequeue());
        //            }
        //        }
        //        return;
        //    }

        //    Queue<object> sending = null;
        //    lock (protocol)
        //    {
        //        sendings = protocol.sendings;
        //        protocol.sendings = null;

        //        while (protocol.pendings.Count > 0)
        //        {
        //            sendings.Enqueue(protocol.pendings.Dequeue());
        //        }
        //    }
        //    lock (this)
        //    {
        //        if (sendings != null)
        //        {
        //            while (sendings.Count > 0)
        //            {
        //                pendings.Enqueue(sending.Dequeue());
        //            }
        //        }
        //    }
        //}

        //internal void Merge(Tcp protocol)
        //{
        //    if (protocol == null) { return; }
        //    if (protocol == this)
        //    {
        //        lock (this)
        //        {
        //            while(pendings.Count > 0)
        //            {
        //                sendings.Enqueue(pendings.Dequeue());
        //            }

        //            while (sendings.Count > 0)
        //            {
        //                pendings.Enqueue(sendings.Dequeue());
        //            }
        //        }
        //        return;
        //    }

        //    Queue<object> preSendings = null;
        //    lock (protocol)
        //    {
        //        preSendings = protocol.sendings;
        //        protocol.sendings = new Queue<object>();


        //        while (protocol.pendings.Count > 0)
        //        {
        //            preSendings.Enqueue(protocol.pendings.Dequeue());
        //        }
        //    }
        //    lock (this)
        //    {
        //        if (preSendings != null && preSendings.Count > 0)
        //        {
        //            var prePending = pendings;
        //            pendings = new Queue<object>();
        //            while(preSendings.Count > 0)
        //            {
        //                pendings.Enqueue(preSendings.Dequeue());
        //            }

        //            while (prePending.Count > 0)
        //            {
        //                pendings.Enqueue(prePending.Dequeue());
        //            }
        //        }
        //    }
        //}

        private void RecvComplete(IAsyncResult ar)
        {
            SocketError error;
            try
            {
                if (socket == null)
                {
                    Disconnect();
                    return;
                }
                int len = (int)socket.EndReceive(ar, out error);
                if (len == 0)
                {
                    Disconnect();
                    return;
                }

                len = offset + len;

                MemoryStream transferred = new MemoryStream(recvstream, 0, len, true, true);
                Defragment(transferred);
                offset = (int)len - (int)transferred.Position;
                if (offset < 0)
                {
                    Disconnect();
                    return;
                }

                Array.Copy(recvstream, transferred.Position, recvstream, 0, offset);
                socket.BeginReceive(recvstream, offset, RecvBufferSize - offset, SocketFlags.None, RecvComplete, null);





                // new MemoryStream()


                // var r = System.Buffers.MemoryPool<byte>.Shared.Rent(1024);

                // r.Memory


                Span<byte> fff = null;
                var ff = fff.Slice(0);
                //socket.ReceiveAsync()
                return;

            }
            catch (Exception e)
            {
                global::Framework.Caspar.Api.Logger.Debug(e);
            }

            Disconnect();



        }

        protected void SendComplete(IAsyncResult ar)
        {
            lock (this)
            {
                try
                {
                    int len = socket.EndSend(ar);
                    if (len == 0)
                    {
                        Disconnect();
                        return;
                    }
                    sendBuffer = null;
                    if (pendings.Count > 0)
                    {
                        flush();
                    }
                    return;

                }
                catch (Exception /*e*/)
                {

                }
            }
            Disconnect();
        }

        protected Socket socket = null;
        int offset = 0;
        private byte[] recvstream = new byte[65535];

        public EndPoint RemoteEndPoint
        {
            get
            {
                try
                {
                    return socket.RemoteEndPoint;
                }
                catch
                {
                    return null;
                }
            }
        }
        public EndPoint LocalEndPoint
        {
            get
            {
                try
                {
                    return socket.LocalEndPoint;
                }
                catch
                {
                    return null;
                }
            }
        }

    }
}

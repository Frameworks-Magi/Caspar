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
using Microsoft.Extensions.ObjectPool;
using System.Buffers;

namespace Caspar.Protocol
{

    public class Tcp
    {
        public class PoolableSocketAsyncEventArgs<T> : SocketAsyncEventArgs where T : class, new()
        {
            ~PoolableSocketAsyncEventArgs()
            {
                Caspar.Api.Logger.Warning($"{typeof(T)} is GC");
            }
            protected static DefaultObjectPool<T> pool = new(new DefaultPooledObjectPolicy<T>());
            public static T Instantiate
            {
                get
                {
                    //return new T(); //pool.Get();
                    return pool.Get();
                }
            }

            public new void Dispose()
            {
                pool.Return(this as T);
            }

        }

        public class AcceptSocketAsyncEventArgs : PoolableSocketAsyncEventArgs<AcceptSocketAsyncEventArgs>
        {
            public int Index { get; set; }
            protected override void OnCompleted(SocketAsyncEventArgs e)
            {
                try
                {
                    var protocol = e.UserToken as Tcp;
                    protocol.onAcceptComplete(e);
                }
                catch
                {

                }
                finally
                {
                    e.AcceptSocket = null;
                    e.UserToken = null;
                    Dispose();
                }
            }
        }

        public class RecvSocketAsyncEventArgs : PoolableSocketAsyncEventArgs<RecvSocketAsyncEventArgs>
        {
            public void Recv(Tcp protocol)
            {
                try
                {
                    UserToken = protocol;
                    SetBuffer(protocol.recvBuffer, protocol.Offset, protocol.recvBuffer.Length - protocol.Offset);
                    if (protocol.socket.ReceiveAsync(this) == false)
                    {
                        OnCompleted(this);
                    }
                }
                catch
                {
                    this.SocketError = SocketError.SocketError;
                    OnCompleted(this);
                }

            }

            protected override void OnCompleted(SocketAsyncEventArgs e)
            {
                try
                {
                    var protocol = e.UserToken as Tcp;
                    if (e.SocketError != SocketError.Success || e.BytesTransferred == 0)
                    {
                        protocol.Disconnect();
                        return;
                    }

                    protocol.onRecvComplete(e);
                }
                catch
                {

                }
                finally
                {
                    Dispose();
                }
            }
        }

        public class SendSocketAsyncEventArgs : PoolableSocketAsyncEventArgs<SendSocketAsyncEventArgs>
        {
            protected override void OnCompleted(SocketAsyncEventArgs e)
            {
                try
                {
                    var protocol = e.UserToken as Tcp;
                    protocol.onSendComplete(e);
                }
                catch
                {

                }
                finally
                {
                    Dispose();
                }
            }
            public void Write(Tcp protocol, byte[] buffer, int length)
            {
                try
                {
                    UserToken = protocol;
                    SetBuffer(buffer, 0, length);
                    if (protocol.socket.SendAsync(this) == false)
                    {
                        OnCompleted(this);
                    }
                }
                catch
                {
                    this.SocketError = SocketError.SocketError;
                    OnCompleted(this);
                }

            }

        }

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
                recvBuffer = new byte[value];
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
        public virtual bool Accept(ushort port)
        {
            if (socket != null)
            {
                global::Caspar.Api.Logger.Info("!!!!!!!!!!!!!!! Accept Listen Fail socket == null !!!!!!!!!!!!!!");
                return false;
            }
            if (global::Caspar.Api.IsOpen == false) return false;

            this.port = port;

            Socket listen = global::Caspar.Api.Acceptor(port);
            if (listen == null)
            {
                global::Caspar.Api.Logger.Info("!!!!!!!!!!!!!!! Accept Listen Fail listen == null !!!!!!!!!!!!!!");
                return false;
            }

            try
            {
                var args = AcceptSocketAsyncEventArgs.Instantiate;
                args.UserToken = this;
                listen.AcceptAsync(args);
                return true;
            }
            catch (Exception e)
            {
                global::Caspar.Api.Logger.Info($"{port}  Accept Listen Fail ex");
                global::Caspar.Api.Logger.Info("!!!!!!!!!!!!!!! Accept Listen Fail !!!!!!!!!!!!!!\n" + e);
            }
            return false;
        }
        public virtual bool Connect(string ip, ushort port)
        {
            if (global::Caspar.Api.IsOpen == false) return false;
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
                global::Caspar.Api.Logger.Debug(e);
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

        public bool Write(global::Caspar.ISerializable msg)
        {
            lock (this)
            {
                if (IsClosed())
                {
                    return false;
                }
                pendings.Enqueue(msg);
                if (sendBuffer != null) { return true; }
                try
                {
                    flush();
                    return true;
                }
                catch (Exception e)
                {
                    //Caspar.Api.Logger.Verbose($"Ip = {IP}, Port = {Port}");
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
                        catch (Exception e)
                        {
                            global::Caspar.Api.Logger.Debug(e);
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

            var args = PoolableSocketAsyncEventArgs<SendSocketAsyncEventArgs>.Instantiate;
            args.Write(this, sendBuffer, sendBuffer.Length);
            //socket.BeginSend(sendBuffer, 0, (int)sendBuffer.Length, SocketFlags.None, SendComplete, null);
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
                catch (Exception /* e */)
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
                catch (Exception /* e */)
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

        protected virtual void onAcceptComplete(SocketAsyncEventArgs e)
        {

            try
            {
                if (e.SocketError != SocketError.Success)
                {
                    state = (int)EState.Closed;
                    try
                    {
                        var invoker = OnAccept;
                        OnAccept = null;
                        invoker?.Invoke(false);
                    }
                    catch
                    {

                    }

                    try
                    {
                        Disconnect();
                    }
                    catch
                    {

                    }
                }
                else
                {
                    lock (this)
                    {
                        state = (int)EState.Establish;
                        socket = e.AcceptSocket;
                    }
                    Offset = 0;
                    try
                    {
                        var invoker = OnAccept;
                        OnAccept = null;
                        invoker?.Invoke(true);
                    }
                    catch
                    {

                    }
                    var recv = PoolableSocketAsyncEventArgs<RecvSocketAsyncEventArgs>.Instantiate;
                    recv.Recv(this);
                }

            }
            catch
            {
                try
                {
                    OnAccept?.Invoke(false);
                }
                catch
                {

                }

                try
                {
                    Disconnect();
                }
                catch
                {

                }
            }
            finally
            {
                global::Caspar.Api.Listen(port);
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
                Offset = 0;
                var recv = PoolableSocketAsyncEventArgs<RecvSocketAsyncEventArgs>.Instantiate;
                recv.Recv(this);

                lock (this)
                {
                    if (sendBuffer == null) { flush(); }
                }
                return;
            }
            catch (Exception e)
            {
                global::Caspar.Api.Logger.Debug(e);
            }
            state = (int)EState.Closed;
            OnConnect(false);
            Disconnect();

        }

        protected virtual void defragmentation(MemoryStream transferred)
        {
            var buffer = transferred.GetBuffer();

            int blockSize = 0;
            int readBytes = 0;

            while (transferred.Length - readBytes > sizeof(int))
            {
                blockSize = BitConverter.ToInt32(buffer, readBytes);
                if (blockSize < 1 || blockSize > RecvBufferSize)
                {
                    //global::Caspar.Api.Logger.Error($"Recv huge packet. BlockSize : {blockSize} - {this.GetType().FullName} @{RemoteEndPoint} - [{IP}-{Port}]");
                    transferred.Seek(transferred.Length, SeekOrigin.Begin);
                    Disconnect();
                    return;
                }

                if (blockSize + readBytes > transferred.Length) { break; }

                Stream stream = new MemoryStream(buffer, readBytes + 4, blockSize - 4, true, true);
                readBytes += blockSize;

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
                    global::Caspar.Api.Logger.Info("OnRead Exception " + e);
                }


            }

            transferred.Seek(readBytes, SeekOrigin.Begin);

        }

        protected virtual void onRecvComplete(SocketAsyncEventArgs args)
        {
            try
            {
                if (socket == null)
                {
                    Disconnect();
                    return;
                }
                int len = args.BytesTransferred + args.Offset;

                MemoryStream transferred = new MemoryStream(recvBuffer, 0, len, true, true);
                defragmentation(transferred);
                Offset = (int)len - (int)transferred.Position;
                if (Offset < 0)
                {
                    Disconnect();
                    return;
                }

                if (Offset > 0)
                {
                    Array.Copy(recvBuffer, transferred.Position, recvBuffer, 0, Offset);
                }

                var recv = PoolableSocketAsyncEventArgs<RecvSocketAsyncEventArgs>.Instantiate;
                recv.Recv(this);
                return;
            }
            catch (Exception e)
            {
                global::Caspar.Api.Logger.Debug(e);
            }
            Disconnect();
        }

        protected internal virtual void onSendComplete(SocketAsyncEventArgs e)
        {

            lock (this)
            {
                //ArrayPool<byte>.Shared.Return(sendBuffer);
                sendBuffer = null;
                if (e.SocketError != SocketError.Success || e.BytesTransferred == 0)
                {
                    Disconnect();
                    return;
                }

                try
                {
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
        protected int Offset = 0;
        protected byte[] recvBuffer = new byte[65535];

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

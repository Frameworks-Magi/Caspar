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
using Microsoft.Extensions.ObjectPool;
using System.Buffers;
using static Caspar.Api;
using System.Text;

namespace Caspar.Protocol
{

    public class WebSocket
    {

        // public bool Write()
        // {
        //     socket.SendAsync()
        // }

        public bool Write(global::Caspar.ISerializable msg)
        {
            lock (this)
            {
                if (socket == null)
                {
                    return false;
                }
                pendings.Enqueue(msg);
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
                    //Caspar.Api.Logger.Verbose($"Ip = {IP}, Port = {Port}");
                }
            }
            _ = Disconnect();
            return false;
        }

        public bool Write(byte[] msg)
        {
            lock (this)
            {
                if (socket == null)
                {
                    return false;
                }
                pendings.Enqueue(msg);
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
                    //Caspar.Api.Logger.Verbose($"Ip = {IP}, Port = {Port}");
                }
            }
            _ = Disconnect();
            return false;
        }

        protected virtual void OnConnect()
        {

        }
        public async Task OnConnected()
        {
            try
            {

                recvBuffer = ArrayPool<byte>.Shared.Rent(65535);
                OnConnect();
                while (true)
                {
                    if (await ReceiveAsync() == false) { break; }
                }
            }
            catch
            {

            }
            finally
            {
                ArrayPool<byte>.Shared.Return(recvBuffer);
            }

        }


        protected virtual void OnRead(MemoryStream transferred)
        {
            transferred.Seek(0, SeekOrigin.End);

            var buffer = ("Received " + transferred.Length + " bytes").ToBytes();
            Write(buffer);

        }

        public async Task<bool> ReceiveAsync()
        {
            byte[] bytes = ArrayPool<byte>.Shared.Rent(65535);
            try
            {
                Memory<byte> buffer = new Memory<byte>(bytes);
                var ret = await socket.ReceiveAsync(buffer, CancellationToken.None);

                if (ret.Count == 0)
                {
                    await Disconnect();
                }
                else
                {
                    defragmentation(bytes, ret.Count);
                }

            }
            catch (Exception e)
            {
                //  Logger.Debug(e);
                await Disconnect();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytes);
            }
            return socket != null;
        }

        public void Write()
        {
            lock (this)
            {
                if (sendBuffer == null)
                {
                    flush();
                }
            }
        }
        protected byte[] sendBuffer = null;

        protected Queue<object> pendings = new Queue<object>();
        protected virtual void flush()
        {
            if (pendings.Count == 0) { return; }

            sendBuffer = ArrayPool<byte>.Shared.Rent(65536);
            MemoryStream stream = new MemoryStream(sendBuffer, 0, 65535);
            int length = 0;
            while (pendings.Count > 0 && length < 65535)
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
                            _ = Disconnect();
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

            _ = Task.Run(async () =>
            {
                try
                {
                    await socket.SendAsync(new ReadOnlyMemory<byte>(sendBuffer, 0, length), System.Net.WebSockets.WebSocketMessageType.Binary, true, CancellationToken.None);
                    return;
                }
                catch
                {
                    _ = Disconnect();
                }
                finally
                {
                    lock (this)
                    {
                        ArrayPool<byte>.Shared.Return(sendBuffer);
                        sendBuffer = null;
                        try
                        {
                            if (pendings.Count > 0)
                            {
                                flush();
                            }
                        }
                        catch
                        {

                        }
                    }
                }

            });
        }

        protected virtual void defragmentation(byte[] transferred, int length)
        {
            Array.Copy(transferred, 0, recvBuffer, Offset, length);
            Offset += length;
            length = Offset;

            var buffer = recvBuffer;

            using var stream = new MemoryStream(buffer, 0, length, true, true);

            OnRead(stream);

            Offset = (int)length - (int)stream.Position;

            buffer = ArrayPool<byte>.Shared.Rent(65535);

            if (Offset > 0)
            {
                Array.Copy(recvBuffer, stream.Position, buffer, 0, Offset);
            }
            ArrayPool<byte>.Shared.Return(recvBuffer);
            recvBuffer = buffer;

        }

        public async Task Disconnect()
        {
            try
            {
                System.Net.WebSockets.WebSocket temp = null;
                lock (this)
                {
                    if (socket == null) { return; }
                    temp = socket;
                    socket = null;

                }
                OnDisconnect();
                await temp.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.Empty, "", CancellationToken.None);
            }
            catch
            {

            }
        }
        protected virtual void OnDisconnect()
        {

        }

        protected byte[] recvBuffer;
        protected int Offset { get; set; }
        public System.Net.WebSockets.WebSocket socket { get; set; } = null;

    }
}

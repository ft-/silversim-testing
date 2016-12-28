// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Http;
using SilverSim.Types;
using System;
using System.IO;

namespace SilverSim.Main.Common.HttpServer
{
    public class HttpWebSocket : IDisposable
    {
        public enum CloseReason
        {
            NormalClosure = 1000,
            GoingAway = 1001,
            ProtocolError = 1002,
            UnsupportedData = 1003,
            NoStatusReceived = 1005,
            InvalidFramePayloadData = 1007,
            PolicyViolation = 1008,
            MessageTooBig = 1009,
            MandatoryExtension = 1010,
            InternalError = 1011,
            ServiceRestart = 1012,
            TryAgainLater = 1013,
            BadGateway = 1014,
        }
        [Serializable]
        public class MessageTimeoutException : Exception
        {
            public MessageTimeoutException()
            {

            }
        }

        static Random Random = new Random();
        static byte[] MaskingKey
        {
            get
            {
                int mask;
                lock(Random)
                {
                    mask = Random.Next(int.MinValue, int.MaxValue);
                }
                return BitConverter.GetBytes(mask);
            }
        }
        readonly Stream m_WebSocketStream;
        bool m_IsClosed;
        readonly object m_SendLock = new object();
        bool m_IsDisposed;

        internal HttpWebSocket(Stream o)
        {
            m_WebSocketStream = o;
        }

        public void Close(CloseReason reason = CloseReason.NormalClosure)
        {
            if (!m_IsDisposed)
            {
                try
                {
                    SendClose(CloseReason.NormalClosure);
                }
                catch
                {
                    /* intentionally ignore errors */
                }
            }
            m_WebSocketStream.Dispose();
            m_IsDisposed = true;
        }

        public void Dispose()
        {
            if (!m_IsDisposed)
            {
                try
                {
                    SendClose(CloseReason.NormalClosure);
                }
                catch
                {
                    /* intentionally ignore errors */
                }
            }
            m_WebSocketStream.Dispose();
            m_IsDisposed = true;
        }

        enum OpCode
        {
            Continuation = 0,
            Text = 1,
            Binary = 2,
            Close = 8,
            Ping = 9,
            Pong = 10,
        }

        public enum MessageType
        {
            Text = 1,
            Binary = 2
        }

        public struct Message
        {
            public MessageType Type;
            public bool IsLastSegment;
            public byte[] Data;
        }

        public Message Receive()
        {
            m_WebSocketStream.ReadTimeout = 1000;
            for (;;)
            {
                byte[] hdr = new byte[2];
                if (m_IsClosed)
                {
                    throw new WebSocketClosedException();
                }
                try
                {
                    if (2 != m_WebSocketStream.Read(hdr, 0, 2))
                    {
                        m_IsClosed = true;
                        throw new WebSocketClosedException();
                    }
                }
                catch(HttpStream.TimeoutException)
                {
                    throw new MessageTimeoutException();
                }

                OpCode opcode = (OpCode)(hdr[0] & 0xF);
                if (opcode == OpCode.Close)
                {
                    m_IsClosed = true;
                    throw new WebSocketClosedException();
                }
                int payloadlen = hdr[1] & 0x7F;
                if (payloadlen == 127)
                {
                    byte[] leninfo = new byte[8];
                    if (8 != m_WebSocketStream.Read(leninfo, 0, 8))
                    {
                        m_IsClosed = true;
                        throw new WebSocketClosedException();
                    }
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(leninfo);
                    }
                    payloadlen = (int)BitConverter.ToUInt64(leninfo, 0);
                }
                else if (payloadlen == 126)
                {
                    byte[] leninfo = new byte[2];
                    if (2 != m_WebSocketStream.Read(leninfo, 0, 2))
                    {
                        m_IsClosed = true;
                        throw new WebSocketClosedException();
                    }
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(leninfo);
                    }
                    payloadlen = BitConverter.ToUInt16(leninfo, 0);
                }
                else
                {
                    payloadlen = hdr[1] & 0x7F;
                }
                byte[] maskingkey = new byte[4] { 0, 0, 0, 0 };
                if ((hdr[1] & 128) != 0)
                {
                    if (4 != m_WebSocketStream.Read(maskingkey, 0, 4))
                    {
                        m_IsClosed = true;
                        throw new WebSocketClosedException();
                    }
                }

                byte[] payload = new byte[payloadlen];
                if (payloadlen != m_WebSocketStream.Read(payload, 0, payloadlen))
                {
                    m_IsClosed = true;
                    throw new WebSocketClosedException();
                }
                for (int offset = 0; offset < payloadlen; ++offset)
                {
                    payload[offset] ^= maskingkey[offset % 4];
                }

                if (opcode == OpCode.Binary)
                {
                    Message msg = new Message();
                    msg.Data = payload;
                    msg.Type = MessageType.Binary;
                    msg.IsLastSegment = (hdr[0] & 128) != 0;
                    return msg;
                }
                else if (opcode == OpCode.Text)
                {
                    Message msg = new Message();
                    msg.Data = payload;
                    msg.Type = MessageType.Text;
                    msg.IsLastSegment = (hdr[0] & 128) != 0;
                    return msg;
                }
                else if (opcode == OpCode.Ping)
                {
                    SendFrame(OpCode.Pong, true, payload, 0, payload.Length, true);
                }
            }
        }

        public void WriteText(string text, bool fin = true, bool masked = false)
        {
            byte[] utf8 = text.ToUTF8Bytes();
            SendFrame(OpCode.Text, fin, utf8, 0, utf8.Length, masked);
        }

        public void WriteBinary(byte[] data, int offset, int length, bool fin = true, bool masked = false)
        {
            SendFrame(OpCode.Binary, fin, data, offset, length, masked);
        }

        void SendClose(CloseReason reason)
        {
            byte[] res = BitConverter.GetBytes((ushort)reason);
            if(BitConverter.IsLittleEndian)
            {
                Array.Reverse(res);
            }
            SendFrame(OpCode.Close, true, res, 0, 2);
        }

        void SendFrame(OpCode opcode, bool fin, byte[] payload, int offset, int length, bool masked = false)
        {
            lock (m_SendLock)
            {
                byte[] frame;
                byte[] maskingkey;

                if (offset < 0 || length < 0 || offset > payload.Length || offset + length > payload.Length || offset + length < 0)
                {
                    throw new ArgumentOutOfRangeException();
                }

                if (length < 126)
                {
                    frame = new byte[2];
                    frame[1] = (byte)length;
                }
                else if (payload.Length < 65536)
                {
                    frame = new byte[4];
                    frame[1] = 126;
                    frame[2] = (byte)(payload.Length >> 8);
                    frame[3] = (byte)(payload.Length & 0xFF);
                }
                else
                {
                    frame = new byte[10];
                    frame[1] = 127;
                    frame[2] = 0;
                    frame[3] = 0;
                    frame[4] = 0;
                    frame[5] = 0;
                    frame[6] = (byte)((payload.Length >> 24) & 0xFF);
                    frame[7] = (byte)((payload.Length >> 16) & 0xFF);
                    frame[8] = (byte)((payload.Length >> 8) & 0xFF);
                    frame[9] = (byte)(payload.Length & 0xFF);
                }
                frame[0] = (byte)(int)opcode;
                if (fin)
                {
                    frame[0] |= 128;
                }
                if (masked)
                {
                    frame[1] |= 128;
                    maskingkey = MaskingKey;
                }
                else
                {
                    maskingkey = new byte[4] { 0, 0, 0, 0 };
                }
                m_WebSocketStream.Write(frame, 0, frame.Length);
                if (masked)
                {
                    m_WebSocketStream.Write(maskingkey, 0, maskingkey.Length);
                    byte[] maskedpayload = new byte[payload.Length];
                    for (int i = 0; i < maskedpayload.Length; ++i)
                    {
                        maskedpayload[i] = (byte)(payload[i + offset] ^ maskingkey[i % 4]);
                    }
                    m_WebSocketStream.Write(maskedpayload, 0, maskedpayload.Length);
                }
                else
                {
                    m_WebSocketStream.Write(payload, offset, length);
                }
            }
        }
    }
}

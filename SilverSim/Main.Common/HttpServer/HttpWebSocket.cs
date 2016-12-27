// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.IO;
using SilverSim.Types;

namespace SilverSim.Main.Common.HttpServer
{
    public class HttpWebSocket : IDisposable
    {
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

        public void Dispose()
        {
            if (!m_IsDisposed)
            {
                try
                {
                    SendFrame(OpCode.Close, true, new byte[0], 0, 0);
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
            for (;;)
            {
                byte[] hdr = new byte[2];
                if (m_IsClosed)
                {
                    throw new WebSocketClosedException();
                }
                if (2 != m_WebSocketStream.Read(hdr, 0, 2))
                {
                    m_IsClosed = true;
                    throw new WebSocketClosedException();
                }

                OpCode opcode = (OpCode)(hdr[0] >> 4);
                if (opcode == OpCode.Close)
                {
                    m_IsClosed = true;
                    throw new WebSocketClosedException();
                }
                int payloadlen = hdr[1] >> 1;
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
                    payloadlen = hdr[1] >> 1;
                }
                byte[] maskingkey = new byte[4] { 0, 0, 0, 0 };
                if ((hdr[1] & 1) != 0)
                {
                    if (4 != m_WebSocketStream.Read(maskingkey, 0, 4))
                    {
                        m_IsClosed = true;
                        throw new WebSocketClosedException();
                    }
                }

                byte[] payload = new byte[payloadlen];
                int offset = 0;
                while (payloadlen - offset > 10240)
                {
                    if (10240 != m_WebSocketStream.Read(payload, offset, 10240))
                    {
                        m_IsClosed = true;
                        throw new WebSocketClosedException();
                    }
                    offset += 10240;
                }
                if (payloadlen > offset)
                {
                    if (payloadlen - offset != m_WebSocketStream.Read(payload, offset, payloadlen - offset))
                    {
                        m_IsClosed = true;
                        throw new WebSocketClosedException();
                    }
                }
                for (offset = 0; offset < payloadlen; ++offset)
                {
                    payload[offset] ^= maskingkey[offset % 4];
                }

                if (opcode == OpCode.Binary)
                {
                    Message msg = new Message();
                    msg.Data = payload;
                    msg.Type = MessageType.Binary;
                    msg.IsLastSegment = (hdr[0] & 1) != 0;
                    return msg;
                }
                else if (opcode == OpCode.Text)
                {
                    Message msg = new Message();
                    msg.Data = payload;
                    msg.Type = MessageType.Text;
                    msg.IsLastSegment = (hdr[0] & 1) != 0;
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
                    frame[1] = (byte)((length << 1) | 1);
                }
                else if (payload.Length < 65536)
                {
                    frame = new byte[4];
                    frame[1] = 0xFC;
                    frame[2] = (byte)(payload.Length >> 8);
                    frame[3] = (byte)(payload.Length & 0xFF);
                }
                else
                {
                    frame = new byte[10];
                    frame[1] = 0xFE;
                    frame[2] = 0;
                    frame[3] = 0;
                    frame[4] = 0;
                    frame[5] = 0;
                    frame[6] = (byte)((payload.Length >> 24) & 0xFF);
                    frame[7] = (byte)((payload.Length >> 16) & 0xFF);
                    frame[8] = (byte)((payload.Length >> 8) & 0xFF);
                    frame[9] = (byte)(payload.Length & 0xFF);
                }
                frame[0] = (byte)((int)opcode << 4);
                if (fin)
                {
                    frame[0] |= 1;
                }
                if (masked)
                {
                    frame[1] |= 1;
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

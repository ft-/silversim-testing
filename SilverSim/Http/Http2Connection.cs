// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using SilverSim.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace SilverSim.Http
{
    public sealed class Http2Connection : IDisposable
    {
        private readonly Stream m_OriginalStream;
        private readonly RwLockedDictionary<uint, Http2Stream> m_Streams = new RwLockedDictionary<uint, Http2Stream>();
        private int m_ActiveUsers;
        private bool m_GoAwaySent;

        [Serializable]
        public class ProtocolErrorException : Exception
        {
            public ProtocolErrorException()
            {
            }

            public ProtocolErrorException(string message) : base(message)
            {
            }

            public ProtocolErrorException(string message, Exception innerException) : base(message, innerException)
            {
            }

            protected ProtocolErrorException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
            {
            }
        }

        [Serializable]
        public class StreamErrorException : Exception
        {
            public StreamErrorException()
            {
            }

            public StreamErrorException(string message) : base(message)
            {
            }

            public StreamErrorException(string message, Exception innerException) : base(message, innerException)
            {
            }

            protected StreamErrorException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
            {
            }
        }

        public Http2Connection(Stream originalStream)
        {
            m_OriginalStream = originalStream;
        }

        public void Dispose()
        {
            if (Interlocked.Decrement(ref m_ActiveUsers) == 0)
            {
                SendGoAway(Http2ErrorCode.NoError);
                m_OriginalStream?.Dispose();
            }
        }

        internal enum Http2ErrorCode
        {
            NoError = 0,
            ProtocolError = 1,
            InternalError = 2,
            FlowControlError = 3,
            SettingsTimeout = 4,
            StreamClosed = 5,
            FrameSizeError = 6,
            RefusedStream = 7,
            Cancel = 8,
            CompressionError = 9,
            ConnectError = 10,
            EnhanceYourCalm = 11,
            InadequateSecurity = 12,
            Http11Required = 13
        }

        internal enum Http2FrameType : byte
        {
            Data = 0x0,
            Headers = 0x1,
            Priority = 0x2,
            RstStream = 0x3,
            Settings = 0x4,
            PushPromise = 0x5,
            Ping = 0x6,
            GoAway = 0x7,
            WindowUpdate = 0x8,
            Continuation = 0x9
        }

        [Flags]
        internal enum DataFrameFlags : byte
        {
            None = 0,
            EndStream = 0x1,
            Padded = 0x8
        }

        [Flags]
        internal enum HeadersFrameFlags : byte
        {
            None = 0,
            EndStream = 0x1,
            EndHeaders = 0x4,
            Padded = 0x8,
            Priority = 0x20
        }

        [Flags]
        internal enum SettingsFrameFlags : byte
        {
            None = 0,
            Ack = 0x01
        }

        [Flags]
        internal enum PushPromiseFrameFlags : byte
        {
            None = 0,
            EndHeaders = 0x4,
            Padded = 0x8
        }

        [Flags]
        internal enum PingFrameFlags : byte
        {
            None = 0,
            Ack = 0x1
        }

        [Flags]
        internal enum ContinuationFrameFlags : byte
        {
            None = 0,
            EndHeaders = 0x4
        }

        internal sealed class Http2Frame
        {
            public int Length;
            public Http2FrameType Type;
            public byte Flags;
            public uint StreamIdentifier;
            public byte[] Data;
        }

        private readonly object m_SendLock = new object();
        private uint m_LastReceivedStreamId;

        private void SendGoAway(Http2ErrorCode reason)
        {
            lock (m_SendLock)
            {
                if (m_GoAwaySent)
                {
                    return;
                }
                m_GoAwaySent = true;
            }
            var goawaydata = new byte[8];
            goawaydata[0] = (byte)((m_LastReceivedStreamId >> 24) & 0xFF);
            goawaydata[1] = (byte)((m_LastReceivedStreamId >> 16) & 0xFF);
            goawaydata[2] = (byte)((m_LastReceivedStreamId >> 8) & 0xFF);
            goawaydata[3] = (byte)(m_LastReceivedStreamId & 0xFF);
            goawaydata[4] = (byte)(((int)reason >> 24) & 0xFF);
            goawaydata[5] = (byte)(((int)reason >> 16) & 0xFF);
            goawaydata[6] = (byte)(((int)reason >> 8) & 0xFF);
            goawaydata[7] = (byte)((int)reason & 0xFF);

            SendFrame(Http2FrameType.GoAway, 0, 0, goawaydata, 0, goawaydata.Length);
        }

        internal void SendFrame(Http2FrameType type, byte flags, uint streamid, byte[] data, int offset, int length)
        {
            if(offset < 0 || length < 0 ||
                offset > data.Length || length > data.Length ||
                offset + length > data.Length ||
                length > 0xFFFFFF)
            {
                throw new ArgumentOutOfRangeException();
            }
            lock (m_SendLock)
            {
                if (m_OriginalStream != null)
                {
                    var hdr = new byte[9];
                    hdr[0] = (byte)((length >> 16) & 0xFF);
                    hdr[1] = (byte)((length >> 8) & 0xFF);
                    hdr[2] = (byte)(length & 0xFF);
                    hdr[3] = (byte)type;
                    hdr[4] = flags;
                    hdr[5] = (byte)((streamid >> 24) & 0x7F);
                    hdr[6] = (byte)((streamid >> 16) & 0xFF);
                    hdr[7] = (byte)((streamid >> 8) & 0xFF);
                    hdr[8] = (byte)(streamid & 0xFF);
                    m_OriginalStream.Write(hdr, 0, 9);
                    m_OriginalStream.Write(data, offset, length);
                }
            }
        }

        internal void SendRstStream(uint streamid, Http2ErrorCode error)
        {
            var errorcode = (uint)error;
            byte[] block = BitConverter.GetBytes(errorcode);
            if(BitConverter.IsLittleEndian)
            {
                Array.Reverse(block);
            }
            SendFrame(Http2FrameType.RstStream, 0, streamid, block, 0, 4);
        }

        private Http2Frame ReceiveFrame()
        {
            Http2Frame frame;
            do
            {
                frame = new Http2Frame();
                var hdr = new byte[9];
                if (9 != m_OriginalStream.Read(hdr, 0, 9))
                {
                    SendGoAway(Http2ErrorCode.ProtocolError);
                    throw new ProtocolErrorException();
                }

                frame.Length = (hdr[0] << 16) | (hdr[1] << 8) | (hdr[2]);
                frame.Type = (Http2FrameType)hdr[3];
                frame.Flags = hdr[4];
                frame.StreamIdentifier = (uint)(((hdr[5] & 0x7F) << 24) | (hdr[6] << 16) | (hdr[7] << 8) | hdr[8]);
                frame.Data = new byte[frame.Length];

                int rcvdbytes;
                try
                {
                    rcvdbytes = m_OriginalStream.Read(frame.Data, 0, frame.Length);
                }
                catch
                {
                    SendGoAway(Http2ErrorCode.ProtocolError);
                    throw new ProtocolErrorException();
                }

                if (frame.Length != rcvdbytes)
                {
                    SendGoAway(Http2ErrorCode.ProtocolError);
                    throw new ProtocolErrorException();
                }
                if(frame.Type == Http2FrameType.Ping)
                {
                    if(frame.StreamIdentifier != 0)
                    {
                        SendGoAway(Http2ErrorCode.ProtocolError);
                        throw new ProtocolErrorException();
                    }
                    if(frame.Length != 8)
                    {
                        SendGoAway(Http2ErrorCode.FrameSizeError);
                        throw new ProtocolErrorException();
                    }
                    SendFrame(Http2FrameType.Ping, (byte)PingFrameFlags.Ack, 0, frame.Data, 0, frame.Length);
                }
            } while (frame.Type == Http2FrameType.Ping);
            m_LastReceivedStreamId = frame.StreamIdentifier;
            return frame;
        }

        public void Run()
        {
            Interlocked.Increment(ref m_ActiveUsers);
            for(; ;)
            {
                Http2Frame frame = ReceiveFrame();

                if (frame.StreamIdentifier == 0)
                {
                    switch (frame.Type)
                    {
                        case Http2FrameType.Settings:
                            break;

                        default:
                            SendGoAway(Http2ErrorCode.ProtocolError);
                            throw new ProtocolErrorException();
                    }
                }
                else
                {
                    Http2Stream stream;
                    switch (frame.Type)
                    {
                        case Http2FrameType.Headers:
                            if (!m_Streams.TryGetValue(frame.StreamIdentifier, out stream))
                            {
                                stream = new Http2Stream(this, frame.StreamIdentifier);
                                m_Streams[frame.StreamIdentifier] = stream;
                            }
                            stream.NewFrameReceived(frame);
                            break;

                        case Http2FrameType.Data:
                        case Http2FrameType.WindowUpdate:
                        case Http2FrameType.Continuation:
                        case Http2FrameType.Priority:
                            if (m_Streams.TryGetValue(frame.StreamIdentifier, out stream))
                            {
                                stream.NewFrameReceived(frame);
                            }
                            else
                            {
                                SendRstStream(frame.StreamIdentifier, Http2ErrorCode.ProtocolError);
                            }
                            break;

                        case Http2FrameType.RstStream:
                            if (m_Streams.TryGetValue(frame.StreamIdentifier, out stream))
                            {
                                stream.NewFrameReceived(frame);
                            }
                            break;
                    }
                }
            }
        }

        public class Http2Stream : Stream
        {
            private BlockingQueue<Http2Frame> m_Http2Queue = new BlockingQueue<Http2Frame>();
            private readonly Http2Connection m_Conn;
            private readonly uint m_StreamIdentifier;
            private bool m_HaveReceivedHeaders;
            private bool m_HaveSentHeaders;
            private bool m_HaveReceivedEoS;
            private bool m_HaveSentEoS;
            private byte[] m_BufferedReceiveData;
            private int m_ConsumedReceiveDataBytes;
            private int m_AvailableReceiveDataBytes;
            private byte[] m_BufferedTransmitData = new byte[16384];
            private int m_BufferedTransmitDataBytes;
            private int m_MaxTransmitDataBytes = 16384;

            public Http2Stream(Http2Connection conn, uint streamid)
            {
                m_Conn = conn;
                m_StreamIdentifier = streamid;
                Interlocked.Increment(ref conn.m_ActiveUsers);
            }

            ~Http2Stream()
            {
                m_Conn.Dispose();
            }

            protected override void Dispose(bool disposing)
            {
                if(!m_HaveSentEoS)
                {
                    m_Conn.SendFrame(Http2FrameType.Data, (byte)DataFrameFlags.EndStream, m_StreamIdentifier, m_BufferedTransmitData, 0, m_BufferedTransmitDataBytes);
                }
                m_Conn.Dispose();
            }

            internal void NewFrameReceived(Http2Frame frame)
            {
                m_Http2Queue.Enqueue(frame);
            }

            public override bool CanRead => true;

            public override bool CanSeek => false;

            public override bool CanWrite => true;

            public override long Length
            {
                get
                {
                    throw new NotSupportedException();
                }
            }

            public override long Position
            {
                get
                {
                    throw new NotSupportedException();
                }
                set
                {
                    throw new NotSupportedException();
                }
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int consumed = 0;
                if(!m_HaveReceivedHeaders)
                {
                    m_Conn.SendRstStream(m_StreamIdentifier, Http2ErrorCode.ProtocolError);
                    throw new StreamErrorException();
                }
                
                while(count > 0)
                {
                    if (m_ConsumedReceiveDataBytes == m_AvailableReceiveDataBytes)
                    {
                        if (m_HaveReceivedEoS)
                        {
                            return 0;
                        }

                        Http2Frame frame = m_Http2Queue.Dequeue();
                        if (frame.Type != Http2FrameType.Data)
                        {
                            m_Conn.SendRstStream(m_StreamIdentifier, Http2ErrorCode.ProtocolError);
                            throw new StreamErrorException();
                        }
                        if ((frame.Flags & (byte)DataFrameFlags.EndStream) != 0)
                        {
                            m_HaveReceivedEoS = true;
                        }

                        if((frame.Flags & (byte)DataFrameFlags.Padded) != 0)
                        {
                            if(frame.Data[0] + 1 > frame.Data.Length)
                            {
                                m_Conn.SendGoAway(Http2ErrorCode.ProtocolError);
                                throw new ProtocolErrorException();
                            }
                            m_AvailableReceiveDataBytes = frame.Data.Length - frame.Data[0];
                            m_ConsumedReceiveDataBytes = 1;
                        }
                        else
                        {
                            m_AvailableReceiveDataBytes = frame.Data.Length;
                            m_ConsumedReceiveDataBytes = 0;
                        }
                        m_BufferedReceiveData = frame.Data;
                    }
                    else
                    {
                        int consumebytes = count;
                        if(consumebytes > m_AvailableReceiveDataBytes - m_ConsumedReceiveDataBytes)
                        {
                            consumebytes = m_AvailableReceiveDataBytes - m_ConsumedReceiveDataBytes;
                        }
                        Buffer.BlockCopy(m_BufferedReceiveData, m_ConsumedReceiveDataBytes, buffer, offset, consumebytes);
                        offset += consumebytes;
                        m_ConsumedReceiveDataBytes += consumebytes;
                        consumed += consumebytes;
                    }
                }

                return consumed;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                if(!m_HaveSentHeaders)
                {
                    m_Conn.SendRstStream(m_StreamIdentifier, Http2ErrorCode.ProtocolError);
                }

                while(count > 0)
                {
                    int remaining = m_MaxTransmitDataBytes - m_BufferedTransmitDataBytes;
                    if(remaining == 0)
                    {
                        m_Conn.SendFrame(Http2FrameType.Data, 0, m_StreamIdentifier, buffer, offset, count);
                        m_BufferedTransmitDataBytes = 0;
                        remaining = m_MaxTransmitDataBytes;
                    }
                    if(remaining > count)
                    {
                        remaining = count;
                    }
                    Buffer.BlockCopy(buffer, offset, m_BufferedTransmitData, m_BufferedTransmitDataBytes, remaining);
                    m_BufferedTransmitDataBytes += remaining;
                    offset += remaining;
                    count -= remaining;
                }
            }

            public void SendHeaders(Dictionary<string, string> headers, bool eos = false)
            {
                byte[] data = BuildHeaderData(headers);
                int offset = 0;
                var type = Http2FrameType.Headers;
                while(data.Length - offset > 16384)
                {
                    m_Conn.SendFrame(type, 0, m_StreamIdentifier, data, offset, 16384);
                    offset += 16384;
                    type = Http2FrameType.Continuation;
                }
                if (data.Length - offset <= 16384)
                {
                    var hf = (byte)HeadersFrameFlags.EndHeaders;
                    if(eos)
                    {
                        hf |= (byte)HeadersFrameFlags.EndStream;
                    }
                    m_Conn.SendFrame(type, hf, m_StreamIdentifier, data, offset, data.Length - offset);
                }

                m_HaveSentHeaders = true;
                m_HaveSentEoS = eos;
            }

            private byte[] BuildHeaderData(Dictionary<string, string> headers)
            {
                var res = new List<byte>();
                foreach(KeyValuePair<string, string> kvp in headers)
                {
                    string name = kvp.Key.ToLowerInvariant();
                    switch(name)
                    {
                        case ":authority":
                            AppendInteger(res, LITERAL_HEADER_FIELD_NEVER_INDEXED, 4, 1);
                            AppendString(res, kvp.Value);
                            continue;

                        case ":method":
                            if (kvp.Value == "GET")
                            {
                                AppendInteger(res, INDEX_HEADER_FIELD, 7, 2);
                            }
                            else if (kvp.Value == "POST")
                            {
                                AppendInteger(res, INDEX_HEADER_FIELD, 7, 3);
                            }
                            else
                            {
                                break;
                            }
                            continue;

                        case ":path":
                            if (kvp.Value == "/")
                            {
                                AppendInteger(res, INDEX_HEADER_FIELD, 7, 4);
                            }
                            else if (kvp.Value == "/index.html")
                            {
                                AppendInteger(res, INDEX_HEADER_FIELD, 7, 5);
                            }
                            else
                            {
                                break;
                            }
                            continue;

                        case ":scheme":
                            if (kvp.Value == "http")
                            {
                                AppendInteger(res, INDEX_HEADER_FIELD, 7, 6);
                            }
                            else if (kvp.Value == "https")
                            {
                                AppendInteger(res, INDEX_HEADER_FIELD, 7, 7);
                            }
                            else
                            {
                                break;
                            }
                            continue;

                        case ":status":
                            if (kvp.Value == "200")
                            {
                                AppendInteger(res, INDEX_HEADER_FIELD, 7, 8);
                            }
                            else if (kvp.Value == "204")
                            {
                                AppendInteger(res, INDEX_HEADER_FIELD, 7, 9);
                            }
                            else if (kvp.Value == "206")
                            {
                                AppendInteger(res, INDEX_HEADER_FIELD, 7, 10);
                            }
                            else if (kvp.Value == "304")
                            {
                                AppendInteger(res, INDEX_HEADER_FIELD, 7, 11);
                            }
                            else if (kvp.Value == "400")
                            {
                                AppendInteger(res, INDEX_HEADER_FIELD, 7, 12);
                            }
                            else if (kvp.Value == "404")
                            {
                                AppendInteger(res, INDEX_HEADER_FIELD, 7, 13);
                            }
                            else if (kvp.Value == "500")
                            {
                                AppendInteger(res, INDEX_HEADER_FIELD, 7, 14);
                            }
                            else
                            {
                                break;
                            }
                            continue;

                        case "accept-encoding":
                            if(kvp.Value == "gzip, deflate" || kvp.Value == "gzip,deflate")
                            {
                                AppendInteger(res, INDEX_HEADER_FIELD, 7, 16);
                                continue;
                            }
                            break;
                    }

                    uint index;
                    if (FieldNamesToIndex.TryGetValue(name, out index))
                    {
                        AppendInteger(res, LITERAL_HEADER_FIELD_NEVER_INDEXED, 4, index);
                        AppendString(res, kvp.Value);
                    }
                    else
                    {
                        AppendInteger(res, LITERAL_HEADER_FIELD_NEVER_INDEXED, 4, 0);
                        AppendString(res, kvp.Key);
                        AppendString(res, kvp.Value);
                    }
                    break;
                }
                return res.ToArray();
            }

            private static readonly Dictionary<string, uint> FieldNamesToIndex = new Dictionary<string, uint>
            {
                [":authority"] = 1,
                ["accept-charset"] = 15,
                ["accept-language"] = 17,
                ["accept-ranges"] = 18,
                ["accept"] = 19,
                ["access-control-allow-origin"] = 20,
                ["age"] = 21,
                ["allow"] = 22,
                ["authorization"] = 23,
                ["cache-control"] = 24,
                ["content-disposition"] = 25,
                ["content-encoding"] = 26,
                ["content-language"] = 27,
                ["content-length"] = 28,
                ["content-location"] = 29,
                ["content-range"] = 30,
                ["content-type"] = 31,
                ["cookie"] = 32,
                ["date"] = 33,
                ["etag"] = 34,
                ["expect"] = 35,
                ["expires"] = 36,
                ["from"] = 37,
                ["host"] = 38,
                ["if-match"] = 39,
                ["if-modified-since"] = 40,
                ["if-none-match"] = 41,
                ["if-range"] = 42,
                ["if-unmodified-since"] = 43,
                ["last-modified"] = 44,
                ["link"] = 45,
                ["location"] = 46,
                ["max-forwards"] = 47,
                ["proxy-authenticate"] = 48,
                ["proxy-authorization"] = 49,
                ["range"] = 50,
                ["referer"] = 51,
                ["refresh"] = 52,
                ["retry-after"] = 53,
                ["server"] = 54,
                ["set-cookie"] = 55,
                ["strict-transport-security"] = 56,
                ["transfer-encoding"] = 57,
                ["user-agent"] = 58,
                ["vary"] = 59,
                ["via"] = 60,
                ["www-authenticate"] = 61
            };

            private const byte INDEX_HEADER_FIELD = 0x80; /* N=7 */
            private const byte LITERAL_HEADER_WITH_INC_INDEX = 0x40; /* N=6 */
            private const byte LITERAL_HEADER_FIELD = 0x00; /* N=4 */
            private const byte LITERAL_HEADER_FIELD_NEVER_INDEXED = 0x10; /* N=4 */

            private static readonly UTF8Encoding UTF8NoBOM = new UTF8Encoding(false);

            private void AppendString(List<byte> res, string s)
            {
                byte[] strdata = UTF8NoBOM.GetBytes(s);
                AppendInteger(res, 0, 7, (uint)strdata.Length);
                res.AddRange(strdata);
            }

            private void AppendInteger(List<byte> res, byte firstbyte, int nbits, uint value)
            {
                int maxprefix = (1 << nbits) - 1;
                if (value < maxprefix)
                {
                    res.Add((byte)(firstbyte | value));
                }
                else
                {
                    res.Add((byte)(firstbyte | maxprefix));
                    var i = (uint)(value - maxprefix);
                    while(i >= 128)
                    {
                        res.Add((byte)((i & 0x7F) | 0x80));
                        i >>= 7;
                    }
                }
            }

            public Dictionary<string, string> ReceiveHeaders()
            {
                if(m_HaveReceivedHeaders)
                {
                    m_Conn.SendRstStream(m_StreamIdentifier, Http2ErrorCode.ProtocolError);
                    throw new StreamErrorException();
                }
                Http2Frame frame = m_Http2Queue.Dequeue();
                if(frame.Type != Http2FrameType.Headers)
                {
                    m_Conn.SendRstStream(m_StreamIdentifier, Http2ErrorCode.ProtocolError);
                    throw new StreamErrorException();
                }

                while((frame.Flags & (byte)HeadersFrameFlags.EndHeaders) == 0)
                {
                    if((frame.Flags & (byte)HeadersFrameFlags.EndStream) != 0)
                    {
                        m_Conn.SendRstStream(m_StreamIdentifier, Http2ErrorCode.ProtocolError);
                        throw new StreamErrorException();
                    }
                    frame = m_Http2Queue.Dequeue();
                    if(frame.Type != Http2FrameType.Continuation)
                    {
                        throw new StreamErrorException();
                    }
                }

                if((frame.Flags & (byte)HeadersFrameFlags.EndStream) != 0)
                {
                    m_HaveReceivedEoS = true;
                }

                m_HaveReceivedHeaders = true;

                throw new NotImplementedException();
            }
        }
    }
}
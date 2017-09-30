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
        private uint m_MaxTableByteSize = 4096;
        private uint m_InitialWindowSize = 65536;

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
            var settings = new List<byte>();
            SendFrame(Http2FrameType.Settings, 0, 0, settings.ToArray());
        }

        private void AddSettingsData(List<byte> data, ushort paraid, uint paravalue)
        {
            data.Add((byte)((paraid >> 8) & 0xFF));
            data.Add((byte)((paraid >> 0) & 0xFF));
            data.Add((byte)((paravalue >> 24) & 0xFF));
            data.Add((byte)((paravalue >> 16) & 0xFF));
            data.Add((byte)((paravalue >> 8) & 0xFF));
            data.Add((byte)((paravalue >> 0) & 0xFF));
        }

        public void Dispose()
        {
            if (Interlocked.Decrement(ref m_ActiveUsers) == 0)
            {
                SendGoAway(Http2ErrorCode.NoError);
                m_OriginalStream?.Dispose();
            }
        }

        public enum Http2ErrorCode
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

        internal void SendFrame(Http2FrameType type, byte flags, uint streamid, byte[] data) =>
            SendFrame(type, flags, streamid, data, 0, data.Length);

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

        private readonly object m_ClientStreamIdentifierLock = new object();
        uint m_ClientStreamIdentifier = 1;

        public Http2Stream OpenClientStream()
        {
            uint streamid;
            lock(m_ClientStreamIdentifierLock)
            {
                streamid = m_ClientStreamIdentifier;
                m_ClientStreamIdentifier = (m_ClientStreamIdentifier + 2) & 0x7FFFFFFF;
            }
            return new Http2Stream(this, streamid, m_MaxTableByteSize, m_InitialWindowSize);
        }

        public Http2Stream UpgradeStream(byte[] settings, bool hasPost)
        {
            uint value;
            if (TryGetSettingsValue(settings, SETTINGS_HEADER_TABLE_SIZE, out value))
            {
                m_MaxTableByteSize = value;
            }
            if (TryGetSettingsValue(settings, SETTINGS_INITIAL_WINDOW_SIZE, out value))
            {
                m_InitialWindowSize = value;
            }

            uint streamid;
            lock (m_ClientStreamIdentifierLock)
            {
                streamid = m_ClientStreamIdentifier;
                m_ClientStreamIdentifier = (m_ClientStreamIdentifier + 2) & 0x7FFFFFFF;
            }
            return new Http2Stream(this, streamid, m_MaxTableByteSize, m_InitialWindowSize, hasPost);
        }

        public void Run(Action<Http2Stream> action = null)
        {
            Interlocked.Increment(ref m_ActiveUsers);
            for(; ;)
            {
                Http2Frame frame = ReceiveFrame();

                if (frame.StreamIdentifier == 0)
                {
                    uint value;
                    switch (frame.Type)
                    {
                        case Http2FrameType.Settings:
                            if ((frame.Flags & (byte)SettingsFrameFlags.Ack) == 0)
                            {
                                if (TryGetSettingsValue(frame.Data, SETTINGS_HEADER_TABLE_SIZE, out value))
                                {
                                    m_MaxTableByteSize = value;
                                }
                                if(TryGetSettingsValue(frame.Data, SETTINGS_INITIAL_WINDOW_SIZE, out value))
                                {
                                    m_InitialWindowSize = value;
                                }
                                foreach (Http2Stream s in m_Streams.Values)
                                {
                                    /* pass settings through */
                                    s.NewFrameReceived(frame);
                                }
                            }
                            SendFrame(Http2FrameType.Settings, (byte)SettingsFrameFlags.Ack, 0, new byte[0]);
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
                            if (action == null)
                            {
                                SendRstStream(frame.StreamIdentifier, Http2ErrorCode.RefusedStream);
                            }
                            else
                            {
                                if (!m_Streams.TryGetValue(frame.StreamIdentifier, out stream))
                                {
                                    stream = new Http2Stream(this, frame.StreamIdentifier, m_MaxTableByteSize, m_InitialWindowSize);
                                    m_Streams[frame.StreamIdentifier] = stream;
                                }
                                bool startStream = !stream.HaveReceivedHeaders;
                                stream.NewFrameReceived(frame);
                                if(startStream && stream.HaveReceivedHeaders)
                                {
                                    action(stream);
                                }
                            }
                            break;

                        case Http2FrameType.Priority:
                            if (!m_Streams.ContainsKey(frame.StreamIdentifier))
                            {
                                SendRstStream(frame.StreamIdentifier, Http2ErrorCode.ProtocolError);
                            }
                            break;

                        case Http2FrameType.Data:
                        case Http2FrameType.WindowUpdate:
                        case Http2FrameType.Continuation:
                            if(frame.Type == Http2FrameType.Data)
                            {

                            }
                            if(frame.Type == Http2FrameType.WindowUpdate && frame.Data.Length != 4)
                            {
                                SendGoAway(Http2ErrorCode.FrameSizeError);
                                throw new ProtocolErrorException();
                            }
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

        internal const ushort SETTINGS_HEADER_TABLE_SIZE = 1;
        internal const ushort SETTINGS_ENABLE_PUSH = 2;
        internal const ushort SETTINGS_MAX_CONCURRENT_STREAMS = 3;
        internal const ushort SETTINGS_INITIAL_WINDOW_SIZE = 4;
        internal const ushort SETTINGS_MAX_FRAME_SIZE = 5;
        internal const ushort SETTINGS_MAX_HEADER_LIST_SIZE = 6;

        internal static bool TryGetSettingsValue(byte[] settingsdata, ushort param, out uint val)
        {
            int offset = 0;
            while(offset + 5 < settingsdata.Length)
            {
                var paraid = (ushort)((settingsdata[offset] << 8) | settingsdata[offset + 1]);
                offset += 2;
                if(paraid == param)
                {
                    val = (uint)((settingsdata[offset] << 24) | (settingsdata[offset + 1] << 16) | (settingsdata[offset + 2] << 8) | settingsdata[offset + 3]);
                    return true;
                }
                offset += 4;
            }
            val = 0;
            return false;
        }

        public class Http2Stream : Stream
        {
            private BlockingQueue<Http2Frame> m_Http2Queue = new BlockingQueue<Http2Frame>();
            private BlockingQueue<Http2Frame> m_Http2WindowUpdateQueue = new BlockingQueue<Http2Frame>();
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
            private readonly List<KeyValuePair<byte[], byte[]>> m_RxDynamicTable = new List<KeyValuePair<byte[], byte[]>>();
            private uint m_MaxTableByteSize;
            private uint m_WindowSize;

            public Http2Stream(Http2Connection conn, uint streamid, uint maxtablebytesize, uint windowsize)
            {
                m_Conn = conn;
                m_StreamIdentifier = streamid;
                m_MaxTableByteSize = maxtablebytesize;
                m_WindowSize = windowsize;
                Interlocked.Increment(ref conn.m_ActiveUsers);
            }

            public Http2Stream(Http2Connection conn, uint streamid, uint maxtablebytesize, uint windowsize, bool havepost)
            {
                m_Conn = conn;
                m_StreamIdentifier = streamid;
                m_MaxTableByteSize = maxtablebytesize;
                m_WindowSize = windowsize;
                m_HaveReceivedHeaders = true;
                m_HaveReceivedEoS = !havepost;
                Interlocked.Increment(ref conn.m_ActiveUsers);
            }

            ~Http2Stream()
            {
                m_Conn.Dispose();
            }

            public bool HaveReceivedEoS => m_HaveReceivedEoS;
            public bool HaveReceivedHeaders => m_HaveReceivedHeaders;

            private void ChangeTableSize(int maxtablebytesize)
            {
                if(maxtablebytesize == 0)
                {
                    m_RxDynamicTable.Clear();
                }

                int startevict = 0;
                int curtablesize = 0;
                while(startevict < m_RxDynamicTable.Count)
                {
                    KeyValuePair<byte[], byte[]> entry = m_RxDynamicTable[startevict];
                    int entrysize = 32 + entry.Key.Length + entry.Value.Length;
                    if(entrysize + curtablesize > maxtablebytesize)
                    {
                        break;
                    }
                    curtablesize += entrysize;
                }

                if (startevict < m_RxDynamicTable.Count)
                {
                    m_RxDynamicTable.RemoveRange(startevict, m_RxDynamicTable.Count - startevict);
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (!m_HaveSentEoS)
                {
                    m_Conn.SendFrame(Http2FrameType.Data, (byte)DataFrameFlags.EndStream, m_StreamIdentifier, m_BufferedTransmitData, 0, m_BufferedTransmitDataBytes);
                }
                m_Conn.Dispose();
            }

            internal void NewFrameReceived(Http2Frame frame)
            {
                if (frame.Type == Http2FrameType.WindowUpdate)
                {
                    m_Http2WindowUpdateQueue.Enqueue(frame);
                }
                else
                {
                    m_Http2Queue.Enqueue(frame);
                }
            }

            public override bool CanRead => true;

            public override bool CanSeek => false;

            public override bool CanWrite => true;

            #region Unsupported methods
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

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }
            #endregion

            public override void Flush()
            {
            }

            public void SendRstStream(Http2ErrorCode errorcode)
            {
                m_Conn.SendRstStream(m_StreamIdentifier, errorcode);
            }

            public void SendEndOfStream()
            {
                if (!m_HaveSentEoS)
                {
                    m_HaveSentEoS = true;
                    m_Conn.SendFrame(Http2FrameType.Data, (byte)DataFrameFlags.EndStream, m_StreamIdentifier, m_BufferedTransmitData, 0, m_BufferedTransmitDataBytes);
                }
            }

            #region Body data access
            public override int Read(byte[] buffer, int offset, int count)
            {
                int consumed = 0;
                if (!m_HaveReceivedHeaders)
                {
                    m_Conn.SendRstStream(m_StreamIdentifier, Http2ErrorCode.ProtocolError);
                    throw new StreamErrorException();
                }

                while (count > 0)
                {
                    if (m_ConsumedReceiveDataBytes == m_AvailableReceiveDataBytes)
                    {
                        if (m_HaveReceivedEoS)
                        {
                            return 0;
                        }

                        Http2Frame frame = m_Http2Queue.Dequeue();
                        uint value;
                        m_Conn.SendFrame(Http2FrameType.WindowUpdate, 0, m_StreamIdentifier, new byte[] { 0, 0x1, 0, 0 });
                        if (TryGetSettingsValue(frame.Data, SETTINGS_HEADER_TABLE_SIZE, out value))
                        {
                            m_MaxTableByteSize = value;
                            ChangeTableSize((int)m_MaxTableByteSize);
                        }
                        else if (frame.Type == Http2FrameType.Headers)
                        {
                            /* ignore end headers */
                        }
                        else if (frame.Type != Http2FrameType.Data)
                        {
                            m_Conn.SendRstStream(m_StreamIdentifier, Http2ErrorCode.ProtocolError);
                            throw new StreamErrorException();
                        }
                        if ((frame.Flags & (byte)DataFrameFlags.EndStream) != 0)
                        {
                            m_HaveReceivedEoS = true;
                        }

                        if ((frame.Flags & (byte)DataFrameFlags.Padded) != 0)
                        {
                            if (frame.Data[0] + 1 > frame.Data.Length)
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
                        if (consumebytes > m_AvailableReceiveDataBytes - m_ConsumedReceiveDataBytes)
                        {
                            consumebytes = m_AvailableReceiveDataBytes - m_ConsumedReceiveDataBytes;
                        }
                        Buffer.BlockCopy(m_BufferedReceiveData, m_ConsumedReceiveDataBytes, buffer, offset, consumebytes);
                        offset += consumebytes;
                        m_ConsumedReceiveDataBytes += consumebytes;
                        consumed += consumebytes;
                        count -= consumebytes;
                    }
                }

                return consumed;
            }

            private uint GetWindowSize(Http2Frame frame) =>
                 (uint)(((frame.Data[0] & 0x7F) << 24) |
                    (frame.Data[1] << 16) |
                    (frame.Data[2] << 8) |
                    frame.Data[3]);

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (!m_HaveSentHeaders)
                {
                    m_Conn.SendRstStream(m_StreamIdentifier, Http2ErrorCode.ProtocolError);
                }

                while (count > 0)
                {
                    if(m_Http2WindowUpdateQueue.Count != 0)
                    {
                        Http2Frame frame = m_Http2WindowUpdateQueue.Dequeue();
                        m_WindowSize = GetWindowSize(frame);
                    }
                    int remaining = m_MaxTransmitDataBytes - m_BufferedTransmitDataBytes;
                    while(m_WindowSize == 0)
                    {
                        Http2Frame frame = m_Http2WindowUpdateQueue.Dequeue();
                        m_WindowSize = GetWindowSize(frame);
                    }
                    /* prevent stalls due to inconsistent settings */
                    if(remaining > m_WindowSize)
                    {
                        remaining = (int)m_WindowSize;
                    }
                    if (remaining == 0)
                    {
                        m_Conn.SendFrame(Http2FrameType.Data, 0, m_StreamIdentifier, m_BufferedTransmitData, 0, m_BufferedTransmitDataBytes);
                        m_BufferedTransmitDataBytes = 0;
                        remaining = m_MaxTransmitDataBytes;
                        m_WindowSize -= (uint)m_BufferedTransmitDataBytes;
                    }
                    if (remaining > count)
                    {
                        remaining = count;
                    }
                    Buffer.BlockCopy(buffer, offset, m_BufferedTransmitData, m_BufferedTransmitDataBytes, remaining);
                    m_BufferedTransmitDataBytes += remaining;
                    offset += remaining;
                    count -= remaining;
                }
            }
            #endregion

            #region Send HTTP headers
            public void SendHeaders(Dictionary<string, string> headers, bool eos = false)
            {
                byte[] data = BuildHeaderData(headers);
                int offset = 0;
                var type = Http2FrameType.Headers;
                while (data.Length - offset > 16384)
                {
                    m_Conn.SendFrame(type, 0, m_StreamIdentifier, data, offset, 16384);
                    offset += 16384;
                    type = Http2FrameType.Continuation;
                }
                if (data.Length - offset <= 16384)
                {
                    var hf = (byte)HeadersFrameFlags.EndHeaders;
                    if (eos)
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
                foreach (KeyValuePair<string, string> kvp in headers)
                {
                    string name = kvp.Key.ToLowerInvariant();
                    switch (name)
                    {
                        case ":authority":
                            AppendInteger(res, LITERAL_HEADER_FIELD, 4, 1);
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
                            if (kvp.Value == "gzip, deflate" || kvp.Value == "gzip,deflate")
                            {
                                AppendInteger(res, INDEX_HEADER_FIELD, 7, 16);
                                continue;
                            }
                            break;
                    }

                    uint index;
                    if (m_FieldNamesToIndex.TryGetValue(name, out index))
                    {
                        AppendInteger(res, LITERAL_HEADER_FIELD, 4, index);
                        AppendString(res, kvp.Value);
                    }
                    else
                    {
                        AppendInteger(res, LITERAL_HEADER_FIELD, 4, 0);
                        AppendString(res, kvp.Key);
                        AppendString(res, kvp.Value);
                    }
                    break;
                }
                return res.ToArray();
            }

            private static readonly Dictionary<string, uint> m_FieldNamesToIndex = new Dictionary<string, uint>
            {
                [":authority"] = 1,
                [":method"] = 2,
                [":path"] = 4,
                [":scheme"] = 6,
                [":status"] = 8,
                ["accept-charset"] = 15,
                ["accept-encoding"] = 16,
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
                    while (i >= 128)
                    {
                        res.Add((byte)((i & 0x7F) | 0x80));
                        i >>= 7;
                    }
                }
            }
            #endregion

            #region HTTP Header Decoding
            private readonly List<byte> m_HeaderRxBuf = new List<byte>();

            private static readonly KeyValuePair<string, string>[] m_RxStaticTable = new KeyValuePair<string, string>[]
            {
                new KeyValuePair<string, string>(),
                /* 1-10 */
                new KeyValuePair<string, string>(":authority", string.Empty),
                new KeyValuePair<string, string>(":method", "GET"),
                new KeyValuePair<string, string>(":method", "POST"),
                new KeyValuePair<string, string>(":path", "/"),
                new KeyValuePair<string, string>(":path", "/index.html"),
                new KeyValuePair<string, string>(":scheme", "http"),
                new KeyValuePair<string, string>(":scheme", "https"),
                new KeyValuePair<string, string>(":status", "200"),
                new KeyValuePair<string, string>(":status", "204"),
                new KeyValuePair<string, string>(":status", "206"),
                /* 11-20 */
                new KeyValuePair<string, string>(":status", "304"),
                new KeyValuePair<string, string>(":status", "400"),
                new KeyValuePair<string, string>(":status", "404"),
                new KeyValuePair<string, string>(":status", "500"),
                new KeyValuePair<string, string>("accept-charset", string.Empty),
                new KeyValuePair<string, string>("accept-encoding", "gzip, deflate"),
                new KeyValuePair<string, string>("accept-language", string.Empty),
                new KeyValuePair<string, string>("accept-ranges", string.Empty),
                new KeyValuePair<string, string>("accept", string.Empty),
                new KeyValuePair<string, string>("access-control-allow-origin", string.Empty),
                /* 21-30 */
                new KeyValuePair<string, string>("age", string.Empty),
                new KeyValuePair<string, string>("allow", string.Empty),
                new KeyValuePair<string, string>("authoriztation", string.Empty),
                new KeyValuePair<string, string>("cache-control", string.Empty),
                new KeyValuePair<string, string>("content-disposition", string.Empty),
                new KeyValuePair<string, string>("content-encoding", string.Empty),
                new KeyValuePair<string, string>("content-language", string.Empty),
                new KeyValuePair<string, string>("content-length", string.Empty),
                new KeyValuePair<string, string>("content-location", string.Empty),
                new KeyValuePair<string, string>("content-range", string.Empty),
                /* 31-40 */
                new KeyValuePair<string, string>("content-type", string.Empty),
                new KeyValuePair<string, string>("cookie", string.Empty),
                new KeyValuePair<string, string>("date", string.Empty),
                new KeyValuePair<string, string>("etag", string.Empty),
                new KeyValuePair<string, string>("expect", string.Empty),
                new KeyValuePair<string, string>("expires", string.Empty),
                new KeyValuePair<string, string>("from", string.Empty),
                new KeyValuePair<string, string>("host", string.Empty),
                new KeyValuePair<string, string>("if-match", string.Empty),
                new KeyValuePair<string, string>("if-modified-since", string.Empty),
                /* 41-50 */
                new KeyValuePair<string, string>("if-none-match", string.Empty),
                new KeyValuePair<string, string>("if-range", string.Empty),
                new KeyValuePair<string, string>("if-unmodified-since", string.Empty),
                new KeyValuePair<string, string>("last-modified", string.Empty),
                new KeyValuePair<string, string>("link", string.Empty),
                new KeyValuePair<string, string>("location", string.Empty),
                new KeyValuePair<string, string>("max-forwards", string.Empty),
                new KeyValuePair<string, string>("proxy-authenticate", string.Empty),
                new KeyValuePair<string, string>("proxy-authorization", string.Empty),
                new KeyValuePair<string, string>("range", string.Empty),
                /* 51-60 */
                new KeyValuePair<string, string>("referer", string.Empty),
                new KeyValuePair<string, string>("refresh", string.Empty),
                new KeyValuePair<string, string>("retry-after", string.Empty),
                new KeyValuePair<string, string>("server", string.Empty),
                new KeyValuePair<string, string>("set-cookie", string.Empty),
                new KeyValuePair<string, string>("strict-transport-security", string.Empty),
                new KeyValuePair<string, string>("transfer-encoding", string.Empty),
                new KeyValuePair<string, string>("user-agent", string.Empty),
                new KeyValuePair<string, string>("vary", string.Empty),
                new KeyValuePair<string, string>("via", string.Empty),
                /* 61 */
                new KeyValuePair<string, string>("www-authenticate", string.Empty),
            };

            private bool TryGetNameFromTable(int index, out byte[] name)
            {
                name = null;
                if (index >= m_RxStaticTable.Length)
                {
                    index -= m_RxStaticTable.Length;
                    if (m_RxDynamicTable.Count <= index)
                    {
                        return false;
                    }
                    KeyValuePair<byte[], byte[]> bdat = m_RxDynamicTable[index];
                    name = bdat.Key;
                    return true;
                }
                else
                {
                    name = UTF8NoBOM.GetBytes(m_RxStaticTable[index].Key);
                    return true;
                }
            }

            private void DecodeHeaderStream(byte[] data, bool ispadded, Dictionary<string, string> headers)
            {
                int offset = 0;
                int length = data.Length;
                if (ispadded)
                {
                    if (data.Length == 0)
                    {
                        m_Conn.SendRstStream(m_StreamIdentifier, Http2ErrorCode.ProtocolError);
                        throw new StreamErrorException();
                    }
                    ++offset;
                    if (offset + data[0] > data.Length)
                    {
                        m_Conn.SendRstStream(m_StreamIdentifier, Http2ErrorCode.ProtocolError);
                        throw new StreamErrorException();
                    }
                    length = data.Length - data[0] - 1;
                    while(offset < length)
                    {
                        m_HeaderRxBuf.Add(data[offset++]);
                    }
                }
                else
                {
                    m_HeaderRxBuf.AddRange(data);
                }

                while (m_HeaderRxBuf.Count > 0)
                {
                    byte cmd = m_HeaderRxBuf[0];
                    offset = 1;
                    if((cmd & 0x80) != 0)
                    {
                        int value;
                        if(!TryGetInteger(ref offset, 7, out value))
                        {
                            break;
                        }
                        KeyValuePair<string, string> entry;
                        if(value >= m_RxStaticTable.Length)
                        {
                            value -= m_RxStaticTable.Length;
                            if(m_RxDynamicTable.Count <= value)
                            {
                                m_Conn.SendRstStream(m_StreamIdentifier, Http2ErrorCode.ProtocolError);
                                throw new StreamErrorException();
                            }
                            KeyValuePair<byte[], byte[]> bdat = m_RxDynamicTable[(int)value];
                            entry = new KeyValuePair<string, string>(
                                UTF8NoBOM.GetString(bdat.Key),
                                UTF8NoBOM.GetString(bdat.Value));
                        }
                        else
                        {
                            entry = m_RxStaticTable[value];
                        }
                        headers[entry.Key] = entry.Value;
                        m_HeaderRxBuf.RemoveRange(0, offset);
                    }
                    else if((cmd & 0xC0) == 0x40)
                    {
                        /* Literal header field with incremental indexing */
                        int index;
                        if(!TryGetInteger(ref offset, 6, out index))
                        {
                            break;
                        }
                        byte[] name;
                        byte[] value;

                        if(!TryGetName(ref offset, index, out name))
                        {
                            break;
                        }

                        if(!TryGetLiteral(ref offset, out value))
                        {
                            break;
                        }

                        m_RxDynamicTable.Insert(0, new KeyValuePair<byte[], byte[]>(name, value));
                        headers[UTF8NoBOM.GetString(name)] = UTF8NoBOM.GetString(value);
                        m_HeaderRxBuf.RemoveRange(0, offset);
                    }
                    else if((cmd & 0xE0) == 0x00)
                    {
                        /* Literal Header field without indexing */
                        /* Literal Header field never indexed */

                        int index;
                        if (!TryGetInteger(ref offset, 6, out index))
                        {
                            break;
                        }
                        byte[] name;
                        byte[] value;

                        if (!TryGetName(ref offset, index, out name))
                        {
                            break;
                        }

                        if (!TryGetLiteral(ref offset, out value))
                        {
                            break;
                        }

                        headers[UTF8NoBOM.GetString(name)] = UTF8NoBOM.GetString(value);
                        m_HeaderRxBuf.RemoveRange(0, offset);
                    }
                    else if ((cmd & 0xE0) == 0x20)
                    {
                        /* Dynamic Table Size update */
                        int newtablesize;
                        if(!TryGetInteger(ref offset, 5, out newtablesize))
                        {
                            break;
                        }
                        if(newtablesize > m_MaxTableByteSize)
                        {
                            m_Conn.SendRstStream(m_StreamIdentifier, Http2ErrorCode.CompressionError);
                            throw new StreamErrorException();
                        }
                        ChangeTableSize(newtablesize);
                    }
                }
            }

            private bool TryGetLiteral(ref int offset, out byte[] literal)
            {
                literal = null;
                byte valuetype = m_HeaderRxBuf[offset];
                int valuelen;
                if (!TryGetInteger(ref offset, 7, out valuelen))
                {
                    return false;
                }
                if (offset + valuelen < m_HeaderRxBuf.Count)
                {
                    return false;
                }

                literal = new byte[valuelen];
                Buffer.BlockCopy(m_HeaderRxBuf.GetRange(offset, valuelen).ToArray(), 0, literal, 0, valuelen);
                offset += valuelen;

                if ((valuetype & 0x80) != 0 && !TryDecodeHuffman(literal, out literal))
                {
                    m_Conn.SendRstStream(m_StreamIdentifier, Http2ErrorCode.CompressionError);
                    throw new StreamErrorException();
                }
                return true;
            }

            private bool TryGetName(ref int offset, int index, out byte[] name) =>
                (index != 0) ? TryGetNameFromTable(index, out name) : TryGetLiteral(ref offset, out name);

            private bool TryGetInteger(ref int offset, int nbits, out int value)
            {
                byte b = m_HeaderRxBuf[offset++];
                var bmask = (byte)((1 << nbits) - 1);
                if((b & bmask) != bmask)
                {
                    value = b & bmask;
                }
                else
                {
                    int m = 0;
                    value = b & bmask;
                    do
                    {
                        b = m_HeaderRxBuf[offset++];
                        value += ((b & 0x7F) << m);
                        m += 7;
                    } while ((b & 128) != 0);
                }
                return true;
            }

            public Dictionary<string, string> ReceiveHeaders()
            {
                var headers = new Dictionary<string, string>();
                if (m_HaveReceivedHeaders)
                {
                    m_Conn.SendRstStream(m_StreamIdentifier, Http2ErrorCode.ProtocolError);
                    throw new StreamErrorException();
                }
                Http2Frame frame = m_Http2Queue.Dequeue();
                if (frame.Type != Http2FrameType.Headers)
                {
                    m_Conn.SendRstStream(m_StreamIdentifier, Http2ErrorCode.ProtocolError);
                    throw new StreamErrorException();
                }

                DecodeHeaderStream(frame.Data, (frame.Flags & (byte)HeadersFrameFlags.Padded) != 0, headers);

                while ((frame.Flags & (byte)HeadersFrameFlags.EndHeaders) == 0)
                {
                    if ((frame.Flags & (byte)HeadersFrameFlags.EndStream) != 0)
                    {
                        m_Conn.SendRstStream(m_StreamIdentifier, Http2ErrorCode.ProtocolError);
                        throw new StreamErrorException();
                    }
                    do
                    {
                        frame = m_Http2Queue.Dequeue();
                        if (frame.Type == Http2FrameType.Settings)
                        {
                            uint value;
                            if (TryGetSettingsValue(frame.Data, SETTINGS_HEADER_TABLE_SIZE, out value))
                            {
                                m_MaxTableByteSize = value;
                                ChangeTableSize((int)m_MaxTableByteSize);
                            }
                        }
                    } while (frame.Type == Http2FrameType.Settings);

                    if (frame.Type != Http2FrameType.Continuation)
                    {
                        throw new StreamErrorException();
                    }

                    DecodeHeaderStream(frame.Data, false, headers);
                }

                if ((frame.Flags & (byte)HeadersFrameFlags.EndStream) != 0)
                {
                    m_HaveReceivedEoS = true;
                }

                m_HaveReceivedHeaders = true;

                throw new NotImplementedException();
            }
            #endregion

            #region Static Huffman Coding
            private static bool TryDecodeHuffman(byte[] input, out byte[] output)
            {
                var outputdata = new List<byte>();
                output = null;
                int codestream = 0;
                int codelength = 0;

                foreach(byte c in input)
                {
                    for(int bit = 8; bit-- != 0; )
                    {
                        codestream <<= 1;
                        if((c & (1 << bit)) != 0)
                        {
                            codestream |= 1;
                        }
                        ++codelength;

                        if(codelength > 31)
                        {
                            return false;
                        }

                        Dictionary<int, byte> bitcodes;
                        if(!m_DecodeHuffmanTable.TryGetValue(codelength, out bitcodes))
                        {
                            continue;
                        }

                        byte b;
                        if(bitcodes.TryGetValue(codestream, out b))
                        {
                            outputdata.Add(b);
                            codestream = 0;
                            codelength = 0;
                        }
                    }
                }

                /* last incomplete segment must be less than 8 bits and all read bits being set */
                if(codelength > 7 || codestream != (1 << codelength) - 1)
                {
                    return false;
                }

                output = outputdata.ToArray();
                return true;
            }

            private static byte[] EncodeHuffman(byte[] input)
            {
                var res = new List<byte>();
                byte buf = 0;
                byte bmask = 0x80;

                foreach(byte c in input)
                {
                    HuffmanCode code = m_EncodeHuffmanTable[c];
                    int codestream = code.Code;

                    for(int bit = code.BitLength; bit-- != 0;)
                    {
                        if ((codestream & (1 << bit)) != 0)
                        {
                            buf |= bmask;
                        }
                        bmask >>= 1;
                        if(bmask == 0)
                        {
                            res.Add(buf);
                            bmask = 0x80;
                        }
                    }
                }

                /* fill up rule */
                if(bmask != 0x80)
                {
                    buf |= (byte)(bmask - 1);
                    res.Add(buf);
                }

                return res.ToArray();
            }

            private struct HuffmanCode
            {
                public readonly int Code;
                public readonly int BitLength;

                public HuffmanCode(int code, int bitlength)
                {
                    Code = code;
                    BitLength = bitlength;
                }
            }

            static Http2Stream()
            {
                /* Generate Huffman decoder table */
                foreach(KeyValuePair<byte, HuffmanCode> kvp in m_EncodeHuffmanTable)
                {
                    Dictionary<int, byte> dict;
                    if(!m_DecodeHuffmanTable.TryGetValue(kvp.Value.BitLength, out dict))
                    {
                        dict = new Dictionary<int, byte>();
                        m_DecodeHuffmanTable.Add(kvp.Value.BitLength, dict);
                    }
                    dict.Add(kvp.Value.Code, kvp.Key);
                }
            }

            private static readonly Dictionary<int, Dictionary<int, byte>> m_DecodeHuffmanTable = new Dictionary<int, Dictionary<int, byte>>();

            private static readonly Dictionary<byte, HuffmanCode> m_EncodeHuffmanTable = new Dictionary<byte, HuffmanCode>
            {
                [0] = new HuffmanCode(0x1ff8, 13),
                [1] = new HuffmanCode(0x7fffd8, 23),
                [2] = new HuffmanCode(0xfffffe2, 28),
                [3] = new HuffmanCode(0xfffffe3, 28),
                [4] = new HuffmanCode(0xfffffe4, 28),
                [5] = new HuffmanCode(0xfffffe5, 28),
                [6] = new HuffmanCode(0xfffffe6, 28),
                [7] = new HuffmanCode(0xfffffe7, 28),
                [8] = new HuffmanCode(0xfffffe8, 28),
                [9] = new HuffmanCode(0xffffea, 24),
                [10] = new HuffmanCode(0x3ffffffc, 30),
                [11] = new HuffmanCode(0xfffffe9, 28),
                [12] = new HuffmanCode(0xfffffea, 28),
                [13] = new HuffmanCode(0x3ffffffd, 30),
                [14] = new HuffmanCode(0xfffffeb, 28),
                [15] = new HuffmanCode(0xfffffec, 28),
                [16] = new HuffmanCode(0xfffffed, 28),
                [17] = new HuffmanCode(0xfffffee, 28),
                [18] = new HuffmanCode(0xfffffef, 28),
                [19] = new HuffmanCode(0xffffff0, 28),
                [20] = new HuffmanCode(0xffffff1, 28),
                [21] = new HuffmanCode(0xffffff2, 28),
                [22] = new HuffmanCode(0x3ffffffe, 30),
                [23] = new HuffmanCode(0xffffff3, 28),
                [24] = new HuffmanCode(0xffffff4, 28),
                [25] = new HuffmanCode(0xffffff5, 28),
                [26] = new HuffmanCode(0xffffff6, 28),
                [27] = new HuffmanCode(0xffffff7, 28),
                [28] = new HuffmanCode(0xffffff8, 28),
                [29] = new HuffmanCode(0xffffff9, 28),
                [30] = new HuffmanCode(0xffffffa, 28),
                [31] = new HuffmanCode(0xffffffb, 28),
                [32] = new HuffmanCode(0x14, 6),
                [33] = new HuffmanCode(0x3f8, 10),
                [34] = new HuffmanCode(0x3f9, 10),
                [35] = new HuffmanCode(0xffa, 12),
                [36] = new HuffmanCode(0x1ff9, 13),
                [37] = new HuffmanCode(0x15, 6),
                [38] = new HuffmanCode(0xf8, 8),
                [39] = new HuffmanCode(0x7fa, 11),
                [40] = new HuffmanCode(0x3fa, 10),
                [41] = new HuffmanCode(0x3fb, 10),
                [42] = new HuffmanCode(0xf9, 8),
                [43] = new HuffmanCode(0x7fb, 11),
                [44] = new HuffmanCode(0xfa, 8),
                [45] = new HuffmanCode(0x16, 6),
                [46] = new HuffmanCode(0x17, 6),
                [47] = new HuffmanCode(0x18, 6),
                [48] = new HuffmanCode(0x0, 5),
                [49] = new HuffmanCode(0x1, 5),
                [50] = new HuffmanCode(0x2, 5),
                [51] = new HuffmanCode(0x19, 6),
                [52] = new HuffmanCode(0x1a, 6),
                [53] = new HuffmanCode(0x1b, 6),
                [54] = new HuffmanCode(0x1c, 6),
                [55] = new HuffmanCode(0x1d, 6),
                [56] = new HuffmanCode(0x1e, 6),
                [57] = new HuffmanCode(0x1f, 6),
                [58] = new HuffmanCode(0x5c, 7),
                [59] = new HuffmanCode(0xfb, 8),
                [60] = new HuffmanCode(0x7ffc, 15),
                [61] = new HuffmanCode(0x20, 6),
                [62] = new HuffmanCode(0xffb, 12),
                [63] = new HuffmanCode(0x3fc, 10),
                [64] = new HuffmanCode(0x1ffa, 13),
                [65] = new HuffmanCode(0x21, 6),
                [66] = new HuffmanCode(0x5d, 7),
                [67] = new HuffmanCode(0x5e, 7),
                [68] = new HuffmanCode(0x5f, 7),
                [69] = new HuffmanCode(0x60, 7),
                [70] = new HuffmanCode(0x61, 7),
                [71] = new HuffmanCode(0x62, 7),
                [72] = new HuffmanCode(0x63, 7),
                [73] = new HuffmanCode(0x64, 7),
                [74] = new HuffmanCode(0x65, 7),
                [75] = new HuffmanCode(0x66, 7),
                [76] = new HuffmanCode(0x67, 7),
                [77] = new HuffmanCode(0x68, 7),
                [78] = new HuffmanCode(0x69, 7),
                [79] = new HuffmanCode(0x6a, 7),
                [80] = new HuffmanCode(0x6b, 7),
                [81] = new HuffmanCode(0x6c, 7),
                [82] = new HuffmanCode(0x6d, 7),
                [83] = new HuffmanCode(0x6e, 7),
                [84] = new HuffmanCode(0x6f, 7),
                [85] = new HuffmanCode(0x70, 7),
                [86] = new HuffmanCode(0x71, 7),
                [87] = new HuffmanCode(0x72, 7),
                [88] = new HuffmanCode(0xfc, 8),
                [89] = new HuffmanCode(0x73, 7),
                [90] = new HuffmanCode(0xfd, 8),
                [91] = new HuffmanCode(0x1ffb, 13),
                [92] = new HuffmanCode(0x7fff0, 19),
                [93] = new HuffmanCode(0x1ffc, 13),
                [94] = new HuffmanCode(0x3ffc, 14),
                [95] = new HuffmanCode(0x22, 6),
                [96] = new HuffmanCode(0x7ffd, 15),
                [97] = new HuffmanCode(0x3, 5),
                [98] = new HuffmanCode(0x23, 6),
                [99] = new HuffmanCode(0x4, 5),
                [100] = new HuffmanCode(0x24, 6),
                [101] = new HuffmanCode(0x5, 5),
                [102] = new HuffmanCode(0x25, 6),
                [103] = new HuffmanCode(0x26, 6),
                [104] = new HuffmanCode(0x27, 6),
                [105] = new HuffmanCode(0x6, 5),
                [106] = new HuffmanCode(0x74, 7),
                [107] = new HuffmanCode(0x75, 7),
                [108] = new HuffmanCode(0x28, 6),
                [109] = new HuffmanCode(0x29, 6),
                [110] = new HuffmanCode(0x2a, 6),
                [111] = new HuffmanCode(0x7, 5),
                [112] = new HuffmanCode(0x2b, 6),
                [113] = new HuffmanCode(0x76, 7),
                [114] = new HuffmanCode(0x2c, 6),
                [115] = new HuffmanCode(0x8, 5),
                [116] = new HuffmanCode(0x9, 5),
                [117] = new HuffmanCode(0x2d, 6),
                [118] = new HuffmanCode(0x77, 7),
                [119] = new HuffmanCode(0x78, 7),
                [120] = new HuffmanCode(0x79, 7),
                [121] = new HuffmanCode(0x7a, 7),
                [122] = new HuffmanCode(0x7b, 7),
                [123] = new HuffmanCode(0x7ffe, 15),
                [124] = new HuffmanCode(0x7fc, 11),
                [125] = new HuffmanCode(0x3ffd, 14),
                [126] = new HuffmanCode(0x1ffd, 13),
                [127] = new HuffmanCode(0xffffffc, 28),
                [128] = new HuffmanCode(0xfffe6, 20),
                [129] = new HuffmanCode(0x3fffd2, 22),
                [130] = new HuffmanCode(0xfffe7, 20),
                [131] = new HuffmanCode(0xfffe8, 20),
                [132] = new HuffmanCode(0x3fffd3, 22),
                [133] = new HuffmanCode(0x3fffd4, 22),
                [134] = new HuffmanCode(0x3fffd5, 22),
                [135] = new HuffmanCode(0x7fffd9, 23),
                [136] = new HuffmanCode(0x3fffd6, 22),
                [137] = new HuffmanCode(0x7fffda, 23),
                [138] = new HuffmanCode(0x7fffdb, 23),
                [139] = new HuffmanCode(0x7fffdc, 23),
                [140] = new HuffmanCode(0x7fffdd, 23),
                [141] = new HuffmanCode(0x7fffde, 23),
                [142] = new HuffmanCode(0xffffeb, 24),
                [143] = new HuffmanCode(0x7fffdf, 23),
                [144] = new HuffmanCode(0xffffec, 24),
                [145] = new HuffmanCode(0xffffed, 24),
                [146] = new HuffmanCode(0x3fffd7, 22),
                [147] = new HuffmanCode(0x7fffe0, 23),
                [148] = new HuffmanCode(0xffffee, 24),
                [149] = new HuffmanCode(0x7fffe1, 23),
                [150] = new HuffmanCode(0x7fffe2, 23),
                [151] = new HuffmanCode(0x7fffe3, 23),
                [152] = new HuffmanCode(0x7fffe4, 23),
                [153] = new HuffmanCode(0x1fffdc, 21),
                [154] = new HuffmanCode(0x3fffd8, 22),
                [155] = new HuffmanCode(0x7fffe5, 23),
                [156] = new HuffmanCode(0x3fffd9, 22),
                [157] = new HuffmanCode(0x7fffe6, 23),
                [158] = new HuffmanCode(0x7fffe7, 23),
                [159] = new HuffmanCode(0xffffef, 24),
                [160] = new HuffmanCode(0x3fffda, 22),
                [161] = new HuffmanCode(0x1fffdd, 21),
                [162] = new HuffmanCode(0xfffe9, 20),
                [163] = new HuffmanCode(0x3fffdb, 22),
                [164] = new HuffmanCode(0x3fffdc, 22),
                [165] = new HuffmanCode(0x7fffe8, 23),
                [166] = new HuffmanCode(0x7fffe9, 23),
                [167] = new HuffmanCode(0x1fffde, 21),
                [168] = new HuffmanCode(0x7fffea, 23),
                [169] = new HuffmanCode(0x3fffdd, 22),
                [170] = new HuffmanCode(0x3fffde, 22),
                [171] = new HuffmanCode(0xfffff0, 24),
                [172] = new HuffmanCode(0x1fffdf, 21),
                [173] = new HuffmanCode(0x3fffdf, 22),
                [174] = new HuffmanCode(0x7fffeb, 23),
                [175] = new HuffmanCode(0x7fffec, 23),
                [176] = new HuffmanCode(0x1fffe0, 21),
                [177] = new HuffmanCode(0x1fffe1, 21),
                [178] = new HuffmanCode(0x3fffe0, 22),
                [179] = new HuffmanCode(0x1fffe2, 21),
                [180] = new HuffmanCode(0x7fffed, 23),
                [181] = new HuffmanCode(0x3fffe1, 22),
                [182] = new HuffmanCode(0x7fffee, 23),
                [183] = new HuffmanCode(0x7fffef, 23),
                [184] = new HuffmanCode(0xfffea, 20),
                [185] = new HuffmanCode(0x3fffe2, 22),
                [186] = new HuffmanCode(0x3fffe3, 22),
                [187] = new HuffmanCode(0x3fffe4, 22),
                [188] = new HuffmanCode(0x7ffff0, 23),
                [189] = new HuffmanCode(0x3fffe5, 22),
                [190] = new HuffmanCode(0x3fffe6, 22),
                [191] = new HuffmanCode(0x7ffff1, 23),
                [192] = new HuffmanCode(0x3ffffe0, 26),
                [193] = new HuffmanCode(0x3ffffe1, 26),
                [194] = new HuffmanCode(0xfffeb, 20),
                [195] = new HuffmanCode(0x7fff1, 19),
                [196] = new HuffmanCode(0x3fffe7, 22),
                [197] = new HuffmanCode(0x7ffff2, 23),
                [198] = new HuffmanCode(0x3fffe8, 22),
                [199] = new HuffmanCode(0x1ffffec, 25),
                [200] = new HuffmanCode(0x3ffffe2, 26),
                [201] = new HuffmanCode(0x3ffffe3, 26),
                [202] = new HuffmanCode(0x3ffffe4, 26),
                [203] = new HuffmanCode(0x7ffffde, 27),
                [204] = new HuffmanCode(0x7ffffdf, 27),
                [205] = new HuffmanCode(0x3ffffe5, 26),
                [206] = new HuffmanCode(0xfffff1, 24),
                [207] = new HuffmanCode(0x1ffffed, 25),
                [208] = new HuffmanCode(0x7fff2, 19),
                [209] = new HuffmanCode(0x1fffe3, 21),
                [210] = new HuffmanCode(0x3ffffe6, 26),
                [211] = new HuffmanCode(0x7ffffe0, 27),
                [212] = new HuffmanCode(0x7ffffe1, 27),
                [213] = new HuffmanCode(0x3ffffe7, 26),
                [214] = new HuffmanCode(0x7ffffe2, 27),
                [215] = new HuffmanCode(0xfffff2, 24),
                [216] = new HuffmanCode(0x1fffe4, 21),
                [217] = new HuffmanCode(0x1fffe5, 21),
                [218] = new HuffmanCode(0x3ffffe8, 26),
                [219] = new HuffmanCode(0x3ffffe9, 26),
                [220] = new HuffmanCode(0xffffffd, 28),
                [221] = new HuffmanCode(0x7ffffe3, 27),
                [222] = new HuffmanCode(0x7ffffe4, 27),
                [223] = new HuffmanCode(0x7ffffe5, 27),
                [224] = new HuffmanCode(0xfffec, 20),
                [225] = new HuffmanCode(0xfffff3, 24),
                [226] = new HuffmanCode(0xfffed, 20),
                [227] = new HuffmanCode(0x1fffe6, 21),
                [228] = new HuffmanCode(0x3fffe9, 22),
                [229] = new HuffmanCode(0x1fffe7, 21),
                [230] = new HuffmanCode(0x1fffe8, 21),
                [231] = new HuffmanCode(0x7ffff3, 23),
                [232] = new HuffmanCode(0x3fffea, 22),
                [233] = new HuffmanCode(0x3fffeb, 22),
                [234] = new HuffmanCode(0x1ffffee, 25),
                [235] = new HuffmanCode(0x1ffffef, 25),
                [236] = new HuffmanCode(0xfffff4, 24),
                [237] = new HuffmanCode(0xfffff5, 24),
                [238] = new HuffmanCode(0x3ffffea, 26),
                [239] = new HuffmanCode(0x7ffff4, 23),
                [240] = new HuffmanCode(0x3ffffeb, 26),
                [241] = new HuffmanCode(0x7ffffe6, 27),
                [242] = new HuffmanCode(0x3ffffec, 26),
                [243] = new HuffmanCode(0x3ffffed, 26),
                [244] = new HuffmanCode(0x7ffffe7, 27),
                [245] = new HuffmanCode(0x7ffffe8, 27),
                [246] = new HuffmanCode(0x7ffffe9, 27),
                [247] = new HuffmanCode(0x7ffffea, 27),
                [248] = new HuffmanCode(0x7ffffeb, 27),
                [249] = new HuffmanCode(0xffffffe, 28),
                [250] = new HuffmanCode(0x7ffffec, 27),
                [251] = new HuffmanCode(0x7ffffed, 27),
                [252] = new HuffmanCode(0x7ffffee, 27),
                [253] = new HuffmanCode(0x7ffffef, 27),
                [254] = new HuffmanCode(0x7fffff0, 27),
                [255] = new HuffmanCode(0x3ffffee, 26)
            };
            #endregion
        }
    }
}
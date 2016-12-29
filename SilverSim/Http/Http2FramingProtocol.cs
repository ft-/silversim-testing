// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.IO;

namespace SilverSim.Http
{
    public abstract class Http2FramingProtocol : IDisposable
    {
        Stream m_OriginalStream;
        bool m_DisposeFlag;

        [Serializable]
        public class ProtocolErrorException : Exception
        {
            public ProtocolErrorException()
            {

            }
        }

        protected Http2FramingProtocol(Stream originalStream, bool dispose = true)
        {
            m_OriginalStream = originalStream;
            m_DisposeFlag = dispose;
        }

        public void Dispose()
        {
            SendGoAway(GoAwayReasonCode.NoError);
            if(m_DisposeFlag)
            {
                m_OriginalStream.Dispose();
            }
        }

        protected enum GoAwayReasonCode
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

        protected enum Http2FrameType : byte
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
        protected enum DataFrameFlags : byte
        {
            EndStream = 0x1,
            Padded = 0x8
        }

        [Flags]
        protected enum HeadersFrameFlags : byte
        {
            EndStream = 0x1,
            EndHeaders = 0x4,
            Padded = 0x8,
            Priority = 0x20
        }

        [Flags]
        protected enum SettingsFrameFlags : byte
        {
            Ack = 0x01
        }

        [Flags]
        protected enum PushPromiseFrameFlags : byte
        {
            EndHeaders = 0x4,
            Padded = 0x8
        }

        [Flags]
        protected enum PingFrameFlags : byte
        {
            Ack = 0x1
        }

        [Flags]
        protected enum ContinuationFrameFlags : byte
        {
            EndHeaders = 0x4
        }
        protected sealed class Http2Frame
        {
            public int Length;
            public Http2FrameType Type;
            public byte Flags;
            public int StreamIdentifier;
            public byte[] Data;

            public Http2Frame()
            {
            }
        }

        readonly object m_SendLock = new object();
        int m_LastReceivedStreamId;

        protected void SendGoAway(GoAwayReasonCode reason)
        {
            byte[] goawaydata = new byte[8];
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

        protected void SendFrame(Http2FrameType type, byte flags, int streamid, byte[] data, int offset, int length)
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
                byte[] hdr = new byte[9];
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

        protected Http2Frame ReceiveFrame()
        {
            Http2Frame frame;
            do
            {
                frame = new Http2Frame();
                byte[] hdr = new byte[9];
                if (9 != m_OriginalStream.Read(hdr, 0, 9))
                {
                    SendGoAway(GoAwayReasonCode.ProtocolError);
                    throw new ProtocolErrorException();
                }

                frame.Length = (hdr[0] << 16) | (hdr[1] << 8) | (hdr[2]);
                frame.Type = (Http2FrameType)hdr[3];
                frame.Flags = hdr[4];
                frame.StreamIdentifier = ((hdr[5] & 0x7F) << 24) | (hdr[6] << 16) | (hdr[7] << 8) | hdr[8];
                frame.Data = new byte[frame.Length];

                int rcvdbytes;
                try
                {
                    rcvdbytes = m_OriginalStream.Read(frame.Data, 0, frame.Length);
                }
                catch
                {
                    SendGoAway(GoAwayReasonCode.ProtocolError);
                    throw new ProtocolErrorException();
                }

                if (frame.Length != rcvdbytes)
                {
                    SendGoAway(GoAwayReasonCode.ProtocolError);
                    throw new ProtocolErrorException();
                }
                if(frame.Type == Http2FrameType.Ping)
                {
                    if(frame.StreamIdentifier != 0)
                    {
                        SendGoAway(GoAwayReasonCode.ProtocolError);
                        throw new ProtocolErrorException();
                    }
                    if(frame.Length != 8)
                    {
                        SendGoAway(GoAwayReasonCode.FrameSizeError);
                        throw new ProtocolErrorException();
                    }
                    SendFrame(Http2FrameType.Ping, (byte)PingFrameFlags.Ack, 0, frame.Data, 0, frame.Length);
                }
            } while (frame.Type == Http2FrameType.Ping);
            m_LastReceivedStreamId = frame.StreamIdentifier;
            return frame;
        }
    }
}
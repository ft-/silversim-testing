// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SilverSim.Viewer.Messages
{
    public class UDPPacket
    {
        public const int DEFAULT_BUFFER_SIZE = 4096;

        public byte[] Data { get; protected set; }

        public int DataLength = 6;

        public int DataPos = 6;

        public int TransferredAtTime;
        public int EnqueuedAtTime;
        public uint ResentCount;

        public Message.QueueOutType OutQueue = Message.QueueOutType.Low;
        public Message AckMessage; /* only used by Circuit */

        public UInt32 SequenceNumber
        {
            get
            {
                if(BitConverter.IsLittleEndian)
                {
                    byte[] b = new byte[4];
                    Buffer.BlockCopy(Data, 1, b, 0, 4);
                    Array.Reverse(b);
                    return BitConverter.ToUInt32(b, 0);
                }
                else
                {
                    return BitConverter.ToUInt32(Data, 1);
                }
            }
            set
            {
                Buffer.BlockCopy(BitConverter.GetBytes(value), 0, Data, 1, 4);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(Data, 1, 4);
                }
            }
        }

        public bool IsZeroEncoded
        {
            get
            {
                return (Data[0] & 0x80) != 0;
            }
            set
            {
                byte v = Data[0];
                v &= 0x7F;
                if(value)
                {
                    v |= 0x80;
                }
                Data[0] = v;
            }
        }

        public bool IsReliable
        {
            get
            {
                return (Data[0] & 0x40) != 0;
            }
            set
            {
                byte v = Data[0];
                v &= 0xBF;
                if (value)
                {
                    v |= 0x40;
                }
                Data[0] = v;
            }
        }

        public bool IsResent
        {
            get
            {
                return (Data[0] & 0x20) != 0;
            }
            set
            {
                byte v = Data[0];
                v &= 0xDF;
                if (value)
                {
                    v |= 0x20;
                }
                Data[0] = v;
            }
        }

        public bool HasAckFlag
        {
            get
            {
                return (Data[0] & 0x10) != 0;
            }
            set
            {
                byte v = Data[0];
                v &= 0xEF;
                if (value)
                {
                    v |= 0x10;
                }
                Data[0] = v;
            }
        }
        public int AppendedNumberOfAcks
        {
            get
            {
                return (HasAckFlag) ?
                    Data[DataLength - 1] :
                    0;
            }
        }

        private void DecodeZLE()
        {
            if(!IsZeroEncoded)
            {
                return;
            }
            byte[] oldData = new byte[DataLength];
            int extralen = (int)(uint)Data[5];
            int srcpos = 6 + extralen;
            int dstpos = 6 + extralen;
            Buffer.BlockCopy(Data, 0, oldData, 0, DataLength);

            for(;srcpos < DataLength; ++srcpos)
            {
                if(oldData[srcpos] == 0)
                {
                    uint cnt = oldData[++srcpos];
                    for (uint i = 0; i < cnt; ++i)
                    {
                        Data[dstpos++] = 0;
                    }
                }
                else
                {
                    Data[dstpos++] = oldData[srcpos];
                }
            }
            DataLength = dstpos;
            IsZeroEncoded = false;
        }

        public List<UInt32> Acks
        {
            get
            {
                uint numacks = 0;
                /* singleton method, it will adjust the data length afterwards */
                if(HasAckFlag)
                {
                    /* acks are appended uncompressed */
                    numacks = Data[DataLength - 1];
                    int AckStartPos = DataLength - 1 - 4 * (int)numacks;

                    List<UInt32> acknumbers = new List<uint>();

                    for (uint ackidx = 0; ackidx < numacks; ++ackidx)
                    {
                        if(BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(Data, AckStartPos + (int)ackidx * 4, 4);
                        }
                        acknumbers.Add(BitConverter.ToUInt32(Data, AckStartPos + (int)ackidx * 4));
                    }

                    DataLength = AckStartPos;

                    /* we consumed those appended acks, so remove that flag */
                    HasAckFlag = false;

                    return acknumbers;
                }
                else
                {
                    return new List<UInt32>();
                }
            }
        }

        public bool IsUndersized
        {
            get
            {
                uint numacks = 0;
                if(HasAckFlag)
                {
                    /* last byte can never be a ZLE count when we have an ack flag */
                    numacks = Data[DataLength - 1];
                    if(IsZeroEncoded)
                    {
                        int pos = DataLength - 2;
                        int len = (int)numacks * 4;
                        while(len > 0)
                        {
                            if(Data[pos - 1] == 0)
                            {
                                /* we got a ZLE group */
                                int zlelen = Data[pos];
                                if(zlelen > len)
                                {
                                    len = 0;
                                }
                                else
                                {
                                    len -= zlelen;
                                }
                                pos -= 2;
                            }
                            else
                            {
                                --pos;
                                --len;
                            }

                            /* check for undersize criteria when pre-decoding acks */
                            if(pos < 5 + Data[5])
                            {
                                return true;
                            }
                        }
                    }
                    else
                    {
                        numacks = 1 + numacks * 4;
                    }
                }
                return (5 + Data[5] + numacks) > Data.Length;
            }
        }

        private byte zleCount;

        public static UDPPacket PacketAckImmediate(UInt32 seqno)
        {
            UDPPacket p = new UDPPacket();
            p.WriteMessageNumber(MessageType.PacketAck);
            p.WriteUInt8(1);
            p.WriteUInt32(seqno);
            return p;
        }

        public UDPPacket()
        {
            Data = new byte[DEFAULT_BUFFER_SIZE];
            Data[0] = 0;
            Data[5] = 0;
        }

        public UDPPacket(int bufferSize, bool zeroencoded)
        {
            Data = new byte[bufferSize];
            Data[0] = zeroencoded ? (byte)0x80 : (byte)0;
            Data[5] = 0;
        }

        public override string ToString()
        {
            string dmp = string.Empty;
            for(int i = 0; i < DataLength; ++i)
            {
                if(i != 0)
                {
                    dmp += " ";
                }
                dmp += string.Format("{0:x2}", (uint)Data[i]);
            }
            return dmp;
        }

        public void Reset()
        {
            zleCount = 0;
            DataPos = 6 + Data[5];
        }

        public void Clear()
        {
            zleCount = 0;
            DataPos = 6 + Data[5];
            DataLength = 6 + Data[5];
        }

        public void Flush()
        {
            if(zleCount != 0)
            {
                WriteUInt8(0);
                WriteUInt8(zleCount);
                zleCount = 0;
            }
            DataLength = DataPos;
        }

        #region Receive Packet
        public void EndReceiveFrom(Socket udpSocket, IAsyncResult ar, ref EndPoint ep)
        {
            DataLength = udpSocket.EndReceiveFrom(ar, ref ep);
            if(DataLength == 0)
            {
                throw new KeyNotFoundException();
            }

            IsZeroEncoded = (Data[0] & 0x80) != 0;
            DataPos = 6 + Data[5]; /* extra header data */
            zleCount = 0;
        }
        #endregion

        #region Send Packet
        public void SendPacket(Socket udpSocket, EndPoint remoteEndpoint, AsyncCallback endsend)
        {
            if (IsZeroEncoded)
            {
                Flush();
            }

            udpSocket.BeginSendTo(
                                    Data,
                                    0,
                                    DataLength,
                                    SocketFlags.None,
                                    remoteEndpoint,
                                    endsend,
                                    this);
        }
        #endregion

        private byte[] ReadZeroEncoded(int length)
        {
            byte[] outbuf = new byte[length];
            for (int i = 0; i < length; ++i)
            {
                if (zleCount == 0)
                {
                    if (Data[DataPos] == 0)
                    {
                        zleCount = Data[++DataPos];
                        outbuf[i] = 0;
                        zleCount--;
                        DataPos++;
                    }
                    else
                    {
                        outbuf[i] = Data[DataPos];
                        ++DataPos;
                    }
                }
                else
                {
                    --zleCount;
                    outbuf[i] = 0;
                }
            }
            return outbuf;
        }

        private void WriteZeroEncoded(byte[] buf)
        {
            for(int i = 0; i < buf.Length; ++i)
            {
                byte b = buf[i];
                if(b == 0)
                {
                    if(zleCount == 255)
                    {
                        Data[DataPos++] = 0;
                        Data[DataPos++] = zleCount;
                        zleCount = 1;
                    }
                    else
                    {
                        ++zleCount;
                    }
                }
                else
                {
                    if(zleCount != 0)
                    {
                        Data[DataPos++] = 0;
                        Data[DataPos++] = zleCount;
                        zleCount = 0;
                    }
                    Data[DataPos++] = b;
                }
            }
            DataLength = DataPos;
        }

        #region Bool
        public bool ReadBoolean()
        {
            return ReadUInt8() != 0;
        }

        public void WriteBoolean(bool b)
        {
            WriteUInt8(b ? (byte)1 : (byte)0);
        }
        #endregion

        #region UInt64
        public UInt64 ReadUInt64()
        {
            if(IsZeroEncoded)
            {
                byte[] buf = ReadZeroEncoded(8);
                if(!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(buf);
                }
                return BitConverter.ToUInt64(buf, 0);
            }
            else if (!BitConverter.IsLittleEndian)
            {
                byte[] buf = new byte[8];
                Buffer.BlockCopy(Data, DataPos, buf, 0, 8);
                Array.Reverse(buf);
                DataPos += 8;
                return BitConverter.ToUInt64(buf, 0);
            }
            UInt64 val = BitConverter.ToUInt64(Data, DataPos);
            DataPos += 8;
            return val;
        }

        public void WriteUInt64(UInt64 val)
        {
            if(IsZeroEncoded)
            {
                byte[] buf = BitConverter.GetBytes(val);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(buf);
                }
                WriteZeroEncoded(buf);
            }
            else
            {
                byte[] buf = BitConverter.GetBytes(val);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(buf);
                }
                Buffer.BlockCopy(buf, 0, Data, DataPos, buf.Length);
                DataPos += 8;
                DataLength = DataPos;
            }
        }
        #endregion

        #region Int64
        public void WriteInt64(Int32 val)
        {
            if (IsZeroEncoded)
            {
                byte[] buf = BitConverter.GetBytes(val);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(buf);
                }
                WriteZeroEncoded(buf);
            }
            else
            {
                byte[] buf = BitConverter.GetBytes(val);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(buf);
                }
                Buffer.BlockCopy(buf, 0, Data, DataPos, buf.Length);
                DataPos += 8;
                DataLength = DataPos;
            }
        }

        public Int64 ReadInt64()
        {
            if (IsZeroEncoded)
            {
                byte[] buf = ReadZeroEncoded(8);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(buf);
                }
                return BitConverter.ToInt64(buf, 0);
            }
            else if (!BitConverter.IsLittleEndian)
            {
                byte[] buf = new byte[8];
                Buffer.BlockCopy(Data, DataPos, buf, 0, 8);
                Array.Reverse(buf);
                DataPos += 8;
                return BitConverter.ToInt64(buf, 0);
            }
            Int64 val = BitConverter.ToInt64(Data, DataPos);
            DataPos += 8;
            return val;
        }
        #endregion

        #region UInt32
        public void WriteUInt32(UInt32 val)
        {
            if (IsZeroEncoded)
            {
                byte[] buf = BitConverter.GetBytes(val);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(buf);
                }
                WriteZeroEncoded(buf);
            }
            else
            {
                byte[] buf = BitConverter.GetBytes(val);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(buf);
                }
                Buffer.BlockCopy(buf, 0, Data, DataPos, buf.Length);
                DataPos += 4;
                DataLength = DataPos;
            }
        }

        public UInt32 ReadUInt32()
        {
            if (IsZeroEncoded)
            {
                byte[] buf = ReadZeroEncoded(4);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(buf);
                }
                return BitConverter.ToUInt32(buf, 0);
            }
            else if (!BitConverter.IsLittleEndian)
            {
                byte[] buf = new byte[4];
                Buffer.BlockCopy(Data, DataPos, buf, 0, 4);
                Array.Reverse(buf);
                DataPos += 4;
                return BitConverter.ToUInt32(buf, 0);
            }
            UInt32 val = BitConverter.ToUInt32(Data, DataPos);
            DataPos += 4;
            return val;
        }
        #endregion

        #region UInt32BE
        public void WriteUInt32BE(UInt32 val)
        {
            if (IsZeroEncoded)
            {
                byte[] buf = BitConverter.GetBytes(val);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(buf);
                }
                WriteZeroEncoded(buf);
            }
            else
            {
                byte[] buf = BitConverter.GetBytes(val);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(buf);
                }
                Buffer.BlockCopy(buf, 0, Data, DataPos, buf.Length);
                DataPos += 4;
                DataLength = DataPos;
            }
        }
        #endregion

        #region Int32
        public Int32 ReadInt32()
        {
            if (IsZeroEncoded)
            {
                byte[] buf = ReadZeroEncoded(4);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(buf);
                }
                return BitConverter.ToInt32(buf, 0);
            }
            else if (!BitConverter.IsLittleEndian)
            {
                byte[] buf = new byte[4];
                Buffer.BlockCopy(Data, DataPos, buf, 0, 4);
                Array.Reverse(buf);
                DataPos += 4;
                return BitConverter.ToInt32(buf, 0);
            }
            Int32 val = BitConverter.ToInt32(Data, DataPos);
            DataPos += 4;
            return val;
        }

        public void WriteInt32(Int32 val)
        {
            if (IsZeroEncoded)
            {
                byte[] buf = BitConverter.GetBytes(val);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(buf);
                }
                WriteZeroEncoded(buf);
            }
            else
            {
                byte[] buf = BitConverter.GetBytes(val);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(buf);
                }
                Buffer.BlockCopy(buf, 0, Data, DataPos, buf.Length);
                DataPos += 4;
                DataLength = DataPos;
            }
        }
        #endregion

        #region UInt16
        public UInt16 ReadUInt16()
        {
            if (IsZeroEncoded)
            {
                byte[] buf = ReadZeroEncoded(2);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(buf);
                }
                return BitConverter.ToUInt16(buf, 0);
            }
            else if (!BitConverter.IsLittleEndian)
            {
                byte[] buf = new byte[2];
                Buffer.BlockCopy(Data, DataPos, buf, 0, 2);
                Array.Reverse(buf);
                DataPos += 2;
                return BitConverter.ToUInt16(buf, 0);
            }
            UInt16 val = BitConverter.ToUInt16(Data, DataPos);
            DataPos += 2;
            return val;
        }

        public void WriteUInt16(UInt16 val)
        {
            if (IsZeroEncoded)
            {
                byte[] buf = BitConverter.GetBytes(val);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(buf);
                }
                WriteZeroEncoded(buf);
            }
            else
            {
                byte[] buf = BitConverter.GetBytes(val);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(buf);
                }
                Buffer.BlockCopy(buf, 0, Data, DataPos, buf.Length);
                DataPos += 2;
                DataLength = DataPos;
            }
        }
        #endregion

        #region Int16
        public Int16 ReadInt16()
        {
            if (IsZeroEncoded)
            {
                byte[] buf = ReadZeroEncoded(2);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(buf);
                }
                return BitConverter.ToInt16(buf, 0);
            }
            else if (!BitConverter.IsLittleEndian)
            {
                byte[] buf = new byte[2];
                Buffer.BlockCopy(Data, DataPos, buf, 0, 2);
                Array.Reverse(buf);
                DataPos += 2;
                return BitConverter.ToInt16(buf, 0);
            }
            Int16 val = BitConverter.ToInt16(Data, DataPos);
            DataPos += 2;
            return val;
        }

        public void WriteInt16(Int16 val)
        {
            if (IsZeroEncoded)
            {
                byte[] buf = BitConverter.GetBytes(val);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(buf);
                }
                WriteZeroEncoded(buf);
            }
            else
            {
                byte[] buf = BitConverter.GetBytes(val);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(buf);
                }
                Buffer.BlockCopy(buf, 0, Data, DataPos, buf.Length);
                DataPos += 2;
                DataLength = DataPos;
            }
        }
        #endregion

        #region UInt8

        public byte ReadUInt8()
        {
            if (IsZeroEncoded)
            {
                byte[] buf = ReadZeroEncoded(1);
                return buf[0];
            }
            byte val = Data[DataPos];
            DataPos += 1;
            return val;
        }

        public void WriteUInt8(byte val)
        {
            if (IsZeroEncoded)
            {
                byte[] buf = new byte[] { val };
                WriteZeroEncoded(buf);
            }
            else
            {
                byte[] buf = new byte[]{val};
                Buffer.BlockCopy(buf, 0, Data, DataPos, buf.Length);
                DataPos += 1;
                DataLength = DataPos;
            }
        }
        #endregion

        #region Int8

        public sbyte ReadInt8()
        {
            if (IsZeroEncoded)
            {
                byte[] buf = ReadZeroEncoded(1);
                return (sbyte)buf[0];
            }
            sbyte val = (sbyte)Data[DataPos];
            DataPos += 1;
            return val;
        }

        public void WriteInt8(sbyte val)
        {
            if (IsZeroEncoded)
            {
                byte[] buf = new byte[] { (byte)val };
                WriteZeroEncoded(buf);
            }
            else
            {
                byte[] buf = new byte[] { (byte)val };
                Buffer.BlockCopy(buf, 0, Data, DataPos, buf.Length);
                DataPos += 1;
                DataLength = DataPos;
            }
        }
        #endregion

        #region Double
        public double ReadDouble()
        {
            if (IsZeroEncoded)
            {
                byte[] buf = ReadZeroEncoded(8);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(buf);
                }
                return BitConverter.ToDouble(buf, 0);
            }
            else if(!BitConverter.IsLittleEndian)
            {
                byte[] buf = new byte[8];
                Buffer.BlockCopy(Data, DataPos, buf, 0, 8);
                Array.Reverse(buf);
                DataPos += 8;
                return BitConverter.ToDouble(buf, 0);
            }
            double val = BitConverter.ToDouble(Data, DataPos);
            DataPos += 8;
            return val;
        }

        public void WriteDouble(double val)
        {
            if (IsZeroEncoded)
            {
                byte[] buf = BitConverter.GetBytes(val);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(buf);
                }
                WriteZeroEncoded(buf);
            }
            else
            {
                byte[] buf = BitConverter.GetBytes(val);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(buf);
                }
                Buffer.BlockCopy(buf, 0, Data, DataPos, buf.Length);
                DataPos += 8;
                DataLength = DataPos;
            }
        }
        #endregion

        #region Float
        public float ReadFloat()
        {
            if (IsZeroEncoded)
            {
                byte[] buf = ReadZeroEncoded(4);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(buf);
                }
                return BitConverter.ToSingle(buf, 0);
            }
            else if(!BitConverter.IsLittleEndian)
            {
                byte[] buf = new byte[4];
                Buffer.BlockCopy(Data, DataPos, buf, 0, 4);
                Array.Reverse(buf);
                DataPos += 4;
                return BitConverter.ToSingle(buf, 0);
            }
            float val = BitConverter.ToSingle(Data, DataPos);
            DataPos += 4;
            return val;
        }

        public void WriteFloat(float val)
        {
            if (IsZeroEncoded)
            {
                byte[] buf = BitConverter.GetBytes(val);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(buf);
                }
                WriteZeroEncoded(buf);
            }
            else
            {
                byte[] buf = BitConverter.GetBytes(val);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(buf);
                }
                Buffer.BlockCopy(buf, 0, Data, DataPos, buf.Length);
                DataPos += 4;
                DataLength = DataPos;
            }
        }
        #endregion

        #region String
        public string ReadStringLen8()
        {
            byte len = ReadUInt8();
            if (len == 0)
            {
                return string.Empty;
            }
            byte[] buf = ReadBytes(len);
            while(len > 0)
            {
                if(buf[len - 1] != 0)
                {
                    break;
                }
                len--;
            }
            string s = Encoding.UTF8.GetString(buf, 0, len);
            int i = s.IndexOf('\0');
            if (i >= 0)
            {
                s = s.Substring(0, i);
            }
            return s;
        }

        public void WriteStringLen8(string val)
        {
            byte[] buf = Encoding.UTF8.GetBytes(val);
            if(buf.Length > 254)
            {
                byte[] nbuf = new byte[254];
                Buffer.BlockCopy(buf, 0, nbuf, 0, 254);
                buf = nbuf;
            }
            WriteUInt8((byte)(buf.Length + 1));
            WriteBytes(buf);
            WriteUInt8(0);
        }

        public string ReadStringLen16()
        {
            UInt16 len = ReadUInt16();
            if(len == 0)
            {
                return string.Empty;
            }
            byte[] buf = ReadBytes(len);
            while (len > 0)
            {
                if (buf[len - 1] != 0)
                {
                    break;
                }
                len--;
            }
            string s = Encoding.UTF8.GetString(buf, 0, len);
            int i = s.IndexOf('\0');
            if (i >= 0)
            {
                s = s.Substring(0, i);
            }
            return s;
        }

        public void WriteStringLen16(string val)
        {
            byte[] buf = Encoding.UTF8.GetBytes(val);
            if (buf.Length > 65534)
            {
                byte[] nbuf = new byte[65534];
                Buffer.BlockCopy(buf, 0, nbuf, 0, 65534);
                buf = nbuf;
            }
            WriteUInt16((UInt16)(buf.Length + 1));
            WriteBytes(buf);
            WriteUInt8(0);
        }
        #endregion

        #region UUID
        public UUID ReadUUID()
        {
            if (IsZeroEncoded)
            {
                byte[] buf = ReadZeroEncoded(16);
                return new UUID(buf, 0);
            }
            DataPos += 16;
            return new UUID(Data, DataPos - 16);
        }

        public void WriteUUID(UUID val)
        {
            if (IsZeroEncoded)
            {
                byte[] buf = new byte[16];
                val.ToBytes(buf, 0);
                WriteZeroEncoded(buf);
            }
            else
            {
                val.ToBytes(Data, DataPos);
                DataPos += 16;
                DataLength = DataPos;
            }
        }
        #endregion

        #region UUID
        public byte[] ReadBytes(int length)
        {
            if(length == 0)
            {
                return new byte[0];
            }
            else if(IsZeroEncoded)
            {
                return ReadZeroEncoded(length);
            }
            else
            {
                byte[] buf = new byte[length];
                Buffer.BlockCopy(Data, DataPos, buf, 0, length);
                DataPos += length;
                return buf;
            }
        }

        public void WriteBytes(byte[] buf)
        {
            if(buf.Length == 0)
            {

            }
            else if(IsZeroEncoded)
            {
                WriteZeroEncoded(buf);
            }
            else
            {
                Buffer.BlockCopy(buf, 0, Data, DataPos, buf.Length);
                DataPos += buf.Length;
                DataLength = DataPos;
            }
        }
        #endregion

        #region PacketType
        public MessageType ReadMessageType()
        {
            UInt32 packetType = 0;
            byte h = ReadUInt8();
            if (0xFF == h)
            {
                /* medium or low frequency messages */
                byte m = ReadUInt8();
                if (0xFF == m)
                {
                    /* low frequency messages */
                    packetType = ((UInt32)h << 16) | ((UInt32)m << 8) | ReadUInt8();
                    packetType = (packetType << 8) | ReadUInt8();
                }
                else
                {
                    packetType = ((UInt32)h << 8) | ((UInt32)m);
                }
            }
            else
            {
                packetType = h;
            }

            MessageType mType = (MessageType)packetType;
            return mType;
        }

        public void WriteMessageNumber(MessageType type)
        {
            UInt32 t = (UInt32)type;
            if (t < 256)
            {
                WriteUInt8((byte)t);
            }
            else if(t >= 0xFF00 && t < 0xFFFF)
            {
                WriteUInt8((byte)0xFF);
                WriteUInt8((byte)(t & 0xFF));
            }
            else if(t >= 0xFFFF0000)
            {
                WriteUInt16((UInt16)0xFFFF);
                WriteUInt8((byte)(t >> 8));
                WriteUInt8((byte)(t & 0xFF));
            }
        }
        #endregion

        #region Vector
        public Vector3 ReadVector3f()
        {
            float x, y, z;
            x = ReadFloat();
            y = ReadFloat();
            z = ReadFloat();

            return new Vector3(x, y, z);
        }

        public Vector3 ReadVector3d()
        {
            double x, y, z;
            x = ReadDouble();
            y = ReadDouble();
            z = ReadDouble();

            return new Vector3(x, y, z);
        }

        public void WriteVector3f(Vector3 v)
        {
            WriteFloat((float)v.X);
            WriteFloat((float)v.Y);
            WriteFloat((float)v.Z);
        }

        public void WriteVector3d(Vector3 v)
        {
            WriteDouble(v.X);
            WriteDouble(v.Y);
            WriteDouble(v.Z);
        }
        #endregion

        #region Quaternion
        public Quaternion ReadLLQuaternion()
        {
            float x, y, z;
            x = ReadFloat();
            y = ReadFloat();
            z = ReadFloat();

            return new Quaternion(x, y, z);
        }

        public void WriteLLQuaternion(Quaternion q)
        {
            Quaternion qo = q.Normalize();
            WriteFloat((float)qo.X);
            WriteFloat((float)qo.Y);
            WriteFloat((float)qo.Z);
        }
        #endregion

        #region Ack Append
        public void FinishZLE()
        {
            if (zleCount != 0)
            {
                Data[DataPos++] = 0;
                Data[DataPos++] = zleCount;
                zleCount = 0;
            }
        }

        public void WriteUInt32BE_NoZLE(UInt32 val)
        {
            if (zleCount != 0)
            {
                Data[DataPos++] = 0;
                Data[DataPos++] = zleCount;
                zleCount = 0;
            }
            byte[] buf = BitConverter.GetBytes(val);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(buf);
            }
            Buffer.BlockCopy(buf, 0, Data, DataPos, buf.Length);
            DataPos += 4;
            DataLength = DataPos;
        }
        #endregion
    }
}

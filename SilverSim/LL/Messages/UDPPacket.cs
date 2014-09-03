/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SilverSim.LL.Messages
{
    public class UDPPacket
    {
        public const int DEFAULT_BUFFER_SIZE = 4096;

        public readonly byte[] Data;

        public int DataLength = 6;

        public int DataPos = 6;

        public uint TransferredAtTime = 0;
        public uint EnqueuedAtTime = 0;

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
                if (BitConverter.IsLittleEndian)
                {
                    byte[] b = BitConverter.GetBytes(value);
                    Array.Reverse(b);
                    Buffer.BlockCopy(b, 0, Data, 1, 4);
                }
                else
                {
                    Buffer.BlockCopy(BitConverter.GetBytes(value), 0, Data, 1, 4);
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
                v &= 0x7F;
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
                if(HasAckFlag)
                {
                    return Data[DataLength - 1];
                }
                else
                {
                    return 0;
                }
            }
        }

        public List<UInt32> Acks
        {
            get
            {
                uint numacks = 0;
                /* singleton method, it will adjust the data length afterwards */
                if(HasAckFlag)
                {
                    byte[] ackbuf = new byte[numacks * 4];
                    numacks = Data[DataLength - 1];

                    if(IsZeroEncoded)
                    {
                        int dstpos = (int)numacks * 4;

                        int len = (int)numacks * 4;
                        while (len > 0)
                        {
                            if (Data[DataLength - 1] == 0)
                            {
                                /* we got a ZLE group */
                                int zlelen = Data[DataLength - 1];
                                if (zlelen > len)
                                {
                                    /* we did not fully satisfy it so let us adjust that */
                                    Data[DataLength - 1] -= (byte)len;
                                    while(len-- != 0)
                                    {
                                        ackbuf[--dstpos] = 0;
                                    }
                                }
                                else
                                {
                                    /* we fully satisfied it, so move DataLength */
                                    len -= zlelen;
                                    DataLength -= 2;
                                    while(zlelen-- != 0)
                                    {
                                        ackbuf[--dstpos] = 0;
                                    }
                                }
                            }
                            else
                            {
                                ackbuf[--dstpos] = Data[--DataLength];
                            }
                        }
                    }
                    else
                    {
                        Buffer.BlockCopy(Data, (int)(DataLength - 1 - numacks * 4), ackbuf, 0, (int)numacks * 4);
                        DataLength -= (int)(1 + numacks * 4);
                    }

                    List<UInt32> acknumbers = new List<uint>();
                    byte[] shortbuf = null;
                    if(!BitConverter.IsLittleEndian)
                    {
                        shortbuf = new byte[4];
                    }

                    for (uint ackidx = 0; ackidx < numacks; ++ackidx)
                    {
                        if(BitConverter.IsLittleEndian)
                        {
                            acknumbers.Add(BitConverter.ToUInt32(ackbuf, (int)ackidx * 4));
                        }
                        else
                        {
                            Buffer.BlockCopy(ackbuf, (int)ackidx * 4, shortbuf, 0, 4);
                            Array.Reverse(shortbuf);
                            acknumbers.Add(BitConverter.ToUInt32(shortbuf, 0));
                        }
                    }

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

        private byte zleCount = 0;

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
                DataLength = DataPos += 8;
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
                DataLength = DataPos += 8;
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
                DataLength = DataPos += 4;
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
            UInt32 val = BitConverter.ToUInt32(Data, DataPos);
            DataPos += 4;
            return val;
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
                DataLength = DataPos += 4;
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
                DataLength = DataPos += 2;
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
                DataLength = DataPos += 2;
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
                DataLength = DataPos += 1;
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
                DataLength = DataPos += 1;
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
                DataLength = DataPos += 8;
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
                DataLength = DataPos += 4;
            }
        }
        #endregion

        #region String
        public string ReadStringLen8()
        {
            byte len = ReadUInt8();
            byte[] buf = ReadBytes(len);
            return Encoding.UTF8.GetString(buf, 0, len - 1);
        }

        public void WriteStringLen8(string val)
        {
            byte[] buf = Encoding.UTF8.GetBytes(val + "\0");
            WriteUInt8((byte)buf.Length);
            WriteBytes(buf);
        }

        public string ReadStringLen16()
        {
            UInt16 len = ReadUInt16();
            byte[] buf = ReadBytes(len);
            return Encoding.UTF8.GetString(buf, 0, len - 1);
        }

        public void WriteStringLen16(string val)
        {
            byte[] buf = Encoding.UTF8.GetBytes(val + "\0");
            WriteUInt16((UInt16)buf.Length);
            WriteBytes(buf);
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
                DataLength = DataPos += 16;
            }
        }
        #endregion

        #region UUID
        public byte[] ReadBytes(int length)
        {
            if(IsZeroEncoded)
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
            if(IsZeroEncoded)
            {
                WriteZeroEncoded(buf);
            }
            else
            {
                Buffer.BlockCopy(buf, 0, Data, DataPos, buf.Length);
                DataLength = DataPos += buf.Length;
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

        public void WriteMessageType(MessageType type)
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
    }
}

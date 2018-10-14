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

using System;
using System.IO;

namespace SilverSim.Types.StructuredData.TLV
{
    public sealed class TLV : IDisposable
    {
        public enum EntryType : ushort
        {
            End = 0,
            String = 1,
            UUID = 2,
            Binary = 3,
            Int32 = 4,
            UInt32 = 5,
            Int64 = 6,
            UInt64 = 7,
            Boolean = 8,
            Int16 = 9,
            UInt16 = 10,
            Date = 11,
            Int8 = 12,
            UInt8 = 13,
            Double = 14,
            Float = 15,
            Vector3 = 16,
            Quaternion = 17,
            Vector4 = 18,
            UGI = 19,
            UGUI = 20,
            UGUIWithName = 21,
            URI = 22,
            TLV = 23,
            GridVector = 24,
            Color = 25,
            ColorAlpha = 26
        }

        public struct Header
        {
            public ushort ID;
            public EntryType Type;
            public int Length;
        }

        private Stream m_Stream;
        private readonly bool m_ReadOuter = false;
        private TLV m_Outer;
        private ushort m_OuterId;
        private int m_MaxLength = 0;

        public TLV(Stream s)
        {
            m_Stream = s;
        }

        private TLV(ushort tlvId, TLV tlv)
        {
            if (tlv == null)
            {
                throw new ArgumentNullException(nameof(tlv));
            }
            m_Stream = new MemoryStream();
            m_Outer = tlv;
            m_OuterId = tlvId;
        }

        private TLV(Header header, TLV tlv)
        {
            if(header.Type != EntryType.TLV)
            {
                throw new ArgumentOutOfRangeException(nameof(header));
            }
            if(tlv == null)
            {
                throw new ArgumentNullException(nameof(tlv));
            }
            m_ReadOuter = true;
            m_Outer = tlv;
            m_Stream = tlv.m_Stream;
            m_MaxLength = header.Length;
        }

        public TLV WriteInner(ushort tlvId) => new TLV(tlvId, this);

        public TLV ReadInner(Header header) => new TLV(header, this);

        public void Dispose()
        {
            if (m_Stream != null)
            {
                if(m_Outer != null)
                {
                    byte[] array = ((MemoryStream)m_Stream).ToArray();
                    m_Outer.Write_Header(m_OuterId, EntryType.TLV, array.Length);
                    m_Outer.m_Stream.Write(array, 0, array.Length);
                    m_Outer = null;
                }
                if (!m_ReadOuter)
                {
                    m_Stream.Dispose();
                }
                else
                {
                    byte[] readdata;
                    TryReadData(m_MaxLength, out readdata);
                }
            }
            m_Stream = null;
        }

        /* this tlv structured format is made in a way that it can repeat IDs in same stream */
        private void Write_Header(ushort tlvId, EntryType type, int length)
        {
            if(m_Stream == null)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            byte[] header = new byte[8];
            header[0] = (byte)tlvId;
            header[1] = (byte)(tlvId >> 8);
            header[2] = (byte)type;
            header[3] = (byte)((ushort)type >> 8);
            header[4] = (byte)length;
            header[5] = (byte)(length >> 8);
            header[6] = (byte)(length >> 16);
            header[7] = (byte)(length >> 24);

            m_Stream.Write(header, 0, 8);
        }

        private void Write_Blob(ushort tlvId, EntryType type, byte[] data)
        {
            int length = data.Length;
            Write_Header(tlvId, type, length);
            m_Stream.Write(data, 0, length);
        }

        private void Write_HtoL(ushort tlvId, EntryType type, byte[] data)
        {
            if(!BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }
            Write_Blob(tlvId, type, data);
        }

        public void Write(ushort tlvId, Enum value)
        {
            Type t = value.GetType().GetEnumUnderlyingType();
            GetType().GetMethod("Write", new Type[] { typeof(ushort), t }).Invoke(this, 
                new object[] { tlvId, Convert.ChangeType(value, t) });
        }

        public void Write(ushort tlvId, string value) =>
            Write_Blob(tlvId, EntryType.String, value.ToUTF8Bytes());

        public void Write(ushort tlvId, UGI value) =>
            Write_Blob(tlvId, EntryType.UGI, value.ToString().ToUTF8Bytes());

        public void Write(ushort tlvId, UGUI value) =>
            Write_Blob(tlvId, EntryType.UGUI, value.ToString().ToUTF8Bytes());

        public void Write(ushort tlvId, UGUIWithName value) =>
            Write_Blob(tlvId, EntryType.UGUIWithName, value.ToString().ToUTF8Bytes());

        public void Write(ushort tlvId, URI value) =>
            Write_Blob(tlvId, EntryType.URI, value.ToString().ToUTF8Bytes());

        public void Write(ushort tlvId, UUID value) =>
            Write_Blob(tlvId, EntryType.UUID, value.GetBytes());

        public void Write(ushort tlvId, bool value) =>
            Write_Blob(tlvId, EntryType.Boolean, new byte[] { value ? (byte)1 : (byte)0 });

        public void Write(ushort tlvId, Date value) =>
            Write_HtoL(tlvId, EntryType.Date, BitConverter.GetBytes(value.DateTimeToUnixTime()));

        public void Write(ushort tlvId, Vector3 value)
        {
            Write_Header(tlvId, EntryType.Vector3, 24);
            byte[] dataX;
            byte[] dataY;
            byte[] dataZ;
            dataX = BitConverter.GetBytes(value.X);
            dataY = BitConverter.GetBytes(value.Y);
            dataZ = BitConverter.GetBytes(value.Z);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(dataX);
                Array.Reverse(dataY);
                Array.Reverse(dataZ);
            }
            m_Stream.Write(dataX, 0, 8);
            m_Stream.Write(dataY, 0, 8);
            m_Stream.Write(dataZ, 0, 8);
        }

        public void Write(ushort tlvId, Vector4 value)
        {
            Write_Header(tlvId, EntryType.Vector4, 32);
            byte[] dataX;
            byte[] dataY;
            byte[] dataZ;
            byte[] dataW;
            dataX = BitConverter.GetBytes(value.X);
            dataY = BitConverter.GetBytes(value.Y);
            dataZ = BitConverter.GetBytes(value.Z);
            dataW = BitConverter.GetBytes(value.W);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(dataX);
                Array.Reverse(dataY);
                Array.Reverse(dataZ);
                Array.Reverse(dataW);
            }
            m_Stream.Write(dataX, 0, 8);
            m_Stream.Write(dataY, 0, 8);
            m_Stream.Write(dataZ, 0, 8);
            m_Stream.Write(dataW, 0, 8);
        }

        public void Write(ushort tlvId, Quaternion value)
        {
            Write_Header(tlvId, EntryType.Quaternion, 32);
            byte[] dataX;
            byte[] dataY;
            byte[] dataZ;
            byte[] dataW;
            dataX = BitConverter.GetBytes(value.X);
            dataY = BitConverter.GetBytes(value.Y);
            dataZ = BitConverter.GetBytes(value.Z);
            dataW = BitConverter.GetBytes(value.W);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(dataX);
                Array.Reverse(dataY);
                Array.Reverse(dataZ);
                Array.Reverse(dataW);
            }
            m_Stream.Write(dataX, 0, 8);
            m_Stream.Write(dataY, 0, 8);
            m_Stream.Write(dataZ, 0, 8);
            m_Stream.Write(dataW, 0, 8);
        }

        public void Write(ushort tlvId, GridVector value) =>
            Write_HtoL(tlvId, EntryType.GridVector, BitConverter.GetBytes(value.RegionHandle));

        public void Write(ushort tlvId, float value) =>
            Write_HtoL(tlvId, EntryType.Float, BitConverter.GetBytes(value));

        public void Write(ushort tlvId, double value) =>
            Write_HtoL(tlvId, EntryType.Double, BitConverter.GetBytes(value));

        public void Write(ushort tlvId, ushort value) => 
            Write_HtoL(tlvId, EntryType.UInt16, BitConverter.GetBytes(value));

        public void Write(ushort tlvId, short value) =>
            Write_HtoL(tlvId, EntryType.Int16, BitConverter.GetBytes(value));

        public void Write(ushort tlvId, uint value) =>
            Write_HtoL(tlvId, EntryType.UInt32, BitConverter.GetBytes(value));

        public void Write(ushort tlvId, int value) =>
            Write_HtoL(tlvId, EntryType.Int32, BitConverter.GetBytes(value));

        public void Write(ushort tlvId, ulong value) =>
            Write_HtoL(tlvId, EntryType.UInt64, BitConverter.GetBytes(value));

        public void Write(ushort tlvId, long value) =>
            Write_HtoL(tlvId, EntryType.Int64, BitConverter.GetBytes(value));

        public void Write(ushort tlvId, byte[] data) =>
            Write_Blob(tlvId, EntryType.Binary, data);

        public void Write(ushort tlvId, byte value) =>
            Write_Blob(tlvId, EntryType.UInt8, new byte[] { value });

        public void Write(ushort tlvId, sbyte value) =>
            Write_Blob(tlvId, EntryType.Int8, new byte[] { (byte)value });

        public void Write(ushort tlvIds, Color color) =>
            Write_Blob(tlvIds, EntryType.Color, color.AsByte);

        public void Write(ushort tlvIds, ColorAlpha color) =>
            Write_Blob(tlvIds, EntryType.ColorAlpha, color.AsByte);

        public bool TryReadTypedValue<T>(Header header, out T data)
        {
            data = default(T);
            object d;
            if(!TryReadValue(header, out d))
            {
                return false;
            }
            Type targetType = typeof(T);
            Type sourceType = d.GetType();
            if(sourceType != targetType)
            {
                if(targetType.IsEnum && targetType.GetEnumUnderlyingType() == sourceType)
                {
                    data = (T)Convert.ChangeType(d, targetType);
                }
                return false;
            }
            data = (T)d;
            return true;
        }

        public void SkipValue(Header header)
        {
            byte[] readdata;
            TryReadData(header.Length, out readdata);
        }

        public bool TryRead(out Header header, out object data)
        {
            while(TryReadHeader( out header))
            {
                if(TryReadValue(header, out data))
                {
                    return true;
                }
            }
            data = default(object);
            return false;
        }

        public bool TryReadHeader(out Header header)
        {
            header = default(Header);
            if (m_Stream == null)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if(m_ReadOuter && m_MaxLength < 8)
            {
                return false;
            }

            byte[] headerbytes = new byte[8];
            if(8 != m_Stream.Read(headerbytes, 0, 8))
            {
                return false;
            }
            m_MaxLength -= 8;

            header.ID = (ushort)((headerbytes[1] << 8) | headerbytes[0]);
            header.Type = (EntryType)((headerbytes[3] << 8) | headerbytes[2]);
            header.Length = (headerbytes[7] << 24) | (headerbytes[6] << 16) | (headerbytes[5] << 8) | (headerbytes[4] << 8);
            return true;
        }

        private bool TryReadData(int length, out byte[] readdata)
        {
            readdata = new byte[length];

            if (m_ReadOuter && m_MaxLength < length)
            {
                return false;
            }

            if (length != m_Stream.Read(readdata, 0, length))
            {
                return false;
            }

            m_MaxLength -= length;

            return true;
        }

        public bool TryReadValue(Header header, out object data)
        {
            if (m_Stream == null)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            data = default(object);

            byte[] readdata;
            switch (header.Type)
            {
                case EntryType.End:
                    if(header.Length != 0)
                    {
                        return false;
                    }
                    return true;

                case EntryType.UUID:
                    if (!TryReadData(header.Length, out readdata))
                    {
                        return false;
                    }
                    if (header.Length != 16)
                    {
                        return false;
                    }
                    data = new UUID(readdata, 0);
                    return true;

                case EntryType.String:
                    if (header.Length == 0)
                    {
                        data = string.Empty;
                        return true;
                    }
                    else
                    {
                        if (!TryReadData(header.Length, out readdata))
                        {
                            return false;
                        }
                        data = readdata.FromUTF8Bytes();
                        return true;
                    }

                case EntryType.Binary:
                    if (!TryReadData(header.Length, out readdata))
                    {
                        return false;
                    }
                    data = readdata;
                    return true;

                case EntryType.Int8:
                    if (!TryReadData(header.Length, out readdata))
                    {
                        return false;
                    }
                    if (header.Length != 1)
                    {
                        return false;
                    }
                    data = (sbyte)readdata[0];
                    return true;

                case EntryType.UInt8:
                    if (!TryReadData(header.Length, out readdata))
                    {
                        return false;
                    }
                    if (header.Length != 1)
                    {
                        return false;
                    }
                    data = readdata[0];
                    return true;

                case EntryType.Int32:
                    if (!TryReadData(header.Length, out readdata))
                    {
                        return false;
                    }
                    if (header.Length != 4)
                    {
                        return false;
                    }
                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(readdata);
                    }
                    data = BitConverter.ToInt32(readdata, 0);
                    return true;

                case EntryType.UInt32:
                    if (!TryReadData(header.Length, out readdata))
                    {
                        return false;
                    }
                    if (header.Length != 4)
                    {
                        return false;
                    }
                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(readdata);
                    }
                    data = BitConverter.ToUInt32(readdata, 0);
                    return true;

                case EntryType.Int16:
                    if (!TryReadData(header.Length, out readdata))
                    {
                        return false;
                    }
                    if (header.Length != 2)
                    {
                        return false;
                    }
                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(readdata);
                    }
                    data = BitConverter.ToInt16(readdata, 0);
                    return true;

                case EntryType.UInt16:
                    if (!TryReadData(header.Length, out readdata))
                    {
                        return false;
                    }
                    if (header.Length != 2)
                    {
                        return false;
                    }
                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(readdata);
                    }
                    data = BitConverter.ToUInt16(readdata, 0);
                    return true;

                case EntryType.Int64:
                    if (!TryReadData(header.Length, out readdata))
                    {
                        return false;
                    }
                    if (header.Length != 8)
                    {
                        return false;
                    }
                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(readdata);
                    }
                    data = BitConverter.ToInt64(readdata, 0);
                    return true;

                case EntryType.UInt64:
                    if (!TryReadData(header.Length, out readdata))
                    {
                        return false;
                    }
                    if (header.Length != 8)
                    {
                        return false;
                    }
                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(readdata);
                    }
                    data = BitConverter.ToUInt64(readdata, 0);
                    return true;

                case EntryType.Date:
                    if (!TryReadData(header.Length, out readdata))
                    {
                        return false;
                    }
                    if (header.Length != 8)
                    {
                        return false;
                    }
                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(readdata);
                    }
                    data = Date.UnixTimeToDateTime(BitConverter.ToUInt64(readdata, 0));
                    return true;

                case EntryType.Float:
                    if (!TryReadData(header.Length, out readdata))
                    {
                        return false;
                    }
                    if (header.Length != 4)
                    {
                        return false;
                    }
                    if(!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(readdata);
                    }
                    data = BitConverter.ToSingle(readdata, 0);
                    return true;

                case EntryType.Double:
                    if (!TryReadData(header.Length, out readdata))
                    {
                        return false;
                    }
                    if (header.Length != 8)
                    {
                        return false;
                    }
                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(readdata);
                    }
                    data = BitConverter.ToDouble(readdata, 0);
                    return true;

                case EntryType.Vector3:
                    if (!TryReadData(header.Length, out readdata))
                    {
                        return false;
                    }
                    if (header.Length != 24)
                    {
                        return false;
                    }
                    if(!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(readdata, 0, 8);
                        Array.Reverse(readdata, 8, 8);
                        Array.Reverse(readdata, 16, 8);
                    }
                    data = new Vector3(BitConverter.ToDouble(readdata, 0),
                        BitConverter.ToDouble(readdata, 8),
                        BitConverter.ToDouble(readdata, 16));
                    return true;

                case EntryType.Quaternion:
                    if (!TryReadData(header.Length, out readdata))
                    {
                        return false;
                    }
                    if (header.Length != 32)
                    {
                        return false;
                    }
                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(readdata, 0, 8);
                        Array.Reverse(readdata, 8, 8);
                        Array.Reverse(readdata, 16, 8);
                        Array.Reverse(readdata, 24, 8);
                    }
                    data = new Quaternion(BitConverter.ToDouble(readdata, 0),
                        BitConverter.ToDouble(readdata, 8),
                        BitConverter.ToDouble(readdata, 16),
                        BitConverter.ToDouble(readdata, 24));
                    return true;

                case EntryType.Vector4:
                    if (!TryReadData(header.Length, out readdata))
                    {
                        return false;
                    }
                    if (header.Length != 32)
                    {
                        return false;
                    }
                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(readdata, 0, 8);
                        Array.Reverse(readdata, 8, 8);
                        Array.Reverse(readdata, 16, 8);
                        Array.Reverse(readdata, 24, 8);
                    }
                    data = new Vector4(BitConverter.ToDouble(readdata, 0),
                        BitConverter.ToDouble(readdata, 8),
                        BitConverter.ToDouble(readdata, 16),
                        BitConverter.ToDouble(readdata, 24));
                    return true;

                case EntryType.UGI:
                    if (!TryReadData(header.Length, out readdata))
                    {
                        return false;
                    }
                    data = new UGI(readdata.FromUTF8Bytes());
                    return true;

                case EntryType.UGUI:
                    if (!TryReadData(header.Length, out readdata))
                    {
                        return false;
                    }
                    data = new UGUI(readdata.FromUTF8Bytes());
                    return true;

                case EntryType.UGUIWithName:
                    if (!TryReadData(header.Length, out readdata))
                    {
                        return false;
                    }
                    data = new UGUIWithName(readdata.FromUTF8Bytes());
                    return true;

                case EntryType.URI:
                    if (!TryReadData(header.Length, out readdata))
                    {
                        return false;
                    }
                    data = new URI(readdata.FromUTF8Bytes());
                    return true;

                case EntryType.Boolean:
                    if (!TryReadData(header.Length, out readdata))
                    {
                        return false;
                    }
                    if (header.Length != 1)
                    {
                        return false;
                    }
                    data = readdata[0] != 0;
                    return true;

                case EntryType.GridVector:
                    if (!TryReadData(header.Length, out readdata))
                    {
                        return false;
                    }
                    if (header.Length != 8)
                    {
                        return false;
                    }
                    if(!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(readdata);
                    }
                    data = new GridVector { RegionHandle = BitConverter.ToUInt64(readdata, 0) };
                    return true;

                case EntryType.Color:
                    if (!TryReadData(header.Length, out readdata))
                    {
                        return false;
                    }
                    if (header.Length != 3)
                    {
                        return false;
                    }
                    data = Color.FromRgb(readdata[0], readdata[1], readdata[2]);
                    return true;

                case EntryType.ColorAlpha:
                    if (!TryReadData(header.Length, out readdata))
                    {
                        return false;
                    }
                    if (header.Length != 3)
                    {
                        return false;
                    }
                    data = ColorAlpha.FromRgba(readdata[0], readdata[1], readdata[2], readdata[3]);
                    return true;

                default:
                    SkipValue(header);
                    break;
            }

            return false;
        }
    }
}

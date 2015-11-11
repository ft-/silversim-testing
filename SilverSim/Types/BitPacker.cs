// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Text;

namespace SilverSim.Types
{
    public class BitPacker
    {
        public byte[] Data;
        private int m_BitPos;

        public BitPacker(byte[] data, int pos)
        {
            m_BitPos = pos * 8;
            Data = data;
        }

        public BitPacker(byte[] data)
        {
            Data = data;
        }

        public void Reset()
        {
            m_BitPos = 0;
        }

        public int BitLength
        {
            get
            {
                return m_BitPos;
            }
        }

        public int BitPos
        {
            get
            {
                return m_BitPos % 8;
            }
        }

        public int BytePos
        {
            get
            {
                return m_BitPos / 8;
            }
        }

        public int NumBytes
        {
            get
            {
                return (m_BitPos + 7) / 8;
            }
        }

        #region Pack/Unpack functions
        public int UnpackSignedBits(int bitCount)
        {
            byte[] d = UnpackBitsFromArray(bitCount);

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(d);
            }
            return BitConverter.ToInt32(d, 0);
        }

        public uint UnpackUnsignedBits(int bitCount)
        {
            byte[] d = UnpackBitsFromArray(bitCount);

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(d);
            }
            return BitConverter.ToUInt32(d, 0);
        }

        public void PackBits(int data, int bitCount)
        {
            byte[] d = BitConverter.GetBytes(data);
            if(!BitConverter.IsLittleEndian)
            {
                Array.Reverse(d);
            }
            PackBitsToArray(d, bitCount);
        }

        public void PackBits(uint data, int bitCount)
        {
            byte[] d = BitConverter.GetBytes(data);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(d);
            }
            PackBitsToArray(d, bitCount);
        }
        #endregion

        #region Pack/Unpack Properties

        public float FloatValue
        {
            get
            {
                byte[] b = UnpackBitsFromArray(32);
                if(!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(b);
                }
                return BitConverter.ToSingle(b, 0);
            }
            set
            {
                byte[] b = BitConverter.GetBytes(value);
                if(!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(b);
                }
                PackBitsToArray(b, 32);
            }
        }

        public uint UintValue
        {
            get
            {
                return UnpackUnsignedBits(32);
            }

            set
            {
                PackBits(value, 32);
            }
        }

        public int IntValue
        {
            get
            {
                return UnpackSignedBits(32);
            }

            set
            {
                PackBits(value, 32);
            }
        }

        public short ShortValue
        {
            get
            {
                return (short)UnpackSignedBits(16);
            }
            set
            {
                PackBits((int)value, 16);
            }
        }

        public ushort UshortValue
        {
            get
            {
                return (ushort)UnpackUnsignedBits(16);
            }
            set
            {
                PackBits((uint)value, 16);
            }
        }

        public byte ByteValue
        {
            get
            {
                return UnpackBitsFromArray(8)[0];
            }
            set
            {
                PackBits(value, 8);
            }
        }

        public UUID UuidValue
        {
            get
            {
                if(m_BitPos % 8 != 0)
                {
                    throw new InvalidOperationException();
                }

                UUID val = new UUID(Data, m_BitPos / 8);
                m_BitPos += (16 * 8);
                return val;
            }
            set
            {
                if(m_BitPos % 8 != 0)
                {
                    throw new InvalidOperationException();
                }
                value.ToBytes(Data, m_BitPos / 8);
                m_BitPos += (16 * 8);
            }
        }

        private static readonly byte[] BIT_ON = new byte[] { 1 };
        private static readonly byte[] BIT_OFF = new byte[] { 0 };

        public bool BoolValue
        {
            get
            {
                return UnpackBitsFromArray(1)[0] != 0;
            }
            set
            {
                PackBitsToArray(value ? BIT_ON : BIT_OFF, 1);
            }
        }

        public ColorAlpha ColorValue
        {
            get
            {
                byte[] b = UnpackBitsFromArray(32);
                return new ColorAlpha(b);
            }
            set
            {
                byte[] b = value.AsByte;
                PackBitsToArray(b, 32);
            }
        }
        #endregion

        #region Pack/Unpack Fixed
        public void PackFixed(float data, bool isSigned, int intBits, int fracBits)
        {
            int unsignedBits = intBits + fracBits;
            int totalBits = unsignedBits;
            int min, max;

            if (isSigned)
            {
                totalBits++;
                min = 1 << intBits;
                min *= -1;
            }
            else
            {
                min = 0;
            }

            max = 1 << intBits;

            float fixedVal = data;
            if(data < (float)min)
            {
                data = (float)min;
            }
            if(data > (float)max)
            {
                data = (float)max;
            }
            if (isSigned)
            {
                fixedVal += max;
            }
            fixedVal *= 1 << fracBits;

            if (totalBits <= 8)
            {
                PackBits((uint)fixedVal, 8);
            }
            else if (totalBits <= 16)
            {
                PackBits((uint)fixedVal, 16);
            }
            else if (totalBits <= 31)
            {
                PackBits((uint)fixedVal, 32);
            }
            else
            {
                throw new ArgumentException("Can't use fixed point packing for " + totalBits.ToString());
            }
        }

        public float UnpackFixed(bool signed, int intBits, int fracBits)
        {
            int minVal;
            int maxVal;
            int unsignedBits = intBits + fracBits;
            int totalBits = unsignedBits;
            float fixedVal;

            if (signed)
            {
                totalBits++;

                minVal = 1 << intBits;
                minVal *= -1;
            }
            maxVal = 1 << intBits;

            if (totalBits <= 8)
            {
                fixedVal = (float)ByteValue;
            }
            else if (totalBits <= 16)
            {
                fixedVal = (float)UshortValue;
            }
            else if (totalBits <= 31)
            {
                fixedVal = (float)UintValue;
            }
            else
            {
                return 0.0f;
            }

            fixedVal /= (float)(1 << fracBits);

            if (signed)
            {
                fixedVal -= (float)maxVal;
            }

            return fixedVal;
        }
        #endregion

        #region Pack/Unpack string
        public void PackString(string s)
        {
            byte[] d = UTF8Encoding.UTF8.GetBytes(s);
            if (m_BitPos != 0 || m_BitPos / 8 + d.Length > Data.Length)
            {
                throw new InvalidOperationException();
            }

            Buffer.BlockCopy(d, 0, Data, m_BitPos / 8, d.Length);
            m_BitPos += d.Length * 8;
        }

        public string UnpackString(int size)
        {
            if (m_BitPos != 0 || m_BitPos / 8 + size > Data.Length)
            {
                throw new InvalidOperationException();
            }

            string str = UTF8Encoding.UTF8.GetString(Data, m_BitPos / 8, size);
            m_BitPos += size * 8;
            return str;
        }
        #endregion

        #region Pack Bits
        private void PackBitsToArray(byte[] data, int bitCount)
        {
            int count = 0;
            int curBytePos = 0;
            
            while(bitCount > 0)
            {
                count = bitCount;
                if(count > 8)
                {
                    count = 8;
                }

                while(count-- > 0)
                {
                    byte curBitMask = (byte)(0x80 >> (m_BitPos % 8));

                    if((data[curBytePos] & (0x01 << count)) != 0)
                    {
                        Data[m_BitPos / 8] |= curBitMask;
                    }
                    else
                    {
                        Data[m_BitPos / 8] &= (byte)~curBitMask;
                    }

                    ++m_BitPos;
                }
                ++curBytePos;

                if (bitCount > 8)
                {
                    bitCount -= 8;
                }
                else
                {
                    bitCount = 0;
                }
            }
        }
        #endregion

        #region Unpack bits
        private byte[] UnpackBitsFromArray(int bitCount)
        {
            int count = 0;
            byte[] output = new byte[4];
            int outputBytePos = 0;

            while(bitCount > 0)
            {
                count = bitCount;
                if(count > 8)
                {
                    count = 8;
                }

                while(count > 0)
                {
                    output[outputBytePos] <<= 1;

                    if((Data[m_BitPos / 8] & (0x80 >> (m_BitPos % 8))) != 0)
                    {
                        output[outputBytePos] |= 1;
                    }

                    --count;
                    ++m_BitPos;
                }
                ++outputBytePos;
                if (bitCount > 8)
                {
                    bitCount -= 8;
                }
                else
                {
                    bitCount = 0;
                }
            }

            return output;
        }
        #endregion

        #region Pack BitPacker Data
        public void PackBits(BitPacker src)
        {
            int count = 0;
            int curBytePos = 0;
            int bitCount = src.m_BitPos;

            while (bitCount > 0)
            {
                count = bitCount;
                if (count > 8)
                {
                    count = 8;
                }

                byte srcBits = src.Data[curBytePos];
                while (count-- > 0)
                {
                    byte curBitMask = (byte)(0x80 >> (m_BitPos % 8));

                    if ((srcBits & 0x80) != 0)
                    {
                        Data[m_BitPos / 8] |= curBitMask;
                    }
                    else
                    {
                        Data[m_BitPos / 8] &= (byte)~curBitMask;
                    }

                    ++m_BitPos;
                    srcBits <<= 1;
                }
                ++curBytePos;

                if (bitCount > 8)
                {
                    bitCount -= 8;
                }
                else
                {
                    bitCount = 0;
                }
            }
        }
        #endregion
    }
}

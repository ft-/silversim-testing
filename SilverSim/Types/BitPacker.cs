﻿// SilverSim is distributed under the terms of the
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

        public int BitLength => m_BitPos;

        public int BitPos => m_BitPos % 8;

        public int BytePos => m_BitPos / 8;

        public int NumBytes => (m_BitPos + 7) / 8;

        #region Pack/Unpack functions
        public int UnpackSignedBits(int bitCount)
        {
            var d = UnpackBitsFromArray(bitCount);

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(d);
            }
            return BitConverter.ToInt32(d, 0);
        }

        public uint UnpackUnsignedBits(int bitCount)
        {
            var d = UnpackBitsFromArray(bitCount);

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(d);
            }
            return BitConverter.ToUInt32(d, 0);
        }

        public void PackBits(int data, int bitCount)
        {
            var d = BitConverter.GetBytes(data);
            if(!BitConverter.IsLittleEndian)
            {
                Array.Reverse(d);
            }
            PackBitsToArray(d, bitCount);
        }

        public void PackBits(uint data, int bitCount)
        {
            var d = BitConverter.GetBytes(data);
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
                var b = UnpackBitsFromArray(32);
                if(!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(b);
                }
                return BitConverter.ToSingle(b, 0);
            }
            set
            {
                var b = BitConverter.GetBytes(value);
                if(!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(b);
                }
                PackBitsToArray(b, 32);
            }
        }

        public uint UintValue
        {
            get { return UnpackUnsignedBits(32); }

            set { PackBits(value, 32); }
        }

        public int IntValue
        {
            get { return UnpackSignedBits(32); }

            set { PackBits(value, 32); }
        }

        public short ShortValue
        {
            get { return (short)UnpackSignedBits(16); }

            set { PackBits(value, 16); }
        }

        public ushort UshortValue
        {
            get { return (ushort)UnpackUnsignedBits(16); }

            set { PackBits((uint)value, 16); }
        }

        public byte ByteValue
        {
            get { return UnpackBitsFromArray(8)[0]; }

            set { PackBits(value, 8); }
        }

        public UUID UuidValue
        {
            get
            {
                if(m_BitPos % 8 != 0)
                {
                    throw new InvalidOperationException();
                }

                var val = new UUID(Data, m_BitPos / 8);
                m_BitPos += 16 * 8;
                return val;
            }
            set
            {
                if(m_BitPos % 8 != 0)
                {
                    throw new InvalidOperationException();
                }
                value.ToBytes(Data, m_BitPos / 8);
                m_BitPos += 16 * 8;
            }
        }

        private static readonly byte[] BIT_ON = new byte[] { 1 };
        private static readonly byte[] BIT_OFF = new byte[] { 0 };

        public bool BoolValue
        {
            get { return UnpackBitsFromArray(1)[0] != 0; }

            set { PackBitsToArray(value ? BIT_ON : BIT_OFF, 1); }
        }

        public ColorAlpha ColorValue
        {
            get { return new ColorAlpha(UnpackBitsFromArray(32)); }

            set { PackBitsToArray(value.AsByte, 32); }
        }
        #endregion

        #region Pack/Unpack Fixed
        public void PackFixed(float data, bool isSigned, int intBits, int fracBits)
        {
            int totalBits = intBits + fracBits;
            int min;
            int max;

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
            if(data < min)
            {
                data = min;
            }
            if(data > max)
            {
                data = max;
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
            int maxVal;
            int totalBits = intBits + fracBits;
            float fixedVal;

            if (signed)
            {
                totalBits++;
            }
            maxVal = 1 << intBits;

            if (totalBits <= 8)
            {
                fixedVal = ByteValue;
            }
            else if (totalBits <= 16)
            {
                fixedVal = UshortValue;
            }
            else if (totalBits <= 31)
            {
                fixedVal = UintValue;
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
            var d = UTF8Encoding.UTF8.GetBytes(s);
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
                    var curBitMask = (byte)(0x80 >> (m_BitPos % 8));

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
            var output = new byte[4];
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
                    var curBitMask = (byte)(0x80 >> (m_BitPos % 8));

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

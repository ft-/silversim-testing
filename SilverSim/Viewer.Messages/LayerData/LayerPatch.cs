// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.LayerData
{
    public class LayerPatch
    {
        public uint X;
        public uint Y;

        uint m_Serial = 1; /* we use a serial number similar to other places to know what an agent has already got */

        public uint Serial
        {
            get
            {
                return m_Serial;
            }
            set
            {
                lock(m_Lock)
                {
                    m_Serial = (value == 0) ?
                        1 :
                        value;
                }
            }
        }

        public void IncrementSerial()
        {
            lock(m_Lock)
            {
                if(++m_Serial == 0)
                {
                    m_Serial = 1;
                }
            }
        }

        public float[,] Data = new float[16,16];

        internal uint PackedSerial;
        readonly byte[] PackedDataBytes = new byte[651]; /* maximum length of a single 16 by 16 patch when packed perfectly bad */
        internal readonly BitPacker PackedData;
        readonly object m_Lock = new object();

        public uint ExtendedPatchID
        {
            get
            {
                return (X << 16) | Y;
            }
            set
            {
                X = ((value >> 16) & 0xFFFF);
                Y = (value & 0xFFFF);
            }
        }

        public LayerPatch()
        {
            PackedData = new BitPacker(PackedDataBytes);
        }

        public LayerPatch(double defaultHeight)
        {
            PackedData = new BitPacker(PackedDataBytes);
            int x, y;
            for (y = 0; y < 16; ++y)
            {
                for (x = 0; x < 16; ++x)
                {
                    Data[y, x] = (float)defaultHeight;
                }
            }
        }

        public LayerPatch(LayerPatch p)
        {
            PackedData = new BitPacker(PackedDataBytes);
            X = p.X;
            Y = p.Y;
            int x, y;
            lock (m_Lock)
            {
                Serial = p.Serial;
                for (y = 0; y < 16; ++y)
                {
                    for (x = 0; x < 16; ++x)
                    {
                        Data[y, x] = p.Data[y, x];
                    }
                }
            }
        }

        public void Update(LayerPatch p)
        {
            if(X != p.X || Y != p.Y)
            {
                throw new ArgumentException("p does not match in its parameters X and Y.");
            }
            int x, y;
            lock (m_Lock)
            {
                for (y = 0; y < 16; ++y)
                {
                    for (x = 0; x < 16; ++x)
                    {
                        Data[y, x] = p.Data[y, x];
                    }
                }
                if (++m_Serial == 0)
                {
                    m_Serial = 1;
                }
            }
        }

        public void UpdateWithSerial(LayerPatch p)
        {
            if (X != p.X || Y != p.Y)
            {
                throw new ArgumentException("p does not match in its parameters X and Y.");
            }
            int x, y;
            lock (m_Lock)
            {
                for (y = 0; y < 16; ++y)
                {
                    for (x = 0; x < 16; ++x)
                    {
                        Data[y, x] = p.Data[y, x];
                    }
                }
                m_Serial = p.Serial;
                if (m_Serial == 0)
                {
                    m_Serial = 1;
                }
            }
        }

        public float this[int x, int y]
        {
            get
            {
                return Data[y, x];
            }
            set
            {
                lock(m_Lock)
                {
                    Data[y, x] = value;
                }
            }
        }

        public float this[uint x, uint y]
        {
            get
            {
                return Data[(int)y, (int)x];
            }
            set
            {
                lock (m_Lock)
                {
                    Data[(int)y, (int)x] = value;
                }
            }
        }

        public byte[] Serialization
        {
            get
            {
                LayerPatch copy;
                lock (m_Lock)
                {
                    copy = new LayerPatch(this);
                }
                byte[] dst = new byte[4 + 4 * 256];
                byte[] src = BitConverter.GetBytes(copy.Serial);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(src);
                }
                Buffer.BlockCopy(src, 0, dst, 0, src.Length);

                int x, y, pos = 4;
                for (y = 0; y < 16; ++y)
                {
                    for (x = 0; x < 16; ++x)
                    {
                        src = BitConverter.GetBytes(copy.Data[y, x]);
                        if (!BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(src);
                        }
                        Buffer.BlockCopy(src, 0, dst, pos, src.Length);
                        pos += 4;
                    }
                }
                return dst;
            }
            set
            {
                byte[] src = value;

                if(src.Length != 4 * 256 + 4)
                {
                    throw new ArgumentException("Terrain serialization data length does not match 1028 bytes");
                }
                if (!BitConverter.IsLittleEndian)
                {
                    int pos; 
                    src = new byte[src.Length];
                    Buffer.BlockCopy(value, 0, src, 0, src.Length);

                    for (pos = 0; pos < 16 * 16 + 4; ++pos)
                    {
                        Array.Reverse(src, pos, 4);
                    }
                }
                lock (m_Lock)
                {
                    Serial = BitConverter.ToUInt32(src, 0);
                    int x, y, pos = 4;
                    for (y = 0; y < 16; ++y)
                    {
                        for (x = 0; x < 16; ++x)
                        {
                            Data[y, x] = BitConverter.ToSingle(src, pos);
                            pos += 4;
                        }
                    }
                }
            }
        }
    }
}

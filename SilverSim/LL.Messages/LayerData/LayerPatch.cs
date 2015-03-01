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
namespace SilverSim.LL.Messages.LayerData
{
    public class LayerPatch
    {
        public int X;
        public int Y;

        uint m_Serial = 1; /* we use a serial number similar to other places to know what an agent has already got */

        public uint Serial
        {
            get
            {
                return m_Serial;
            }
            set
            {
                lock(this)
                {
                    if (value == 0)
                    {
                        m_Serial = 1;
                    }
                    else
                    {
                        m_Serial = value;
                    }
                }
            }
        }

        public void IncrementSerial()
        {
            lock(this)
            {
                if(++m_Serial == 0)
                {
                    m_Serial = 1;
                }
            }
        }

        public float[,] Data = new float[16,16];

        internal uint PackedSerial = 0;
        private byte[] PackedDataBytes = new byte[647]; /* maximum length of a single 16 by 16 patch when packed perfectly bad */
        internal BitPacker PackedData;

        public uint ExtendedPatchID
        {
            get
            {
                return ((uint)X << 16) | (uint)Y;
            }
            set
            {
                X = (int)((value >> 16) & 0xFFFF);
                Y = (int)(value & 0xFFFF);
            }
        }

        public LayerPatch()
        {
            PackedData = new BitPacker(PackedDataBytes);
        }

        public LayerPatch(double defaultHeight)
        {
            PackedData = new BitPacker(PackedDataBytes);
            X = 0;
            Y = 0;
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
            lock (this)
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
                throw new ArgumentException();
            }
            int x, y;
            lock (this)
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
                throw new ArgumentException();
            }
            int x, y;
            lock (this)
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
                lock(this)
                {
                    Data[y, x] = value;
                }
            }
        }

        public byte[] Serialization
        {
            get
            {
                LayerPatch copy;
                lock (this)
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
                    throw new ArgumentException();
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
                lock (this)
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

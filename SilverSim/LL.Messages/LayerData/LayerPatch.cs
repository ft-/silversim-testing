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
                    m_Serial = value;
                }
            }
        }

        public float[,] Data = new float[16,16];

        internal uint PackedSerial = 0;
        private byte[] PackedDataBytes = new byte[647]; /* maximum length of a single 16 by 16 patch when packed perfectly bad */
        internal BitPacker PackedData;

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
            for(y = 0; y < 16; ++y)
            {
                for(x = 0; x < 16; ++x)
                {
                    Data[y, x] = p.Data[y, x];
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
    }
}

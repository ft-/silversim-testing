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
using System.Net;

namespace SilverSim.LL.Messages.Circuit
{
    public class EnableSimulator : Message
    {
        public GridVector GridPosition;
        public IPAddress SimIP;
        public UInt16 SimPort;

        /* EQG extension */
        public GridVector RegionSize;

        public EnableSimulator()
        {

        }

        public override MessageType Number
        {
            get
            {
                return MessageType.EnableSimulator;
            }
        }

        public override bool IsReliable
        {
            get
            {
                return true;
            }
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUInt64(GridPosition.RegionHandle);
            p.WriteBytes(SimIP.GetAddressBytes());
            p.WriteUInt16(SimPort);
        }

        public override SilverSim.Types.Map SerializeEQG()
        {
            SilverSim.Types.Map i = new SilverSim.Types.Map();
            i.Add("Handle", new BinaryData(GridPosition.AsBytes));
            i.Add("IP", new BinaryData(SimIP.GetAddressBytes()));
            i.Add("Port", SimPort);
            i.Add("RegionSizeX", RegionSize.X);
            i.Add("RegionSizeY", RegionSize.Y);

            AnArray arr = new AnArray();
            arr.Add(i);

            SilverSim.Types.Map m = new SilverSim.Types.Map();
            m.Add("SimulatorInfo", arr);

            return m;
        }

        public override string NameEQG
        {
            get
            {
                return "EnableSimulator";
            }
        }
    }
}

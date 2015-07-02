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
using SilverSim.Types.Grid;
using System;
using System.Net;

namespace SilverSim.LL.Messages.Teleport
{
    [UDPMessage(MessageType.TeleportFinish)]
    [Reliable]
    [EventQueueGet("TeleportFinish")]
    public class TeleportFinish : Message
    {
        public UUID AgentID;
        public UInt32 LocationID;
        public IPAddress SimIP;
        public UInt16 SimPort;
        public GridVector GridPosition;
        public string SeedCapability;
        public byte SimAccess;
        public TeleportFlags TeleportFlags;

        /* EQG extension */
        public GridVector RegionSize;

        public TeleportFinish()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteUInt32(LocationID);
            p.WriteBytes(SimIP.GetAddressBytes());
            p.WriteUInt16(SimPort);
            p.WriteUInt64(GridPosition.RegionHandle);
            p.WriteStringLen16(SeedCapability);
            p.WriteUInt8(SimAccess);
            p.WriteUInt32((UInt32)TeleportFlags);
        }

        public override SilverSim.Types.IValue SerializeEQG()
        {
            SilverSim.Types.Map m = new SilverSim.Types.Map();
            m.Add("AgentID", AgentID);
            m.Add("LocationID", LocationID);
            m.Add("RegionHandle", new BinaryData(GridPosition.AsBytes));
            m.Add("SeedCapability", SeedCapability);
            m.Add("SimAccess", SimAccess);
            m.Add("SimIP", new BinaryData(SimIP.GetAddressBytes()));
            m.Add("SimPort", SimPort);
            m.Add("TeleportFlags", (uint)TeleportFlags);
            m.Add("RegionSizeX", RegionSize.X);
            m.Add("RegionSizeY", RegionSize.Y);
            return m;
        }
    }
}

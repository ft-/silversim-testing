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

using SilverSim.Types;
using System;
using System.Net;

namespace SilverSim.Viewer.Messages.Circuit
{
    [UDPMessage(MessageType.CrossedRegion)]
    [Reliable]
    [EventQueueGet("CrossedRegion")]
    [Trusted]
    public class CrossedRegion : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public IPAddress SimIP;
        public UInt16 SimPort;
        public GridVector GridPosition;
        public string SeedCapability;
        public Vector3 Position;
        public Vector3 LookAt;

        /* EQG extension */
        public GridVector RegionSize;

        public CrossedRegion()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteBytes(SimIP.GetAddressBytes());
            p.WriteUInt16(SimPort);
            p.WriteUInt64(GridPosition.RegionHandle);
            p.WriteStringLen16(SeedCapability);
            p.WriteVector3f(Position);
            p.WriteVector3f(LookAt);
        }

        public override IValue SerializeEQG()
        {
            Types.Map i = new Types.Map();
            i.Add("LookAt", LookAt);
            i.Add("Position", Position);

            Types.Map a = new Types.Map();
            a.Add("AgentID", AgentID);
            a.Add("SessionID", SessionID);

            Types.Map r = new Types.Map();
            r.Add("RegionHandle", new BinaryData(GridPosition.AsBytes));
            r.Add("SeedCapability", SeedCapability);
            r.Add("SimIP", new BinaryData(SimIP.GetAddressBytes()));
            r.Add("SimPort", SimPort);
            r.Add("RegionSizeX", RegionSize.X);
            r.Add("RegionSizeY", RegionSize.Y);

            Types.Map m = new Types.Map();
            m.Add("Info", i);
            m.Add("AgentData", a);
            m.Add("RegionData", r);

            return m;
        }
    }
}

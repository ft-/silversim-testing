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

namespace SilverSim.Viewer.Messages.Circuit
{
    [UDPMessage(MessageType.AgentMovementComplete)]
    [Reliable]
    [Trusted]
    public class AgentMovementComplete : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public Vector3 Position;
        public Vector3 LookAt;
        public GridVector GridPosition;
        public UInt32 Timestamp;
        public string ChannelVersion;

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteVector3f(Position);
            p.WriteVector3f(LookAt);
            p.WriteUInt64(GridPosition.RegionHandle);
            p.WriteUInt32(Timestamp);
            p.WriteStringLen16(ChannelVersion);
        }

        public static Message Decode(UDPPacket p) => new AgentMovementComplete()
        {
            AgentID = p.ReadUUID(),
            SessionID = p.ReadUUID(),
            Position = p.ReadVector3f(),
            LookAt = p.ReadVector3f(),
            GridPosition = new GridVector(p.ReadUInt64()),
            Timestamp = p.ReadUInt32(),
            ChannelVersion = p.ReadStringLen16()
        };
    }
}

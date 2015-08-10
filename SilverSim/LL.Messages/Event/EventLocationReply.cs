// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.LL.Messages.Event
{
    [UDPMessage(MessageType.EventLocationReply)]
    [Reliable]
    [Trusted]
    public class EventLocationReply : Message
    {
        public UUID QueryID;
        public bool Success;
        public UUID RegionID;
        public Vector3 RegionPos;

        public EventLocationReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(QueryID);
            p.WriteBoolean(Success);
            p.WriteUUID(RegionID);
            p.WriteVector3f(RegionPos);
        }
    }
}

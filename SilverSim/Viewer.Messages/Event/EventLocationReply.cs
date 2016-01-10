// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Event
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
            p.WriteUUID(QueryID);
            p.WriteBoolean(Success);
            p.WriteUUID(RegionID);
            p.WriteVector3f(RegionPos);
        }

        public static Message Decode(UDPPacket p)
        {
            EventLocationReply m = new EventLocationReply();
            m.QueryID = p.ReadUUID();
            m.Success = p.ReadBoolean();
            m.RegionID = p.ReadUUID();
            m.RegionPos = p.ReadVector3f();
            return m;
        }
    }
}

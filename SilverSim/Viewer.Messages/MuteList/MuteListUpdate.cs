// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.MuteList
{
    [UDPMessage(MessageType.MuteListUpdate)]
    [Reliable]
    [Trusted]
    public class MuteListUpdate : Message
    {
        public UUID AgentID;
        public string Filename;

        public MuteListUpdate()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteStringLen8(Filename);
        }

        public static Message Decode(UDPPacket p)
        {
            MuteListUpdate m = new MuteListUpdate();
            m.AgentID = p.ReadUUID();
            m.Filename = p.ReadStringLen8();
            return m;
        }
    }
}

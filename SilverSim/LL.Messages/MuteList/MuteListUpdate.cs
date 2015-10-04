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
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteStringLen8(Filename);
        }
    }
}

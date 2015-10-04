// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.MuteList
{
    [UDPMessage(MessageType.UseCachedMuteList)]
    [Reliable]
    [Trusted]
    public class UseCachedMuteList : Message
    {
        public UUID AgentID;

        public UseCachedMuteList()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
        }
    }
}

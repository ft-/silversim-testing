// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.LL.Messages.Search
{
    [UDPMessage(MessageType.AvatarPickerRequest)]
    [Reliable]
    [NotTrusted]
    public class AvatarPickerRequest : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID QueryID;
        public string Name;

        public AvatarPickerRequest()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            AvatarPickerRequest m = new AvatarPickerRequest();

            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.QueryID = p.ReadUUID();
            m.Name = p.ReadStringLen8();

            return m;
        }
    }
}

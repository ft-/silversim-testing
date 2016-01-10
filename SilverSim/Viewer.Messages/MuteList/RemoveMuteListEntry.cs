// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.MuteList
{
    [UDPMessage(MessageType.RemoveMuteListEntry)]
    [Reliable]
    [NotTrusted]
    public class RemoveMuteListEntry : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID MuteID;
        public string MuteName;

        public RemoveMuteListEntry()
        {

        }

        public static RemoveMuteListEntry Decode(UDPPacket p)
        {
            RemoveMuteListEntry m = new RemoveMuteListEntry();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.MuteID = p.ReadUUID();
            m.MuteName = p.ReadStringLen8();

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(MuteID);
            p.WriteStringLen8(MuteName);
        }
    }
}

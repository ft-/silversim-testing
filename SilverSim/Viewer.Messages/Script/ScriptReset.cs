// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Script
{
    [UDPMessage(MessageType.ScriptReset)]
    [Reliable]
    [NotTrusted]
    public class ScriptReset : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID ObjectID;
        public UUID ItemID;

        public ScriptReset()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ScriptReset m = new ScriptReset();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.ObjectID = p.ReadUUID();
            m.ItemID = p.ReadUUID();

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(ObjectID);
            p.WriteUUID(ItemID);
        }
    }
}

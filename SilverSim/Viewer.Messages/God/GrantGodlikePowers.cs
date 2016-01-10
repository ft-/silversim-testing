// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.God
{
    [UDPMessage(MessageType.GrantGodlikePowers)]
    [Reliable]
    [Trusted]
    public class GrantGodlikePowers : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public byte GodLevel;
        public UUID Token;

        public GrantGodlikePowers()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUInt8(GodLevel);
            p.WriteUUID(Token);
        }

        public static Message Decode(UDPPacket p)
        {
            GrantGodlikePowers m = new GrantGodlikePowers();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.GodLevel = p.ReadUInt8();
            m.Token = p.ReadUUID();
            return m;
        }
    }
}

// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.LL.Messages.God
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
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUInt8(GodLevel);
            p.WriteUUID(Token);
        }
    }
}

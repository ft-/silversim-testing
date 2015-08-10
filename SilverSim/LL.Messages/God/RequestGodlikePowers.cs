// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.LL.Messages.God
{
    [UDPMessage(MessageType.RequestGodlikePowers)]
    [Reliable]
    [NotTrusted]
    public class RequestGodlikePowers : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public bool IsGodlike;
        public UUID Token;

        public RequestGodlikePowers()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            RequestGodlikePowers m = new RequestGodlikePowers();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.IsGodlike = p.ReadBoolean();
            m.Token = p.ReadUUID();

            return m;
        }
    }
}

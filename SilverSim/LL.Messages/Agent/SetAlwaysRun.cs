// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.LL.Messages.Agent
{
    [UDPMessage(MessageType.SetAlwaysRun)]
    [NotTrusted]
    public class SetAlwaysRun : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public bool AlwaysRun = false;

        public SetAlwaysRun()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            SetAlwaysRun m = new SetAlwaysRun();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.AlwaysRun = p.ReadBoolean();
            return m;
        }
    }
}

// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Viewer.Messages.Friend
{
    [UDPMessage(MessageType.TerminateFriendship)]
    [Reliable]
    [NotTrusted]
    public class TerminateFriendship : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID OtherID;

        public TerminateFriendship()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            TerminateFriendship m = new TerminateFriendship();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.OtherID = p.ReadUUID();

            return m;
        }
    }
}

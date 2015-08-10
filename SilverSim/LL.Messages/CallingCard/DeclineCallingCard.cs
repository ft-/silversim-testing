// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;

namespace SilverSim.LL.Messages.CallingCard
{
    [UDPMessage(MessageType.DeclineCallingCard)]
    [Reliable]
    [NotTrusted]
    public class DeclineCallingCard : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID TransactionID;

        public DeclineCallingCard()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            DeclineCallingCard m = new DeclineCallingCard();

            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.TransactionID = p.ReadUUID();

            return m;
        }
    }
}

// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.CallingCard
{
    [UDPMessage(MessageType.AcceptCallingCard)]
    [Reliable]
    [NotTrusted]
    public class AcceptCallingCard : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID TransactionID;

        public List<UUID> FolderIDs = new List<UUID>();

        public AcceptCallingCard()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            AcceptCallingCard m = new AcceptCallingCard();

            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.TransactionID = p.ReadUUID();
            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                m.FolderIDs.Add(p.ReadUUID());
            }

            return m;
        }
    }
}

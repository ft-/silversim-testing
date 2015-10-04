// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Viewer.Messages.Friend
{
    [UDPMessage(MessageType.AcceptFriendship)]
    [Reliable]
    [NotTrusted]
    public class AcceptFriendship : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID TransactionID;
        public List<UUID> FolderIDs = new List<UUID>();

        public AcceptFriendship()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            AcceptFriendship m = new AcceptFriendship();
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

﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.LL.Messages.Inventory
{
    [UDPMessage(MessageType.RemoveInventoryObjects)]
    [Reliable]
    [NotTrusted]
    public class RemoveInventoryObjects : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public List<UUID> FolderIDs = new List<UUID>();
        public List<UUID> ItemIDs = new List<UUID>();

        public RemoveInventoryObjects()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            RemoveInventoryObjects m = new RemoveInventoryObjects();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                m.FolderIDs.Add(p.ReadUUID());
            }

            c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                m.ItemIDs.Add(p.ReadUUID());
            }

            return m;
        }
    }
}

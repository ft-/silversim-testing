﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Inventory
{
    [UDPMessage(MessageType.MoveInventoryItem)]
    [Reliable]
    [NotTrusted]
    public class MoveInventoryItem : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public bool Stamp;
        public struct InventoryDataEntry
        {
            public UUID ItemID;
            public UUID FolderID;
            public string NewName;
        }
        public List<InventoryDataEntry> InventoryData = new List<InventoryDataEntry>();

        public MoveInventoryItem()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            MoveInventoryItem m = new MoveInventoryItem();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.Stamp = p.ReadBoolean();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                InventoryDataEntry d = new InventoryDataEntry();
                d.ItemID = p.ReadUUID();
                d.FolderID = p.ReadUUID();
                d.NewName = p.ReadStringLen8();
                m.InventoryData.Add(d);
            }

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteBoolean(Stamp);
            p.WriteUInt8((byte)InventoryData.Count);
            foreach(InventoryDataEntry d in InventoryData)
            {
                p.WriteUUID(d.ItemID);
                p.WriteUUID(d.FolderID);
                p.WriteStringLen8(d.NewName);
            }
        }
    }
}

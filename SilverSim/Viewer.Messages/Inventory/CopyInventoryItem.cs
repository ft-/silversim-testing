// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Inventory
{
    [UDPMessage(MessageType.CopyInventoryItem)]
    [Reliable]
    [NotTrusted]
    public class CopyInventoryItem : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public struct InventoryDataEntry
        {
            public UInt32 CallbackID;
            public UUID OldAgentID;
            public UUID OldItemID;
            public UUID NewFolderID;
            public string NewName;
        }
        public List<InventoryDataEntry> InventoryData = new List<InventoryDataEntry>();

        public CopyInventoryItem()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUInt8((byte)InventoryData.Count);
            foreach(InventoryDataEntry d in InventoryData)
            {
                p.WriteUInt32(d.CallbackID);
                p.WriteUUID(d.OldAgentID);
                p.WriteUUID(d.OldItemID);
                p.WriteUUID(d.NewFolderID);
                p.WriteStringLen8(d.NewName);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            CopyInventoryItem m = new CopyInventoryItem();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                InventoryDataEntry d = new InventoryDataEntry();
                d.CallbackID = p.ReadUInt32();
                d.OldAgentID = p.ReadUUID();
                d.OldItemID = p.ReadUUID();
                d.NewFolderID = p.ReadUUID();
                d.NewName = p.ReadStringLen8();
                m.InventoryData.Add(d);
            }

            return m;
        }
    }
}

// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

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

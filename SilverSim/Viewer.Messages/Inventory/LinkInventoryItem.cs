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
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;

namespace SilverSim.Viewer.Messages.Inventory
{
    [UDPMessage(MessageType.LinkInventoryItem)]
    [Reliable]
    [NotTrusted]
    public class LinkInventoryItem : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UInt32 CallbackID;
        public UUID FolderID;
        public UUID TransactionID;
        public UUID OldItemID;
        public AssetType AssetType;
        public InventoryType InvType;
        public string Name;
        public string Description;

        public LinkInventoryItem()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            LinkInventoryItem m = new LinkInventoryItem();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.CallbackID = p.ReadUInt32();
            m.FolderID = p.ReadUUID();
            m.TransactionID = p.ReadUUID();
            m.OldItemID = p.ReadUUID();
            m.AssetType = (AssetType)p.ReadInt8();
            m.InvType = (InventoryType)p.ReadInt8();
            m.Name = p.ReadStringLen8();
            m.Description = p.ReadStringLen8();

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUInt32(CallbackID);
            p.WriteUUID(FolderID);
            p.WriteUUID(TransactionID);
            p.WriteUUID(OldItemID);
            p.WriteInt8((sbyte)AssetType);
            p.WriteInt8((sbyte)InvType);
            p.WriteStringLen8(Name);
            p.WriteStringLen8(Description);
        }
    }
}

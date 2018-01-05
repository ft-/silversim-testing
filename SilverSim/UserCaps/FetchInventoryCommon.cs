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
using SilverSim.Types.Inventory;
using System.Xml;

namespace SilverSim.UserCaps
{
    public abstract class FetchInventoryCommon
    {
        protected static void WriteInventoryItem(InventoryItem item, XmlTextWriter writer, UUID agentID)
        {
            writer.WriteKeyValuePair("agent_id", item.Owner.ID);
            writer.WriteKeyValuePair("asset_id", item.AssetID);
            writer.WriteKeyValuePair("created_at", (uint)item.CreationDate.DateTimeToUnixTime());
            writer.WriteKeyValuePair("desc", item.Description);
            writer.WriteKeyValuePair("flags", (uint)item.Flags);
            writer.WriteKeyValuePair("item_id", item.ID);
            writer.WriteKeyValuePair("name", item.Name);
            writer.WriteKeyValuePair("parent_id", item.ParentFolderID);
            writer.WriteKeyValuePair("type", (int)item.AssetType);
            writer.WriteKeyValuePair("inv_type", (int)item.InventoryType);

            writer.WriteStartElement("key");
            writer.WriteValue("permissions");
            writer.WriteEndElement();
            writer.WriteStartElement("map");
            var basePermissions = (uint)item.Permissions.Base;
            if (agentID == item.Creator.ID)
            {
                basePermissions |= (uint)InventoryPermissionsMask.Transfer | (uint)InventoryPermissionsMask.Copy | (uint)InventoryPermissionsMask.Modify;
            }
            if (agentID == item.Owner.ID)
            {
                basePermissions |= (uint)item.Permissions.Current;
            }
            basePermissions |= (uint)item.Permissions.EveryOne;

            writer.WriteKeyValuePair("base_mask", basePermissions);
            writer.WriteKeyValuePair("creator_id", item.Creator.ID);
            writer.WriteKeyValuePair("everyone_mask", (uint)item.Permissions.EveryOne);
            writer.WriteKeyValuePair("group_id", item.Group.ID);
            writer.WriteKeyValuePair("group_mask", (uint)item.Permissions.Group);
            writer.WriteKeyValuePair("is_owner_group", item.IsGroupOwned);
            writer.WriteKeyValuePair("next_owner_mask", (uint)item.Permissions.NextOwner);
            writer.WriteKeyValuePair("owner_id", item.Owner.ID);
            writer.WriteKeyValuePair("owner_mask", (uint)item.Permissions.Current);
            writer.WriteEndElement();

            writer.WriteStartElement("key");
            writer.WriteValue("sale_info");
            writer.WriteEndElement();
            writer.WriteStartElement("map");
            writer.WriteKeyValuePair("sale_price", item.SaleInfo.Price);
            writer.WriteKeyValuePair("sale_type", (uint)item.SaleInfo.Type);
            writer.WriteEndElement();
        }
    }
}

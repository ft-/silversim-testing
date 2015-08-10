// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Types;
using System.Net;
using System.Xml;
using SilverSim.StructuredData.LLSD;
using SilverSim.Types.Inventory;

namespace SilverSim.LL.Core
{
    public partial class Circuit
    {
        void WriteInventoryItem(InventoryItem item, XmlTextWriter writer)
        {
            writer.WriteKeyValuePair("asset_id", item.AssetID);
            writer.WriteKeyValuePair("created_at", (uint)item.CreationDate.DateTimeToUnixTime());
            writer.WriteKeyValuePair("desc", item.Description);
            writer.WriteKeyValuePair("flags", item.Flags);
            writer.WriteKeyValuePair("item_id", item.ID);
            writer.WriteKeyValuePair("name", item.Name);
            writer.WriteKeyValuePair("parent_id", item.ParentFolderID);
            writer.WriteKeyValuePair("type", (uint)item.AssetType);
            writer.WriteKeyValuePair("inv_type", (uint)item.InventoryType);

            writer.WriteStartElement("key");
            writer.WriteValue("permissions");
            writer.WriteEndElement();
            writer.WriteStartElement("map");
            uint basePermissions = (uint)item.Permissions.Base;
            if (AgentID == item.Creator.ID)
            {
                basePermissions |= (uint)InventoryPermissionsMask.Transfer | (uint)InventoryPermissionsMask.Copy | (uint)InventoryPermissionsMask.Modify;
            }
            if (AgentID == item.Owner.ID)
            {
                basePermissions |= (uint)item.Permissions.Current;
            }
            basePermissions |= (uint)item.Permissions.EveryOne;

            writer.WriteKeyValuePair("base_mask", (uint)basePermissions);
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

        void Cap_FetchInventory2(HttpRequest httpreq)
        {
            IValue o;
            if (httpreq.Method != "POST")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            try
            {
                o = LLSD_XML.Deserialize(httpreq.Body);
            }
            catch (Exception e)
            {
                m_Log.WarnFormat("Invalid LLSD_XML: {0} {1}", e.Message, e.StackTrace.ToString());
                httpreq.ErrorResponse(HttpStatusCode.UnsupportedMediaType, "Unsupported Media Type");
                return;
            }
            if (!(o is Map))
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML");
                return;
            }

            Map reqmap = (Map)o;

            HttpResponse res = httpreq.BeginResponse();
            XmlTextWriter text = new XmlTextWriter(res.GetOutputStream(), UTF8NoBOM);
            List<UUID> baditems = new List<UUID>();
            text.WriteStartElement("llsd");
            text.WriteStartElement("map");
            text.WriteKeyValuePair("agent_id", AgentID);
            bool wroteheader = false;

            foreach(IValue iv in (AnArray)reqmap["items"])
            {
                if(!(iv is Map))
                {
                    continue;
                }

                Map itemmap = (Map) iv;
                if(!itemmap.ContainsKey("item_id"))
                {
                    continue;
                }
                UUID itemid = itemmap["item_id"].AsUUID;
                InventoryItem item;
                if(itemid == UUID.Zero)
                {
                    baditems.Add(itemid);
                    continue;
                }
                try
                {
                    item = Agent.InventoryService.Item[AgentID, itemid];
                }
                catch
                {
                    baditems.Add(itemid);
                    continue;
                }
                if (!wroteheader)
                {
                    wroteheader = true;
                    text.WriteNamedValue("key", "items");
                    text.WriteStartElement("array");
                }
                WriteInventoryItem(item, text);
            }
            if(wroteheader)
            {
                text.WriteEndElement();
            }
            if(baditems.Count != 0)
            {
                text.WriteStartElement("key");
                text.WriteValue("bad_items");
                text.WriteEndElement();
                text.WriteStartElement("array");
                foreach(UUID id in baditems)
                {
                    text.WriteNamedValue("uuid", id);
                }
                text.WriteEndElement();
            }
            text.WriteEndElement();
            text.WriteEndElement();
            text.Flush();

            res.Close();
        }
    }
}

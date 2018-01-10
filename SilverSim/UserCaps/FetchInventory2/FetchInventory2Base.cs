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

using log4net;
using SilverSim.Main.Common.HttpServer;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Types;
using SilverSim.Types.Inventory;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Collections.Generic;
using System.Net;

namespace SilverSim.UserCaps.FetchInventory2
{
    public class FetchInventory2Base : FetchInventoryCommon
    {
        private static readonly ILog m_Log = LogManager.GetLogger("FETCHINVENTORY2");

        protected static void HandleHttpRequest(HttpRequest httpreq, InventoryServiceInterface inventoryService, UUID agentID, UUID ownerID)
        {
            IValue o;
            if (httpreq.Method != "POST")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            try
            {
                o = LlsdXml.Deserialize(httpreq.Body);
            }
            catch (Exception e)
            {
                m_Log.WarnFormat("Invalid LLSD_XML: {0} {1}", e.Message, e.StackTrace);
                httpreq.ErrorResponse(HttpStatusCode.UnsupportedMediaType, "Unsupported Media Type");
                return;
            }

            var reqmap = o as Map;
            if (reqmap == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML");
                return;
            }

            using (var res = httpreq.BeginResponse("application/llsd+xml"))
            using (var text = res.GetOutputStream().UTF8XmlTextWriter())
            {
                var baditems = new List<UUID>();
                text.WriteStartElement("llsd");
                text.WriteStartElement("map");
                text.WriteKeyValuePair("agent_id", agentID);
                bool wroteheader = false;

                foreach (var iv in (AnArray)reqmap["items"])
                {
                    var itemmap = iv as Map;
                    if (itemmap == null)
                    {
                        continue;
                    }

                    if (!itemmap.ContainsKey("item_id"))
                    {
                        continue;
                    }
                    var itemid = itemmap["item_id"].AsUUID;
                    InventoryItem item;
                    if (itemid == UUID.Zero)
                    {
                        baditems.Add(itemid);
                        continue;
                    }
                    try
                    {
                        item = inventoryService.Item[ownerID, itemid];
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
                    text.WriteStartElement("map");
                    WriteInventoryItem(item, text, agentID);
                    text.WriteEndElement();
                }
                if (wroteheader)
                {
                    text.WriteEndElement();
                }
                if (baditems.Count != 0)
                {
                    text.WriteStartElement("key");
                    text.WriteValue("bad_items");
                    text.WriteEndElement();
                    text.WriteStartElement("array");
                    foreach (var id in baditems)
                    {
                        text.WriteNamedValue("uuid", id);
                    }
                    text.WriteEndElement();
                }
                text.WriteEndElement();
                text.WriteEndElement();
            }
        }

    }
}

﻿// SilverSim is distributed under the terms of the
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
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Net;

namespace SilverSim.UserCaps.CreateInventoryCategory
{
    public abstract class CreateInventoryCategoryBase
    {
        private static readonly ILog m_Log = LogManager.GetLogger("CREATEINVENTORYCATEGORY");

        protected static void HandleHttpRequest(HttpRequest httpreq, InventoryServiceInterface inventoryService, UGUI agent)
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

            var folder = new InventoryFolder
            {
                ID = reqmap["folder_id"].AsUUID,
                Owner = agent,
                ParentFolderID = reqmap["parent_id"].AsUUID,
                DefaultType = (AssetType)reqmap["type"].AsInt,
                Name = reqmap["name"].ToString(),
                Version = 1
            };
            try
            {
                inventoryService.Folder.Add(folder);
            }
            catch
            {
                httpreq.ErrorResponse(HttpStatusCode.InternalServerError, "Internal Server Error");
                return;
            }

            var resmap = new Map
            {
                { "folder_id", folder.ID },
                { "parent_id", folder.ParentFolderID },
                { "type", (int)folder.DefaultType },
                { "name", folder.Name }
            };
            using (var res = httpreq.BeginResponse())
            using (var stream = res.GetOutputStream())
            {
                LlsdXml.Serialize(resmap, stream);
            }
        }
    }
}

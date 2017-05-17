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
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Transfer;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Inventory;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;

namespace SilverSim.Viewer.Core.Capabilities
{
    public class CopyInventoryFromNotecard : ICapabilityInterface
    {
        public class NotecardAssetTransfer : AssetTransferWorkItem
        {
            public NotecardAssetTransfer(AssetServiceInterface dest, AssetServiceInterface src, List<UUID> assetids)
                : base(dest, src, assetids, ReferenceSource.Source)
            {

            }

            public override void AssetTransferComplete()
            {
                /* nothing to do */
            }

            public override void AssetTransferFailed(Exception e)
            {
                /* nothing to do for now */
            }
        }

        readonly ViewerAgent m_Agent;
        readonly SceneInterface m_Scene;
        readonly string m_RemoteIP;
        private static readonly ILog m_Log = LogManager.GetLogger("COPY INVENTORY FROM NOTECARD");

        public CopyInventoryFromNotecard(ViewerAgent agent, SceneInterface scene, string remoteip)
        {
            m_Agent = agent;
            m_Scene = scene;
            m_RemoteIP = remoteip;
        }

        public string CapabilityName
        {
            get
            {
                return "CopyInventoryFromNotecard";
            }
        }

        public void HttpRequestHandler(HttpRequest httpreq)
        {
            if (httpreq.CallerIP != m_RemoteIP)
            {
                httpreq.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }
            if (httpreq.Method != "POST")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            Map reqmap;
            try
            {
                reqmap = LlsdXml.Deserialize(httpreq.Body) as Map;
            }
            catch
            {
                httpreq.ErrorResponse(HttpStatusCode.UnsupportedMediaType, "Unsupported Media Type");
                return;
            }
            if (null == reqmap)
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML");
                return;
            }

            if (!m_Agent.IsInScene(m_Scene))
            {
                return;
            }

            var notecardID = reqmap["notecard-id"].AsUUID;
            var objectID = reqmap["object-id"].AsUUID;
            var itemID = reqmap["item-id"].AsUUID;
            var destinationFolderID = reqmap["folder-id"].AsUUID;
            var callbackID = reqmap["callback-id"].AsUInt;

            ObjectPart part;
            ObjectPartInventoryItem item;
            Notecard nc = null;
            InventoryFolder destinationFolder = null;
            AssetData data;

            if (m_Scene.Primitives.TryGetValue(objectID, out part) &&
                part.Inventory.TryGetValue(itemID, out item) &&
                item.InventoryType == InventoryType.Notecard &&
                (destinationFolderID == UUID.Zero || m_Agent.InventoryService.Folder.TryGetValue(destinationFolderID, out destinationFolder)) &&
                m_Scene.AssetService.TryGetValue(item.AssetID, out data))
            {
                nc = new Notecard(data);
            }

            var transferItems = new List<UUID>();
            var destFolder = new Dictionary<AssetType, InventoryFolder>();
            if(null != nc)
            {
                foreach (var ncitem in nc.Inventory.Values)
                {
                    try
                    {
                        if(destinationFolderID == UUID.Zero &&
                            !destFolder.ContainsKey(ncitem.AssetType))
                        {
                            if(!m_Agent.InventoryService.Folder.TryGetValue(m_Agent.ID, ncitem.AssetType, out destinationFolder) &&
                                !m_Agent.InventoryService.Folder.TryGetValue(m_Agent.ID, AssetType.Object, out destinationFolder))
                            {
                                m_Log.WarnFormat("Failed to copy notecard inventory {0} to agent {1} ({2}): No Folder found for {3}", ncitem.Name, m_Agent.Owner.FullName, m_Agent.ID, ncitem.AssetType.ToString());
                                continue;
                            }
                            else
                            {
                                destFolder.Add(ncitem.AssetType, destinationFolder);
                            }
                        }
                        var assetID = CreateInventoryItemFromNotecard(destinationFolder, ncitem, callbackID);
                        if (!transferItems.Contains(assetID))
                        {
                            transferItems.Add(assetID);
                        }
                    }
                    catch(Exception e)
                    {
                        m_Log.WarnFormat("Failed to copy notecard inventory {0} to agent {1} ({2}): {3}: {4}\n{5}", ncitem.Name, m_Agent.Owner.FullName, m_Agent.ID, e.GetType().FullName, e.Message, e.StackTrace);
                    }
                }
            }

            var transferItem = new NotecardAssetTransfer(m_Agent.AssetService, m_Scene.AssetService, transferItems);
            ThreadPool.UnsafeQueueUserWorkItem(HandleAssetTransferWorkItem, transferItem);

            using (HttpResponse httpres = httpreq.BeginResponse())
            {
                httpres.ContentType = "application/llsd+xml";
                using (Stream outStream = httpres.GetOutputStream())
                {
                    LlsdXml.Serialize(new Map(), outStream);
                }
            }
        }

        void HandleAssetTransferWorkItem(object o)
        {
            AssetTransferWorkItem wi = (AssetTransferWorkItem)o;
            wi.ProcessAssetTransfer();
        }

        UUID CreateInventoryItemFromNotecard(InventoryFolder destinationFolder, NotecardInventoryItem ncitem, uint callbackID)
        {
            var item = new InventoryItem()
            {
                InventoryType = ncitem.InventoryType,
                AssetType = ncitem.AssetType,
                Description = ncitem.Description,
                Name = ncitem.Name
            };
            item.LastOwner = item.Owner;
            item.Owner = m_Agent.Owner;
            item.Creator = ncitem.Creator;
            item.SaleInfo.Type = InventoryItem.SaleInfoData.SaleType.NoSale;
            item.SaleInfo.Price = 0;
            item.SaleInfo.PermMask = InventoryPermissionsMask.All;
            item.ParentFolderID = destinationFolder.ID;

            item.Permissions.Base = ncitem.Permissions.Base & ncitem.Permissions.NextOwner;
            item.Permissions.Current = ncitem.Permissions.NextOwner;
            item.Permissions.Group = InventoryPermissionsMask.None;
            item.Permissions.EveryOne = InventoryPermissionsMask.None;
            item.Permissions.NextOwner = ncitem.Permissions.NextOwner;


            m_Agent.InventoryService.Item.Add(item);
            m_Agent.SendMessageAlways(new Messages.Inventory.UpdateCreateInventoryItem(m_Agent.ID, true, UUID.Zero, item, callbackID), m_Scene.ID);
            return item.AssetID;
        }
    }
}

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

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Transfer;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Inventory;
using SilverSim.Viewer.Messages;
using System.Collections.Generic;
using System.Threading;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        private sealed class AgentRezObjectHandler  : RezObjectHandler
        {
            public AgentRezObjectHandler(SceneInterface scene, Vector3 targetpos, UUID assetid, AssetServiceInterface source, UGUI rezzingagent, SceneInterface.RezObjectParams rezparams)
                : base(scene, targetpos, assetid, source, rezzingagent, rezparams)
            {
            }

            public AgentRezObjectHandler(SceneInterface scene, Vector3 targetpos, List<UUID> assetids, AssetServiceInterface source, UGUI rezzingagent, SceneInterface.RezObjectParams rezparams)
                : base(scene, targetpos, assetids, source, rezzingagent, rezparams)
            {
            }
        }

        private sealed class AgentRezRestoreObjectHandler : RezRestoreObjectHandler
        {
            public AgentRezRestoreObjectHandler(SceneInterface scene, UUID assetid, AssetServiceInterface source, UGUI rezzingagent, UGI rezzinggroup, InventoryItem sourceItem)
                : base(scene, assetid, source, rezzingagent, rezzinggroup, sourceItem)
            {
            }
        }

        [PacketHandler(MessageType.RezRestoreToWorld)]
        public void HandleRezRestoreToWorld(Message m)
        {
            var req = (Messages.Object.RezRestoreToWorld)m;
            if (req.AgentID != req.CircuitAgentID || req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            InventoryItem item;
            try
            {
                item = InventoryService.Item[Owner.ID, req.ItemID];
            }
            catch
            {
                SendAlertMessage("ALERT: CantFindInvItem", m.CircuitSceneID);
                return;
            }
            if (item.AssetType == AssetType.Link)
            {
                try
                {
                    item = InventoryService.Item[Owner.ID, req.ItemID];
                }
                catch
                {
                    SendAlertMessage("ALERT: CantFindInvItem", m.CircuitSceneID);
                    return;
                }
            }
            if (item.AssetType != AssetType.Object)
            {
                SendAlertMessage("ALERT: InvalidObjectParams", m.CircuitSceneID);
                return;
            }

            UGI restoreGroup = UGI.Unknown;

            if(!(Circuits[m.CircuitSceneID].Scene.GroupsNameService?.TryGetValue(req.GroupID, out restoreGroup) ?? false))
            {
                restoreGroup = UGI.Unknown;
            }

            var rezHandler = new AgentRezRestoreObjectHandler(
                Circuits[m.CircuitSceneID].Scene,
                item.AssetID,
                AssetService,
                Owner,
                restoreGroup,
                item);

            ThreadPool.UnsafeQueueUserWorkItem(HandleAssetTransferWorkItem, rezHandler);
        }

        [PacketHandler(MessageType.RezObject)]
        public void HandleRezObject(Message m)
        {
            var req = (Messages.Object.RezObject)m;
            if(req.AgentID != req.CircuitAgentID || req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            InventoryItem item;
            try
            {
                item = InventoryService.Item[Owner.ID, req.InventoryData.ItemID];
            }
            catch
            {
                SendAlertMessage("ALERT: CantFindInvItem", m.CircuitSceneID);
                return;
            }
            if(item.AssetType == AssetType.Link)
            {
                try
                {
                    item = InventoryService.Item[Owner.ID, req.InventoryData.ItemID];
                }
                catch
                {
                    SendAlertMessage("ALERT: CantFindInvItem", m.CircuitSceneID);
                    return;
                }
            }
            if(item.AssetType != AssetType.Object)
            {
                SendAlertMessage("ALERT: InvalidObjectParams", m.CircuitSceneID);
                return;
            }
            var rezparams = new SceneInterface.RezObjectParams
            {
                RayStart = req.RezData.RayStart,
                RayEnd = req.RezData.RayEnd,
                RayTargetID = req.RezData.RayTargetID,
                RayEndIsIntersection = req.RezData.RayEndIsIntersection,
                RezSelected = req.RezData.RezSelected,
                RemoveItem = req.RezData.RemoveItem,
                Scale = Vector3.One,
                Rotation = Quaternion.Identity,
                ItemFlags = req.RezData.ItemFlags,
                GroupMask = req.RezData.GroupMask,
                EveryoneMask = req.RezData.EveryoneMask,
                NextOwnerMask = req.RezData.NextOwnerMask,
                SourceItem = item
            };
            var rezHandler = new AgentRezObjectHandler(
                Circuits[m.CircuitSceneID].Scene,
                rezparams.RayEnd,
                item.AssetID,
                AssetService,
                Owner,
                rezparams);

            ThreadPool.UnsafeQueueUserWorkItem(HandleAssetTransferWorkItem, rezHandler);
        }

        private void HandleAssetTransferWorkItem(object o)
        {
            var wi = (AssetTransferWorkItem)o;
            wi.ProcessAssetTransfer();
        }

        [PacketHandler(MessageType.ObjectDuplicateOnRay)]
        public void HandleObjectDuplicateOnRay(Message m)
        {
            var req = (Messages.Object.ObjectDuplicateOnRay)m;
            if (req.AgentID != req.CircuitAgentID || req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            SceneInterface scene = Circuits[m.CircuitSceneID].Scene;

            var objgroups = new List<ObjectGroup>();
            foreach(uint localid in req.ObjectLocalIDs)
            {
                ObjectPart part;
                if(scene.Primitives.TryGetValue(localid, out part))
                {
                    objgroups.Add(new ObjectGroup(part.ObjectGroup));
                }
            }

            var rezparams = new SceneInterface.RezObjectParams
            {
                RayStart = req.RayStart,
                RayEnd = req.RayEnd,
                RayTargetID = req.RayTargetID,
                RayEndIsIntersection = req.RayEndIsIntersection,
                RezSelected = false,
                RemoveItem = false,
                Scale = Vector3.One,
                Rotation = Quaternion.Identity,
                ItemFlags = 0,
                GroupMask = 0,
                EveryoneMask = 0,
                NextOwnerMask = 0
            };

            scene.RezObjects(objgroups, rezparams);
        }

        [PacketHandler(MessageType.RezObjectFromNotecard)]
        public void HandleRezObjectFromNotecard(Message m)
        {
            var req = (Messages.Object.RezObjectFromNotecard)m;
            if (req.AgentID != req.CircuitAgentID || req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            SceneInterface scene = Circuits[m.CircuitSceneID].Scene;
            ObjectPart part;
            Notecard nc;
            AssetData data;

            if (req.NotecardData.ObjectID == UUID.Zero)
            {
                /* from inventory */
                InventoryItem item;

                if (!InventoryService.Item.TryGetValue(req.AgentID, req.NotecardData.NotecardItemID, out item))
                {
                    return;
                }

                if (item.AssetType != AssetType.Notecard)
                {
                    return;
                }

                if (!AssetService.TryGetValue(item.AssetID, out data))
                {
                    return;
                }

                try
                {
                    nc = new Notecard(data);
                }
                catch
                {
                    return;
                }
            }
            else if(scene.Primitives.TryGetValue(req.NotecardData.ObjectID, out part))
            {
                /* from object */
                ObjectPartInventoryItem item;
                if(!part.Inventory.TryGetValue(req.NotecardData.NotecardItemID, out item))
                {
                    return;
                }

                if (item.AssetType != AssetType.Notecard)
                {
                    return;
                }


                if (!scene.AssetService.TryGetValue(item.AssetID, out data))
                {
                    return;
                }

                try
                {
                    nc = new Notecard(data);
                }
                catch
                {
                    return;
                }
            }
            else
            {
                return;
            }

            var assetids = new List<UUID>();

            foreach(UUID itemid in req.InventoryData)
            {
                NotecardInventoryItem item;
                if(nc.Inventory.TryGetValue(itemid, out item) && item.AssetType == AssetType.Object)
                {
                    assetids.Add(item.AssetID);
                }
            }

            var rezparams = new SceneInterface.RezObjectParams
            {
                RayStart = req.RezData.RayStart,
                RayEnd = req.RezData.RayEnd,
                RayTargetID = req.RezData.RayTargetID,
                RayEndIsIntersection = req.RezData.RayEndIsIntersection,
                RezSelected = req.RezData.RezSelected,
                RemoveItem = req.RezData.RemoveItem,
                Scale = Vector3.One,
                Rotation = Quaternion.Identity,
                ItemFlags = req.RezData.ItemFlags,
                GroupMask = req.RezData.GroupMask,
                EveryoneMask = req.RezData.EveryoneMask,
                NextOwnerMask = req.RezData.NextOwnerMask
            };
            var rezHandler = new AgentRezObjectHandler(
                Circuits[m.CircuitSceneID].Scene,
                rezparams.RayEnd,
                assetids,
                AssetService,
                Owner,
                rezparams);

            ThreadPool.UnsafeQueueUserWorkItem(HandleAssetTransferWorkItem, rezHandler);
        }
    }
}

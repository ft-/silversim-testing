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

using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Transfer;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using SilverSim.Viewer.Messages;
using System.Threading;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        private sealed class AgentRezObjectHandler  : RezObjectHandler
        {
            public AgentRezObjectHandler(SceneInterface scene, Vector3 targetpos, UUID assetid, AssetServiceInterface source, UUI rezzingagent, SceneInterface.RezObjectParams rezparams, InventoryPermissionsMask itemOwnerPermissions = InventoryPermissionsMask.Every)
                : base(scene, targetpos, assetid, source, rezzingagent, rezparams, itemOwnerPermissions)
            {
            }
        }

        private sealed class AgentRezRestoreObjectHandler : RezRestoreObjectHandler
        {
            public AgentRezRestoreObjectHandler(SceneInterface scene, UUID assetid, AssetServiceInterface source, UUI rezzingagent, InventoryPermissionsMask itemOwnerPermissions = InventoryPermissionsMask.Every)
                : base(scene, assetid, source, rezzingagent, itemOwnerPermissions)
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
            var rezHandler = new AgentRezRestoreObjectHandler(
                Circuits[m.CircuitSceneID].Scene,
                item.AssetID,
                AssetService,
                Owner);

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
                NextOwnerMask = req.RezData.NextOwnerMask
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

        [PacketHandler(MessageType.RezObjectFromNotecard)]
        public void HandleRezObjectFromNotecard(Message m)
        {
            var req = (Messages.Object.RezObjectFromNotecard)m;
            if (req.AgentID != req.CircuitAgentID || req.SessionID != req.CircuitSessionID)
            {
                return;
            }
        }
    }
}

// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common.Transfer;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Inventory;
using SilverSim.Viewer.Messages;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        sealed class AgentRezObjectHandler  : RezObjectHandler
        {
            public AgentRezObjectHandler(SceneInterface scene, Vector3 targetpos, UUID assetid, AssetServiceInterface source, UUI rezzingagent, SceneInterface.RezObjectParams rezparams, InventoryPermissionsMask itemOwnerPermissions = InventoryPermissionsMask.Every)
                : base(scene, targetpos, assetid, source, rezzingagent, rezparams, itemOwnerPermissions)
            {

            }

            public override void PostProcessObjectGroups(List<ObjectGroup> grps)
            {
                foreach (ObjectGroup grp in grps)
                {
                    foreach (ObjectPart part in grp.Values)
                    {
                        UUID oldID = part.ID;
                        UUID newID = UUID.Random;
                        part.ID = newID;
                        grp.ChangeKey(oldID, newID);
                    }
                }
            }
        }

        [PacketHandler(MessageType.RezObject)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleRezObject(Message m)
        {
            Messages.Object.RezObject req = (Messages.Object.RezObject)m;
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
                SendAlertMessage("ALERT: ", m.CircuitSceneID);
                return;
            }
            if(item.AssetType == Types.Asset.AssetType.Link)
            {
                try
                {
                    item = InventoryService.Item[Owner.ID, req.InventoryData.ItemID];
                }
                catch
                {
                    SendAlertMessage("ALERT: ", m.CircuitSceneID);
                    return;
                }
            }
            if(item.AssetType != Types.Asset.AssetType.Object)
            {
                SendAlertMessage("ALERT: InvalidObjectParams", m.CircuitSceneID);
                return;
            }
            SceneInterface.RezObjectParams rezparams = new SceneInterface.RezObjectParams();
            rezparams.RayStart = req.RezData.RayStart;
            rezparams.RayEnd = req.RezData.RayEnd;
            rezparams.RayTargetID = req.RezData.RayTargetID;
            rezparams.RayEndIsIntersection = req.RezData.RayEndIsIntersection;
            rezparams.RezSelected = req.RezData.RezSelected;
            rezparams.RemoveItem = req.RezData.RemoveItem;
            rezparams.Scale = Vector3.One;
            rezparams.Rotation = Quaternion.Identity;
            rezparams.ItemFlags = req.RezData.ItemFlags;
            rezparams.GroupMask = req.RezData.GroupMask;
            rezparams.EveryoneMask = req.RezData.EveryoneMask;
            rezparams.NextOwnerMask = req.RezData.NextOwnerMask;

            AgentRezObjectHandler rezHandler = new AgentRezObjectHandler(
                Circuits[m.ReceivedOnCircuitCode].Scene, 
                rezparams.RayEnd, 
                item.AssetID, 
                AssetService, 
                Owner, 
                rezparams);

            ThreadPool.UnsafeQueueUserWorkItem(HandleAssetTransferWorkItem, rezHandler);
        }

        void HandleAssetTransferWorkItem(object o)
        {
            AssetTransferWorkItem wi = (AssetTransferWorkItem)o;
            wi.ProcessAssetTransfer();
        }

        [PacketHandler(MessageType.RezObjectFromNotecard)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleRezObjectFromNotecard(Message m)
        {
            Messages.Object.RezObjectFromNotecard req = (Messages.Object.RezObjectFromNotecard)m;
            if (req.AgentID != req.CircuitAgentID || req.SessionID != req.CircuitSessionID)
            {
                return;
            }
        }
    }
}

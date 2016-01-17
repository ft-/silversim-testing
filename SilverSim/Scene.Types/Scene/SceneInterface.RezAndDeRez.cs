// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Viewer.Messages;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using SilverSim.Scene.Types.Transfer;
using DeRezAction = SilverSim.Viewer.Messages.Object.DeRezObject.DeRezAction;

namespace SilverSim.Scene.Types.Scene
{
    public abstract partial class SceneInterface
    {
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidLargeStructureRule")]
        public struct RezObjectParams
        {
            public Vector3 RayStart;
            public Vector3 RayEnd;
            public UUID RayTargetID;
            public bool RayEndIsIntersection;
            public bool RezSelected;
            public bool RemoveItem;
            public Vector3 Scale;
            public Quaternion Rotation;
            public UInt32 ItemFlags;
            public InventoryPermissionsMask GroupMask;
            public InventoryPermissionsMask EveryoneMask;
            public InventoryPermissionsMask NextOwnerMask;
        }

        public List<UInt32> RezObjects(List<ObjectGroup> groups, RezObjectParams rezparams)
        {
            throw new NotImplementedException();
        }

        public UInt32 RezObject(ObjectGroup group, RezObjectParams rezparams)
        {
            throw new NotImplementedException();
        }

        [PacketHandler(MessageType.DeRezObject)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        internal void HandleDeRezObject(Message m)
        {
            Viewer.Messages.Object.DeRezAck ackres;
            Viewer.Messages.Object.DeRezObject req = (Viewer.Messages.Object.DeRezObject)m;
            if (req.AgentID != m.CircuitAgentID ||
                req.SessionID != m.CircuitSessionID)
            {
                return;
            }

            IAgent agent;
            List<ObjectGroup> objectgroups = new List<ObjectGroup>();
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            try
            {
                foreach (UInt32 localid in req.ObjectLocalIDs)
                {
                    try
                    {
                        ObjectGroup grp = Primitives[localid].ObjectGroup;
                        objectgroups.Add(grp);
                    }
                    catch
                    {
                        agent.SendAlertMessage("ALERT: DeleteFailObjNotFound", ID);
                    }
                }
            }
            catch
            {
                /* no action required */
            }

            bool isActiveGod = agent.IsActiveGod;

            switch (req.Destination)
            {
                case DeRezAction.GodTakeCopy:
                    if (!isActiveGod || !agent.IsInScene(this))
                    {
                        return;
                    }
                    break;

                case DeRezAction.DeleteToTrash:
                    foreach (ObjectGroup grp in objectgroups)
                    {
                        if (!isActiveGod || !agent.IsInScene(this))
                        {
                            return;
                        }
                        else if (!CanDelete(agent, grp, grp.Position))
                        {
                            agent.SendAlertMessage(this.GetLanguageString(agent.CurrentCulture, "NoPermissionToDeleteObject", "No permission to delete object"), ID);
                            return;
                        }
                    }
                    break;

                case DeRezAction.ReturnToOwner:
                    foreach (ObjectGroup grp in objectgroups)
                    {
                        if (!isActiveGod || !agent.IsInScene(this))
                        {
                            return;
                        }
                        else if (!CanReturn(agent, grp, grp.Position))
                        {
                            agent.SendAlertMessage(string.Format(this.GetLanguageString(agent.CurrentCulture, "NoPermissionToReturnObjectToOwner", "No permission to return object '{0}' to owner"), grp.Name), ID);
                            ackres = new Viewer.Messages.Object.DeRezAck();
                            ackres.TransactionID = req.TransactionID;
                            ackres.Success = false;
                            agent.SendMessageAlways(ackres, ID);
                            return;
                        }
                    }
                    break;

                case DeRezAction.SaveIntoAgentInventory:
                    ackres = new Viewer.Messages.Object.DeRezAck();
                    ackres.TransactionID = req.TransactionID;
                    ackres.Success = false;
                    agent.SendMessageAlways(ackres, ID);
                    return;

                case DeRezAction.Take:
                    foreach (ObjectGroup grp in objectgroups)
                    {
                        if (!isActiveGod || !agent.IsInScene(this))
                        {
                            return;
                        }
                        else if (!CanTake(agent, grp, grp.Position))
                        {
                            agent.SendAlertMessage(string.Format(this.GetLanguageString(agent.CurrentCulture, "NoPermissionToTakeObject", "No permission to take object '{0}'"), grp.Name), ID);
                            ackres = new Viewer.Messages.Object.DeRezAck();
                            ackres.TransactionID = req.TransactionID;
                            ackres.Success = false;
                            agent.SendMessageAlways(ackres, ID);
                            return;
                        }
                    }
                    break;

                case DeRezAction.TakeCopy:
                    foreach (ObjectGroup grp in objectgroups)
                    {
                        if (!CanTakeCopy(agent, grp, grp.Position))
                        {
                            agent.SendAlertMessage(string.Format(this.GetLanguageString(agent.CurrentCulture, "NoPermissionToCopyObject", "No permission to copy object '{0}'"), grp.Name), ID);
                            ackres = new Viewer.Messages.Object.DeRezAck();
                            ackres.TransactionID = req.TransactionID;
                            ackres.Success = false;
                            agent.SendMessageAlways(ackres, ID);
                            return;
                        }
                    }
                    break;

                default:
                    agent.SendAlertMessage(this.GetLanguageString(agent.CurrentCulture, "InvalidDerezRequestByViewer", "Invalid derez request by viewer"), ID);
                    ackres = new Viewer.Messages.Object.DeRezAck();
                    ackres.TransactionID = req.TransactionID;
                    ackres.Success = false;
                    agent.SendMessageAlways(ackres, ID);
                    return;
            }

            Dictionary<UUI, List<InventoryItem>> copyItems = new Dictionary<UUI, List<InventoryItem>>();
            if (req.Destination != DeRezAction.ReturnToOwner)
            {
                foreach (ObjectGroup grp in objectgroups)
                {
                    UUID assetID;
                    bool changePermissions = false;
                    UUI targetAgent;
                    switch (req.Destination)
                    {
                        case DeRezAction.ReturnToOwner:
                            targetAgent = grp.Owner;
                            break;

                        default:
                            targetAgent = agent.Owner;
                            break;
                    }

                    if (!targetAgent.EqualsGrid(agent.Owner))
                    {
                        assetID = grp.NextOwnerAssetID;
                        if (assetID == UUID.Zero)
                        {
                            assetID = AssetService.GenerateNextOwnerAssets(grp);
                        }
                        changePermissions = true;
                    }
                    else
                    {
                        assetID = grp.OriginalAssetID;
                        if(UUID.Zero == assetID)
                        {
                            AssetData asset = grp.Asset(XmlSerializationOptions.WriteOwnerInfo | XmlSerializationOptions.WriteXml2);
                            asset.ID = UUID.Random;
                            AssetService.Store(asset);
                            grp.OriginalAssetID = asset.ID;
                            assetID = asset.ID;
                        }
                    }
                    if(!copyItems.ContainsKey(targetAgent))
                    {
                        copyItems.Add(targetAgent, new List<InventoryItem>());
                    }
                    InventoryItem item = new InventoryItem();
                    item.AssetID = assetID;
                    item.AssetType = AssetType.Object;
                    item.InventoryType = InventoryType.Object;
                    item.Name = grp.Name;
                    item.Description = grp.Description;
                    item.Owner = targetAgent;
                    item.Creator = grp.RootPart.Creator;
                    item.CreationDate = grp.RootPart.CreationDate;
                    item.Permissions.Base = changePermissions ? grp.RootPart.NextOwnerMask : grp.RootPart.BaseMask;
                    item.Permissions.Current = item.Permissions.Base;
                    item.Permissions.Group = InventoryPermissionsMask.None;
                    item.Permissions.NextOwner = grp.RootPart.NextOwnerMask;
                    item.Permissions.EveryOne = InventoryPermissionsMask.None;
                    copyItems[grp.Owner].Add(item);
                }
            }

            foreach(KeyValuePair<UUI, List<InventoryItem>> kvp in copyItems)
            {
                List<UUID> assetIDs = new List<UUID>();
                IAgent toAgent;
                foreach (InventoryItem item in kvp.Value)
                {
                    if (!assetIDs.Contains(item.AssetID))
                    {
                        assetIDs.Add(item.AssetID);
                    }
                }
                if(Agents.TryGetValue(kvp.Key.ID, out toAgent))
                {
                    new ObjectTransferItem(agent,
                        this,
                        assetIDs, 
                        kvp.Value, 
                        req.Destination == DeRezAction.DeleteToTrash ? AssetType.TrashFolder : AssetType.Object).QueueWorkItem();
                }
                else
                {
#warning Implement handling for agents not on region
                }
            }

            if (req.Destination != DeRezAction.TakeCopy &&
                req.Destination != DeRezAction.GodTakeCopy)
            {
                foreach (ObjectGroup grp in objectgroups)
                {
                    grp.Scene.Remove(grp);
                }
            }

            ackres = new Viewer.Messages.Object.DeRezAck();
            ackres.TransactionID = req.TransactionID;
            ackres.Success = true;
            agent.SendMessageAlways(ackres, ID);
        }
    }
}

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

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Object.Localization;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Transfer;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using SilverSim.Viewer.Messages;
using System;
using System.Collections.Generic;
using DeRezAction = SilverSim.Viewer.Messages.Object.DeRezObject.DeRezAction;

namespace SilverSim.Scene.Types.Scene
{
    public abstract partial class SceneInterface
    {
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
            public UGUI RezzingAgent;
        }

        private Vector3 CalculateTargetedRezLocation(
            RayResult ray,
            Vector3 scale,
            Vector3 projectedWaterLocation)
        {
            Vector3 pos = ray.HitNormalWorld.Cross(scale);
            pos *= 0.5;
            pos += ray.HitPointWorld;

            if (projectedWaterLocation.Z > pos.Z)
            {
                pos = projectedWaterLocation;
            }
            return pos;
        }

        public Vector3 CalculateRezLocation(
            RezObjectParams rezparams,
            Vector3 scale)
        {
            Vector3 pos = rezparams.RayEnd + new Vector3(0, 0, scale.Z / 2f);
            if(rezparams.RayEndIsIntersection)
            {
                return rezparams.RayEnd;
            }

            Vector3 projectedWaterLocation = Vector3.Zero;
            double waterHeight = RegionSettings.WaterHeight;
            if(rezparams.RayStart.Z > waterHeight && rezparams.RayEnd.Z < waterHeight)
            {
                Vector3 dir = rezparams.RayEnd - rezparams.RayStart;
                double ratio = (waterHeight - rezparams.RayStart.Z) / dir.Z;

                projectedWaterLocation = rezparams.RayStart;
                projectedWaterLocation.X += ratio * dir.X;
                projectedWaterLocation.Y += ratio * dir.Y;
                projectedWaterLocation.Z = waterHeight;
            }
            else
            {
                projectedWaterLocation.Z = waterHeight;
            }

            ObjectPart target;
            if (Primitives.TryGetValue(rezparams.RayTargetID, out target))
            {
                pos = target.GlobalPosition;
            }
            RayResult[] results = PhysicsScene.RayTest(rezparams.RayStart, pos);

            if (rezparams.RayTargetID != UUID.Zero)
            {
                foreach(RayResult ray in results)
                {
                    if(ray.PartId == rezparams.RayTargetID)
                    {
                        return CalculateTargetedRezLocation(ray, scale, projectedWaterLocation);
                    }
                }
            }
            else
            {
                foreach (RayResult ray in results)
                {
                    if (ray.IsTerrain)
                    {
                        return CalculateTargetedRezLocation(ray, scale, projectedWaterLocation);
                    }
                }
            }

            if(results.Length > 0)
            {
                return CalculateTargetedRezLocation(results[0], scale, projectedWaterLocation);
            }
            else
            {
                pos = rezparams.RayEnd;
                LocationInfo info = GetLocationInfoProvider().At(pos);
                pos.Z = info.GroundHeight;
                if(pos.Z < info.WaterHeight && rezparams.RayStart.Z >= info.WaterHeight)
                {
                    pos.Z = info.WaterHeight;
                }
                pos.Z += scale.Z * 0.5;
            }
            return pos;
        }

        public List<UInt32> RezObjects(List<ObjectGroup> groups, RezObjectParams rezparams)
        {
            var result = new List<uint>();
            if(groups.Count == 0)
            {
                return result;
            }
            Vector3 aabbMin = groups[0].CoalescedRestoreOffset;
            Vector3 aabbMax = aabbMin;
            foreach (ObjectGroup grp in groups)
            {
                aabbMin = aabbMin.ComponentMin(grp.CoalescedRestoreOffset - grp.Size / 2);
                aabbMax = aabbMax.ComponentMax(grp.CoalescedRestoreOffset + grp.Size / 2);
            }
            Vector3 coalescedOffset = (aabbMax - aabbMin) / 2;
#if DEBUG
            m_Log.DebugFormat("RezObject at coalescedbaseoffset={0} aabbmin={1} aabbmax={2}", coalescedOffset, aabbMin, aabbMax);
#endif
            Vector3 basePosition = CalculateRezLocation(rezparams, aabbMax - aabbMin) - coalescedOffset;
            foreach(ObjectGroup grp in groups)
            {
#if DEBUG
                m_Log.DebugFormat("RezObject \"{0}\" at coalescedrestoreoffset={1} size={2}", grp.Name, grp.CoalescedRestoreOffset, grp.Size);
#endif
                try
                {
                    grp.GlobalPosition = basePosition + grp.CoalescedRestoreOffset;
                    result.Add(RezObject(grp, rezparams.RezzingAgent));
                }
                catch(Exception e)
                {
                    m_Log.DebugFormat("Exception at {0}: {1}\n{2}", e.GetType().FullName, e.Message, e.StackTrace);
                    break;
                }
            }
            return result;
        }

        public UInt32 RezObject(ObjectGroup group, RezObjectParams rezparams)
        {
            group.GlobalPosition = CalculateRezLocation(
                rezparams,
                group.Size);
            return RezObject(group, rezparams.RezzingAgent);
        }

        public UInt32 RezObject(ObjectGroup group, UGUI rezzingAgent, int startparameter = 0)
        {
            if (!group.Owner.EqualsGrid(rezzingAgent))
            {
                group.LastOwner = group.Owner;
            }
            foreach (ObjectPart part in group.Values)
            {
                part.Owner = rezzingAgent;
                part.RezDate = Date.Now;
                foreach(ObjectPartInventoryItem item in part.Inventory.ValuesByKey1)
                {
                    if (!item.Owner.EqualsGrid(item.Owner))
                    {
                        item.LastOwner = item.Owner;
                        item.Owner = rezzingAgent;
                    }
                }
            }
            group.Owner = rezzingAgent;
            group.RezzingObjectID = UUID.Zero;
            Add(group);
            RezScriptsForObject(group, startparameter);
            return group.LocalID[ID];
        }

        public abstract void RezScriptsForObject(ObjectGroup group, int startparameter = 0);

        public List<UUID> ReturnObjects(UGUI returningAgent, List<UUID> objectids)
        {
            var returned = new List<UUID>();

            foreach (UUID objectid in objectids)
            {
                ObjectGroup grp;
                if (!ObjectGroups.TryGetValue(objectid, out grp))
                {
                    if (!CanReturn(returningAgent, grp, grp.Position))
                    {
                        continue;
                    }

                    var copyItems = new Dictionary<UGUI, List<InventoryItem>>();
                    UUID assetID;
                    bool changePermissions = false;
                    UGUI targetAgent = grp.Owner;

                    assetID = grp.OriginalAssetID;
                    if (UUID.Zero == assetID)
                    {
                        AssetData asset = grp.Asset(XmlSerializationOptions.WriteOwnerInfo | XmlSerializationOptions.WriteXml2);
                        asset.ID = UUID.Random;
                        AssetService.Store(asset);
                        grp.OriginalAssetID = asset.ID;
                        assetID = asset.ID;
                    }
                    if (!copyItems.ContainsKey(targetAgent))
                    {
                        copyItems.Add(targetAgent, new List<InventoryItem>());
                    }
                    var newitem = new InventoryItem
                    {
                        AssetID = assetID,
                        AssetType = AssetType.Object,
                        InventoryType = InventoryType.Object,
                        Name = grp.Name,
                        Description = grp.Description,
                        LastOwner = grp.Owner,
                        Owner = targetAgent,
                        Creator = grp.RootPart.Creator,
                        CreationDate = grp.RootPart.CreationDate
                    };
                    newitem.Permissions.Base = changePermissions ? grp.RootPart.NextOwnerMask : grp.RootPart.BaseMask;
                    newitem.Permissions.Current = newitem.Permissions.Base;
                    newitem.Permissions.Group = InventoryPermissionsMask.None;
                    newitem.Permissions.NextOwner = grp.RootPart.NextOwnerMask;
                    newitem.Permissions.EveryOne = InventoryPermissionsMask.None;
                    copyItems[grp.Owner].Add(newitem);

                    foreach (KeyValuePair<UGUI, List<InventoryItem>> kvp in copyItems)
                    {
                        var assetIDs = new List<UUID>();
                        IAgent toAgent;
                        foreach (InventoryItem item in kvp.Value)
                        {
                            if (!assetIDs.Contains(item.AssetID))
                            {
                                assetIDs.Add(item.AssetID);
                            }
                        }
                        if (Agents.TryGetValue(kvp.Key.ID, out toAgent))
                        {
                            new ObjectTransferItem(toAgent,
                                this,
                                assetIDs,
                                kvp.Value,
                                AssetType.LostAndFoundFolder).QueueWorkItem();
                        }
                        else
                        {
                            InventoryServiceInterface agentInventoryService;
                            AssetServiceInterface agentAssetService;
                            if (TryGetServices(kvp.Key, out agentInventoryService, out agentAssetService))
                            {
                                new ObjectTransferItem(agentInventoryService,
                                    agentAssetService,
                                    kvp.Key,
                                    this,
                                    assetIDs,
                                    kvp.Value,
                                    AssetType.LostAndFoundFolder).QueueWorkItem();
                            }
                        }
                    }

                    grp.Scene.Remove(grp);
                    returned.Add(objectid);
                }
            }

            return returned;
        }

        [PacketHandler(MessageType.ObjectDuplicate)]
        public void HandleObjectDuplicate(Message m)
        {
            var req = (Viewer.Messages.Object.ObjectDuplicate)m;
            if (req.AgentID != m.CircuitAgentID ||
                req.SessionID != m.CircuitSessionID)
            {
                return;
            }

            IAgent agent;
            var objectgroups = new List<ObjectGroup>();
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            bool isGod = agent.IsInScene(this) && agent.IsActiveGod;

            foreach (UInt32 localid in req.ObjectLocalIDs)
            {
                try
                {
                    ObjectGroup grp = Primitives[localid].ObjectGroup;
                    if (isGod || CanTakeCopy(agent, grp, grp.Position))
                    {
                        objectgroups.Add(grp);
                    }
                }
                catch
                {
                    agent.SendAlertMessage("ALERT: CantFindObject", ID);
                }
            }

            foreach (ObjectGroup grp in objectgroups)
            {
                var newgrp = new ObjectGroup(grp);
                foreach (ObjectPart part in grp.ValuesByKey1)
                {
                    var newpart = new ObjectPart(UUID.Random, part)
                    {
                        RezDate = Date.Now,
                        ObjectGroup = newgrp
                    };
                    newgrp.Add(part.LinkNumber, newpart.ID, newpart);
                    newpart.UpdateData(ObjectPartLocalizedInfo.UpdateDataFlags.All);

                    foreach (KeyValuePair<UUID, ObjectPartInventoryItem> kvp in part.Inventory.Key1ValuePairs)
                    {
                        ScriptInstance instance = kvp.Value.ScriptInstance;
                        var newItem = new ObjectPartInventoryItem(UUID.Random, kvp.Value)
                        {
                            ExperienceID = kvp.Value.ExperienceID
                        };
                        if (instance != null)
                        {
                            try
                            {
                                newItem.ScriptState = instance.ScriptState;
                            }
                            catch
                            {
                                /* if taking script state fails, we do not bail out */
                            }
                        }
                        newpart.Inventory.Add(newItem);
                    }
                    newgrp.GlobalPosition += req.Offset;
                    newpart.IsChangedEnabled = true;
                }

                UGI ugi = UGI.Unknown;
                GroupsNameService?.TryGetValue(req.GroupID, out ugi);
                newgrp.Group = ugi;
                newgrp.Owner = agent.Owner;

                RezObject(newgrp, grp.Owner);
#if DEBUG
                m_Log.DebugFormat("Duplicated object {0} ({1}, {2}) as {3} ({4}, {5})", grp.Name, grp.LocalID, grp.ID, newgrp.Name, newgrp.LocalID, newgrp.ID);
#endif
            }
        }

        [PacketHandler(MessageType.ObjectDelete)]
        public void HandleObjectDelete(Message m)
        {
            var req = (Viewer.Messages.Object.ObjectDelete)m;
            if (req.AgentID != m.CircuitAgentID ||
                req.SessionID != m.CircuitSessionID)
            {
                return;
            }

            IAgent agent;
            var objectgroups = new List<ObjectGroup>();
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            bool isGod = agent.IsInScene(this) && agent.IsActiveGod;

            foreach (UInt32 localid in req.ObjectLocalIDs)
            {
                try
                {
                    ObjectGroup grp = Primitives[localid].ObjectGroup;
                    if ((isGod && req.Force) || CanDelete(agent, grp, grp.Position))
                    {
                        objectgroups.Add(grp);
                    }
                }
                catch
                {
                    agent.SendAlertMessage("ALERT: DeleteFailObjNotFound", ID);
                }
            }

            /* yes, this delete into no-where */
            foreach(ObjectGroup grp in objectgroups)
            {
                Remove(grp);
            }
        }

        [PacketHandler(MessageType.DeRezObject)]
        public void HandleDeRezObject(Message m)
        {
            Viewer.Messages.Object.DeRezAck ackres;
            var req = (Viewer.Messages.Object.DeRezObject)m;
            if (req.AgentID != m.CircuitAgentID ||
                req.SessionID != m.CircuitSessionID)
            {
                return;
            }

            IAgent agent;
            var objectgroups = new List<ObjectGroup>();
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
                        if (isActiveGod && !agent.IsInScene(this))
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
                        if (isActiveGod && !agent.IsInScene(this))
                        {
                            return;
                        }
                        else if (!CanReturn(agent, grp, grp.Position))
                        {
                            agent.SendAlertMessage(string.Format(this.GetLanguageString(agent.CurrentCulture, "NoPermissionToReturnObjectToOwner", "No permission to return object '{0}' to owner"), grp.Name), ID);
                            ackres = new Viewer.Messages.Object.DeRezAck
                            {
                                TransactionID = req.TransactionID,
                                Success = false
                            };
                            agent.SendMessageAlways(ackres, ID);
                            return;
                        }
                    }
                    break;

                case DeRezAction.SaveIntoAgentInventory:
                    ackres = new Viewer.Messages.Object.DeRezAck
                    {
                        TransactionID = req.TransactionID,
                        Success = false
                    };
                    agent.SendMessageAlways(ackres, ID);
                    return;

                case DeRezAction.Take:
                    foreach (ObjectGroup grp in objectgroups)
                    {
                        if (isActiveGod && !agent.IsInScene(this))
                        {
                            return;
                        }
                        else if (!CanTake(agent, grp, grp.Position))
                        {
                            agent.SendAlertMessage(string.Format(this.GetLanguageString(agent.CurrentCulture, "NoPermissionToTakeObject", "No permission to take object '{0}'"), grp.Name), ID);
                            ackres = new Viewer.Messages.Object.DeRezAck
                            {
                                TransactionID = req.TransactionID,
                                Success = false
                            };
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
                            ackres = new Viewer.Messages.Object.DeRezAck
                            {
                                TransactionID = req.TransactionID,
                                Success = false
                            };
                            agent.SendMessageAlways(ackres, ID);
                            return;
                        }
                    }
                    break;

                default:
                    agent.SendAlertMessage(this.GetLanguageString(agent.CurrentCulture, "InvalidDerezRequestByViewer", "Invalid derez request by viewer"), ID);
                    ackres = new Viewer.Messages.Object.DeRezAck
                    {
                        TransactionID = req.TransactionID,
                        Success = false
                    };
                    agent.SendMessageAlways(ackres, ID);
                    return;
            }

            var copyItems = new Dictionary<UGUI, List<InventoryItem>>();
            foreach (ObjectGroup grp in objectgroups)
            {
                UUID assetID;
                bool changePermissions = false;
                UGUI targetAgent;
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
                var item = new InventoryItem
                {
                    AssetID = assetID,
                    AssetType = AssetType.Object,
                    InventoryType = InventoryType.Object,
                    Name = grp.Name,
                    Description = grp.Description,
                    LastOwner = grp.Owner,
                    Owner = targetAgent,
                    Creator = grp.RootPart.Creator,
                    CreationDate = grp.RootPart.CreationDate
                };
                item.Permissions.Base = changePermissions ? grp.RootPart.NextOwnerMask : grp.RootPart.BaseMask;
                item.Permissions.Current = item.Permissions.Base;
                item.Permissions.Group = InventoryPermissionsMask.None;
                item.Permissions.NextOwner = grp.RootPart.NextOwnerMask;
                item.Permissions.EveryOne = InventoryPermissionsMask.None;
                if(!copyItems.ContainsKey(grp.Owner))
                {
                    copyItems.Add(grp.Owner, new List<InventoryItem>());
                }
                copyItems[grp.Owner].Add(item);
            }

            foreach(KeyValuePair<UGUI, List<InventoryItem>> kvp in copyItems)
            {
                var assetIDs = new List<UUID>();
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
                    new ObjectTransferItem(toAgent,
                        this,
                        assetIDs,
                        kvp.Value,
                        req.Destination == DeRezAction.DeleteToTrash ? AssetType.TrashFolder : AssetType.Object).QueueWorkItem();
                }
                else
                {
                    InventoryServiceInterface agentInventoryService;
                    AssetServiceInterface agentAssetService;
                    if (TryGetServices(kvp.Key, out agentInventoryService, out agentAssetService))
                    {
                        new ObjectTransferItem(agentInventoryService,
                            agentAssetService,
                            kvp.Key,
                            this,
                            assetIDs,
                            kvp.Value,
                            req.Destination == DeRezAction.DeleteToTrash ? AssetType.TrashFolder : AssetType.Object).QueueWorkItem();
                    }
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

            ackres = new Viewer.Messages.Object.DeRezAck
            {
                TransactionID = req.TransactionID,
                Success = true
            };
            agent.SendMessageAlways(ackres, ID);
        }
    }
}

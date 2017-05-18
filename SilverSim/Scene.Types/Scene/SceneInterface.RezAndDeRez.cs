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
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Transfer;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using SilverSim.Viewer.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        Vector3 CalculateTargetedRezLocation(
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
                projectedWaterLocation.X += (ratio * dir.X);
                projectedWaterLocation.Y += (ratio * dir.Y);
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
            RayResult[] results = PhysicsScene.ClosestRayTest(rezparams.RayStart, pos);

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
                if(pos.Z < info.WaterHeight)
                {
                    pos.Z = info.WaterHeight;
                }
                pos.Z += scale.Z * 0.5;
            }
            return pos;
        }

        public List<UInt32> RezObjects(List<ObjectGroup> groups, RezObjectParams rezparams)
        {
            List<UInt32> result = new List<uint>();
            foreach(ObjectGroup grp in groups)
            {
                try
                {
                    result.Add(RezObject(grp, rezparams));
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
            group.RezzingObjectID = UUID.Zero;
            Add(group);
            return group.LocalID;
        }

        [PacketHandler(MessageType.DeRezObject)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        internal void HandleDeRezObject(Message m)
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
                            ackres = new Viewer.Messages.Object.DeRezAck()
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
                    ackres = new Viewer.Messages.Object.DeRezAck()
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
                            ackres = new Viewer.Messages.Object.DeRezAck()
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
                            ackres = new Viewer.Messages.Object.DeRezAck()
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
                    ackres = new Viewer.Messages.Object.DeRezAck()
                    {
                        TransactionID = req.TransactionID,
                        Success = false
                    };
                    agent.SendMessageAlways(ackres, ID);
                    return;
            }

            var copyItems = new Dictionary<UUI, List<InventoryItem>>();
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
                    var item = new InventoryItem()
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
                    copyItems[grp.Owner].Add(item);
                }
            }

            foreach(KeyValuePair<UUI, List<InventoryItem>> kvp in copyItems)
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
                    new ObjectTransferItem(agent,
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

            ackres = new Viewer.Messages.Object.DeRezAck()
            {
                TransactionID = req.TransactionID,
                Success = true
            };
            agent.SendMessageAlways(ackres, ID);
        }
    }
}

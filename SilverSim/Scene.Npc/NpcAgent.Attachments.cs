// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Inventory;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Npc
{
    partial class NpcAgent
    {
        #region NPC Appearance
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidLargeStructureRule")]
        struct DetachEntry
        {
            public UUID ItemID;
            public UUID SceneID;
            public UUID ObjectID;

            public DetachEntry(UUID itemID, UUID sceneID, UUID objectID)
            {
                ItemID = itemID;
                SceneID = sceneID;
                ObjectID = objectID;
            }
        }

        public void DetachAllAttachments()
        {
            List<DetachEntry> detachList = new List<DetachEntry>();
            m_AttachmentsList.ForEach(delegate (KeyValuePair<UUID, KeyValuePair<UUID, UUID>> kvp)
            {
                detachList.Add(new DetachEntry(kvp.Key, kvp.Value.Key, kvp.Value.Value));
            });
            foreach (DetachEntry entry in detachList)
            {
                DetachAttachment(entry);
            }
        }

        void DetachAttachment(DetachEntry entry)
        {
            ObjectGroup grp = CurrentScene.ObjectGroups[entry.ObjectID];
            try
            {
                CurrentScene.Remove(grp);
            }
            catch
            {
                m_AttachmentsList.Remove(entry.ItemID);
                return;
            }
            m_AttachmentsList.Remove(entry.ItemID);

            /* only serialize changed and/or scripted attachments */
            bool isChanged = false;
            bool isScripted = false;
            foreach (ObjectPart part in grp.Values)
            {
                isChanged = isChanged || part.IsChanged;
                isScripted = isScripted || part.IsScripted;
                ObjectPart.PrimitiveShape shape = part.Shape;
                shape.State = 0;
                part.Shape = shape;
            }

            if (isChanged || isScripted)
            {
                UUID newAssetID;
                try
                {
                    AssetData data = grp.Asset();
                    newAssetID = data.ID;
                    AssetService.Store(data);
                }
                catch
                {
                    return;
                }
                try
                {
                    InventoryItem item = InventoryService.Item[ID, entry.ItemID];
                    if (item.AssetType != AssetType.Object)
                    {
                        return;
                    }
                    else
                    {
                        item.AssetID = newAssetID;
                        InventoryService.Item.Update(item);
                    }
                }
                catch
                {
                    return;
                }
            }
        }

        public void LoadAppearanceFromNotecard(Notecard nc)
        {
            AppearanceInfo appearance = AppearanceInfo.FromNotecard(nc);

            InventoryService.CheckInventory(ID);
            InventoryService.Folder.Purge(InventoryService.Folder[ID, AssetType.CurrentOutfitFolder].ID);

            UUID bodypartsFolder = InventoryService.Folder[ID, AssetType.Bodypart].ID;
            UUID clothingFolder = InventoryService.Folder[ID, AssetType.Clothing].ID;
            UUID objectFolder = InventoryService.Folder[ID, AssetType.Object].ID;
            List<InventoryItem> attachmentsToRez = new List<InventoryItem>();

            /* generate inventory entries for wearables */
            foreach (KeyValuePair<WearableType, List<AgentWearables.WearableInfo>> kvp in appearance.Wearables.All)
            {
                UUID targetFolder = clothingFolder;
                AssetType assetType = AssetType.Clothing;
                InventoryType invType = InventoryType.Clothing;
                switch (kvp.Key)
                {
                    case WearableType.Shape:
                    case WearableType.Skin:
                    case WearableType.Hair:
                    case WearableType.Eyes:
                    case WearableType.Physics:
                        targetFolder = bodypartsFolder;
                        assetType = AssetType.Bodypart;
                        invType = InventoryType.Bodypart;
                        break;

                    default:
                        break;
                }

                int layer = 0;
                foreach (AgentWearables.WearableInfo wInfo in kvp.Value)
                {
                    InventoryItem item = new InventoryItem();
                    item.AssetID = wInfo.AssetID;
                    item.ID = wInfo.ItemID;
                    item.LastOwner = Owner;
                    item.Owner = Owner;
                    item.Creator = UUI.Unknown;
                    item.InventoryType = invType;
                    item.AssetType = assetType;
                    item.Flags = (InventoryFlags)(uint)kvp.Key;
                    item.Permissions.Base = InventoryPermissionsMask.None;
                    item.Permissions.Current = InventoryPermissionsMask.None;
                    item.Permissions.EveryOne = InventoryPermissionsMask.None;
                    item.Permissions.Group = InventoryPermissionsMask.None;
                    item.Permissions.NextOwner = InventoryPermissionsMask.None;
                    item.Name = wInfo.ItemID.ToString();
                    try
                    {
                        InventoryService.Item.Add(item);
                    }
                    catch
                    {
                        InventoryService.Item.Update(item);
                    }

                    item = new InventoryItem();
                    item.ID = UUID.Random;
                    item.AssetID = item.ID;
                    item.LastOwner = Owner;
                    item.Owner = Owner;
                    item.Creator = Owner;
                    item.InventoryType = invType;
                    item.AssetType = AssetType.Link;
                    item.ParentFolderID = targetFolder;
                    item.Permissions.Base = InventoryPermissionsMask.All;
                    item.Permissions.Current = InventoryPermissionsMask.All;
                    item.Permissions.EveryOne = InventoryPermissionsMask.None;
                    item.Permissions.Group = InventoryPermissionsMask.None;
                    item.Permissions.NextOwner = InventoryPermissionsMask.None;
                    item.Name = wInfo.AssetID.ToString();
                    item.Description = "@" + layer.ToString();
                    InventoryService.Item.Add(item);
                }
            }

            /* generate inventory entries for attachments */
            foreach (KeyValuePair<AttachmentPoint, RwLockedDictionary<UUID, UUID>> kvp in appearance.Attachments)
            {
                foreach (KeyValuePair<UUID, UUID> kvpInner in kvp.Value)
                {
                    InventoryItem item = new InventoryItem();
                    item.AssetID = kvpInner.Value;
                    item.ID = kvpInner.Key;
                    item.LastOwner = Owner;
                    item.Owner = Owner;
                    item.Creator = UUI.Unknown;
                    item.ParentFolderID = objectFolder;
                    item.InventoryType = InventoryType.Object;
                    item.AssetType = AssetType.Object;
                    item.Flags = (InventoryFlags)(uint)kvp.Key;
                    item.Permissions.Base = InventoryPermissionsMask.None;
                    item.Permissions.Current = InventoryPermissionsMask.None;
                    item.Permissions.EveryOne = InventoryPermissionsMask.None;
                    item.Permissions.Group = InventoryPermissionsMask.None;
                    item.Permissions.NextOwner = InventoryPermissionsMask.None;
                    item.Name = kvpInner.Key.ToString();
                    try
                    {
                        InventoryService.Item.Add(item);
                    }
                    catch
                    {
                        InventoryService.Item.Update(item);
                    }
                    attachmentsToRez.Add(item);
                }
            }


            DetachAllAttachments();

            foreach (InventoryItem item in attachmentsToRez)
            {
                AssetData data;
                try
                {
                    data = AssetService[item.AssetID];
                }
                catch (Exception e)
                {
                    m_Log.WarnFormat("Fetch error for object asset {0} for NPC {1} {2} ({3}): {4}: {5}",
                        item.AssetID, Owner.FirstName, Owner.LastName, Owner.ID, e.GetType().FullName, e.ToString());
                    break;
                }

                if (data.Type != AssetType.Object)
                {
                    m_Log.WarnFormat("Wrong asset for object asset {0} for NPC {1} {2} ({3})",
                        item.AssetID, Owner.FirstName, Owner.LastName, Owner.ID);
                    break;
                }

                AttachFromInventory(data, item.ID);
            }

            try
            {
                RebakeAppearance();
            }
            catch
            {
                m_Log.WarnFormat("Failed to rebake NPC {0} {1} ({2})",
                    Owner.FirstName, Owner.LastName, Owner.ID);
            }
        }

        void AttachFromInventory(AssetData data, UUID itemID)
        {
            List<ObjectGroup> objgroups;
#if DEBUG
            m_Log.DebugFormat("Deserializing object asset {0} for agent {1} {2} ({3})", data.ID, Owner.FirstName, Owner.LastName, Owner.ID);
#endif
            try
            {
                objgroups = ObjectXML.FromAsset(data, Owner);
            }
            catch (Exception e)
            {
                m_Log.WarnFormat("Deserialization error for object asset {0} for agent {1} {2} ({3}): {4}: {5}",
                    data.ID, Owner.FirstName, Owner.LastName, Owner.ID, e.GetType().FullName, e.ToString());
                return;
            }

            if (objgroups.Count != 1)
            {
                return;
            }

            ObjectGroup grp = objgroups[0];

            foreach (ObjectPart part in grp.Values)
            {
                if (part.Shape.PCode == PrimitiveCode.Grass ||
                    part.Shape.PCode == PrimitiveCode.Tree ||
                    part.Shape.PCode == PrimitiveCode.NewTree)
                {
                    return;
                }
                UUID oldID = part.ID;
                part.ID = UUID.Random;
                grp.ChangeKey(part.ID, oldID);
            }

            AttachmentPoint attachAt;
            attachAt = grp.AttachPoint;

            if (attachAt == AttachmentPoint.NotAttached)
            {
                grp.AttachPoint = AttachmentPoint.LeftHand;
                grp.AttachedPos = Vector3.Zero;
            }

            grp.FromItemID = itemID;
            grp.IsAttached = true;
            grp.Position = grp.AttachedPos;
            grp.IsChangedEnabled = true;

#if DEBUG
            m_Log.DebugFormat("Adding attachment asset {0} at {4} for agent {1} {2} ({3})", data.ID, Owner.FirstName, Owner.LastName, Owner.ID, grp.AttachPoint.ToString());
#endif
            SceneInterface scene = CurrentScene;
            try
            {
                scene.Add(grp);
                m_AttachmentsList.Add(itemID, grp.LocalID, new KeyValuePair<UUID, UUID>(scene.ID, grp.ID));
            }
            catch
            {
                return;
            }
        }
        #endregion
    }
}

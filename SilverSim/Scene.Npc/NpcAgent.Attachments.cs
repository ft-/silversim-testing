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
            var detachList = new List<DetachEntry>();
            m_AttachmentsList.ForEach((KeyValuePair<UUID, KeyValuePair<UUID, UUID>> kvp) => 
                detachList.Add(new DetachEntry(kvp.Key, kvp.Value.Key, kvp.Value.Value)));
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
            var attachmentsToRez = new List<InventoryItem>();

            /* generate inventory entries for wearables */
            foreach (KeyValuePair<WearableType, List<AgentWearables.WearableInfo>> kvp in appearance.Wearables.All)
            {
                UUID targetFolder = clothingFolder;
                var assetType = AssetType.Clothing;
                var invType = InventoryType.Clothing;
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
                    var item = new InventoryItem()
                    {
                        AssetID = wInfo.AssetID,
                        ID = wInfo.ItemID,
                        LastOwner = Owner,
                        Owner = Owner,
                        Creator = UUI.Unknown,
                        InventoryType = invType,
                        AssetType = assetType,
                        Flags = (InventoryFlags)(uint)kvp.Key,
                        Name = wInfo.ItemID.ToString()
                    };
                    item.Permissions.Base = InventoryPermissionsMask.None;
                    item.Permissions.Current = InventoryPermissionsMask.None;
                    item.Permissions.EveryOne = InventoryPermissionsMask.None;
                    item.Permissions.Group = InventoryPermissionsMask.None;
                    item.Permissions.NextOwner = InventoryPermissionsMask.None;
                    try
                    {
                        InventoryService.Item.Add(item);
                    }
                    catch
                    {
                        InventoryService.Item.Update(item);
                    }

                    item = new InventoryItem()
                    {
                        ID = UUID.Random,
                        LastOwner = Owner,
                        Owner = Owner,
                        Creator = Owner,
                        InventoryType = invType,
                        AssetType = AssetType.Link,
                        ParentFolderID = targetFolder,
                        Name = wInfo.AssetID.ToString(),
                        Description = "@" + layer.ToString()
                    };
                    item.AssetID = item.ID;
                    item.Permissions.Base = InventoryPermissionsMask.All;
                    item.Permissions.Current = InventoryPermissionsMask.All;
                    item.Permissions.EveryOne = InventoryPermissionsMask.None;
                    item.Permissions.Group = InventoryPermissionsMask.None;
                    item.Permissions.NextOwner = InventoryPermissionsMask.None;
                    InventoryService.Item.Add(item);
                }
            }

            /* generate inventory entries for attachments */
            foreach (KeyValuePair<AttachmentPoint, RwLockedDictionary<UUID, UUID>> kvp in appearance.Attachments)
            {
                foreach (KeyValuePair<UUID, UUID> kvpInner in kvp.Value)
                {
                    var item = new InventoryItem()
                    {
                        AssetID = kvpInner.Value,
                        ID = kvpInner.Key,
                        LastOwner = Owner,
                        Owner = Owner,
                        Creator = UUI.Unknown,
                        ParentFolderID = objectFolder,
                        InventoryType = InventoryType.Object,
                        AssetType = AssetType.Object,
                        Flags = (InventoryFlags)(uint)kvp.Key,
                        Name = kvpInner.Key.ToString()
                    };
                    item.Permissions.Base = InventoryPermissionsMask.None;
                    item.Permissions.Current = InventoryPermissionsMask.None;
                    item.Permissions.EveryOne = InventoryPermissionsMask.None;
                    item.Permissions.Group = InventoryPermissionsMask.None;
                    item.Permissions.NextOwner = InventoryPermissionsMask.None;
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

            AttachmentPoint attachAt = grp.AttachPoint;

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

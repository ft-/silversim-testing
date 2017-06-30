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

#pragma warning disable IDE0018
#pragma warning disable RCS1029

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Transfer;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using SilverSim.Viewer.Messages;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        private readonly RwLockedDoubleDictionary<UUID /* ItemID */, UInt32 /* LocalID */, KeyValuePair<UUID /* SceneID */, UUID /* ObjectID */>> m_AttachmentsList = new RwLockedDoubleDictionary<UUID,UInt32,KeyValuePair<UUID, UUID>>();

        private struct DetachEntry
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

        [PacketHandler(MessageType.RezMultipleAttachmentsFromInv)]
        [PacketHandler(MessageType.RezSingleAttachmentFromInv)]
        public void HandleRezAttachment(Message m)
        {
            switch(m.Number)
            {
                case MessageType.RezMultipleAttachmentsFromInv:
                    {
                        var req = (Messages.Object.RezMultipleAttachmentsFromInv)m;
                        if(req.SessionID != SessionID || req.AgentID != ID)
                        {
                            return;
                        }
#if DEBUG
                        m_Log.DebugFormat("RezMultipleAttachmentsFromInv for agent {0}", Owner.FullName);
#endif
                        if (req.FirstDetachAll)
                        {
                            /* get rid of all current attachments */
#if DEBUG
                            m_Log.DebugFormat("Detach previous attachments of agent {0}", Owner.FullName);
#endif
                            DetachAllAttachments();
                        }

                        foreach (var d in req.ObjectData)
                        {
                            RezAttachment(d.ItemID, d.AttachmentPoint);
                        }
                    }
                    break;

                case MessageType.RezSingleAttachmentFromInv:
                    {
                        var req = (Messages.Object.RezSingleAttachmentFromInv)m;
                        if (req.SessionID != SessionID || req.AgentID != ID)
                        {
                            return;
                        }
#if DEBUG
                        m_Log.DebugFormat("RezSingleAttachmentFromInv for agent {0}", Owner.FullName);
#endif
                        RezAttachment(req.ItemID, req.AttachmentPoint);
                    }
                    break;

                default:
                    break;
            }
        }

        [PacketHandler(MessageType.DetachAttachmentIntoInv)]
        [PacketHandler(MessageType.ObjectDetach)]
        public void HandleDetachAttachment(Message m)
        {
            var detachList = new List<DetachEntry>();
            if(m.Number == MessageType.ObjectDetach)
            {
#if DEBUG
                m_Log.DebugFormat("ObjectDetach: Detach attachment to inv received for {0}", Owner.FullName);
#endif
                var req = (Messages.Object.ObjectDetach)m;
                if (req.SessionID != SessionID || req.AgentID != ID)
                {
                    return;
                }

                foreach (var localid in req.ObjectList)
                {
                    KeyValuePair<UUID, KeyValuePair<UUID,UUID>> kvp;
                    if(m_AttachmentsList.TryGetValue(localid, out kvp))
                    {
                        detachList.Add(new DetachEntry(kvp.Key, kvp.Value.Key, kvp.Value.Value));
                    }
                }
            }
            else if(m.Number == MessageType.DetachAttachmentIntoInv)
            {
#if DEBUG
                m_Log.DebugFormat("DetachAttachmentIntoInv: Detach attachment to inv received for {0}", Owner.FullName);
#endif
                var req = (Messages.Object.DetachAttachmentIntoInv)m;
                if (req.AgentID != ID)
                {
                    return;
                }

#if DEBUG
                m_Log.DebugFormat("Detach attachment {1} into inventory of {0}", Owner.FullName, req.ItemID);
#endif

                KeyValuePair<UInt32, KeyValuePair<UUID,UUID>> kvp;
                if(m_AttachmentsList.TryGetValue(req.ItemID, out kvp))
                {
                    detachList.Add(new DetachEntry(req.ItemID, kvp.Value.Key, kvp.Value.Value));
                }
            }

            foreach(DetachEntry entry in detachList)
            {
                DetachAttachment(entry);
            }
        }

        #region Actual attachment handling
        private void DetachAllAttachments()
        {
            var detachList = new List<DetachEntry>();
            foreach (KeyValuePair<UUID, KeyValuePair<UUID, UUID>> kvp in m_AttachmentsList.Key1ValuePairs)
            {
                detachList.Add(new DetachEntry(kvp.Key, kvp.Value.Key, kvp.Value.Value));
            }
            foreach (var entry in detachList)
            {
                DetachAttachment(entry);
            }
        }

        public class RezAttachmentHandler : AssetTransferWorkItem
        {
            private readonly SceneInterface m_Scene;
            private readonly UUID m_ItemID;
            private readonly UUI m_RezzingAgent;
            private readonly AttachmentPoint m_AttachPoint;
            private readonly RwLockedDoubleDictionary<UUID /* ItemID */, UInt32 /* LocalID */, KeyValuePair<UUID /* SceneID */, UUID /* ObjectID */>> m_AttachmentsList = new RwLockedDoubleDictionary<UUID, UInt32, KeyValuePair<UUID, UUID>>();

            internal RezAttachmentHandler(
                SceneInterface scene,
                UUID itemid,
                UUID assetid,
                AssetServiceInterface source,
                UUI rezzingagent,
                AttachmentPoint attachPoint,
                RwLockedDoubleDictionary<UUID /* ItemID */, UInt32 /* LocalID */, KeyValuePair<UUID /* SceneID */, UUID /* ObjectID */>> attachmentsList)
                : base(scene.AssetService, source, assetid, ReferenceSource.Destination)
            {
                m_Scene = scene;
                m_RezzingAgent = rezzingagent;
                m_ItemID = itemid;
                m_AttachPoint = attachPoint;
                m_AttachmentsList = attachmentsList;
            }

            private void SendAlertMessage(string msg)
            {
                IAgent agent;
                if(m_Scene.Agents.TryGetValue(m_RezzingAgent.ID, out agent))
                {
                    agent.SendAlertMessage(msg, m_Scene.ID);
                }
            }

            public override void AssetTransferComplete()
            {
                AssetData data;
                List<ObjectGroup> objgroups;
                try
                {
                    data = m_Scene.AssetService[AssetID];
                }
                catch
                {
                    SendAlertMessage("ALERT: CantFindObject");
                    return;
                }

#if DEBUG
                m_Log.DebugFormat("Deserializing object asset {0} for agent {1} {2} ({3})", data.ID, m_RezzingAgent.FirstName, m_RezzingAgent.LastName, m_RezzingAgent.ID);
#endif
                try
                {
                    objgroups = ObjectXML.FromAsset(data, m_RezzingAgent);
                }
                catch(Exception e)
                {
                    m_Log.WarnFormat("Deserialization error for object asset {0} for agent {1} {2} ({3}): {4}: {5}",
                        data.ID, m_RezzingAgent.FirstName, m_RezzingAgent.LastName, m_RezzingAgent.ID, e.GetType().FullName, e.ToString());
                    SendAlertMessage("ALERT: InvalidObjectParams");
                    return;
                }

                if (objgroups.Count != 1)
                {
                    SendAlertMessage("ALERT: InvalidObjectParams");
                    return;
                }

                ObjectGroup grp = objgroups[0];

                foreach (var part in grp.Values)
                {
                    if (part.Shape.PCode == Types.Primitive.PrimitiveCode.Grass ||
                        part.Shape.PCode == Types.Primitive.PrimitiveCode.Tree ||
                        part.Shape.PCode == Types.Primitive.PrimitiveCode.NewTree)
                    {
                        SendAlertMessage("ALERT: WhyAreYouTryingToWearShrubbery");
                        return;
                    }
                }

                var attachAt = m_AttachPoint & AttachmentPoint.PositionMask;
                if (attachAt != AttachmentPoint.Default && attachAt != grp.AttachPoint)
                {
                    grp.AttachedPos = Vector3.Zero;
                }

                if (attachAt == AttachmentPoint.Default)
                {
                    attachAt = grp.AttachPoint;

                    if (attachAt == AttachmentPoint.NotAttached)
                    {
                        grp.AttachPoint = AttachmentPoint.LeftHand;
                        grp.AttachedPos = Vector3.Zero;
                    }
                }

                grp.FromItemID = m_ItemID;
                grp.IsAttached = true;
                grp.Position = grp.AttachedPos;
                grp.IsChangedEnabled = true;

#if DEBUG
                m_Log.DebugFormat("Adding attachment asset {0} at {4} for agent {1} {2} ({3})", data.ID, m_RezzingAgent.FirstName, m_RezzingAgent.LastName, m_RezzingAgent.ID, grp.AttachPoint.ToString());
#endif
                try
                {
                    m_Scene.Add(grp);
                    m_AttachmentsList.Add(m_ItemID, grp.LocalID, new KeyValuePair<UUID, UUID>(m_Scene.ID, grp.ID));
                }
                catch
                {
                    SendAlertMessage("ALERT: RezAttemptFailed");
                    return;
                }
            }

            public override void AssetTransferFailed(Exception e)
            {
                IAgent agent;
                if(m_Scene.Agents.TryGetValue(m_RezzingAgent.ID, out agent))
                {
                    agent.SendAlertMessage("ALERT: CantFindObject", m_Scene.ID);
                }
            }
        }

        private void RezAttachment(UUID itemID, AttachmentPoint attachpointFlagged)
        {
            InventoryItem item;
            try
            {
                item = InventoryService.Item[ID, itemID];
            }
            catch
            {
                SendAlertMessage("ALERT: CantFindInvItem", SceneID);
                return;
            }
            if(item.AssetType != AssetType.Object)
            {
                SendAlertMessage("ALERT: InvalidObjectParams", SceneID);
                return;
            }
#if DEBUG
            m_Log.DebugFormat("Attaching item {0} / asset {1} to agent {2}", item.ID, item.AssetID, Owner.FullName);
#endif

            var accessFailed = false;
            try
            {
                accessFailed = !AssetService.Exists(item.AssetID);
            }
            catch (Exception e)
            {
                m_Log.WarnFormat("Attaching item {0} / asset {1} not possible due {2}: {3}", item.ID, item.AssetID, e.GetType().FullName, e.ToString());
                SendAlertMessage("ALERT: CantFindInvItem", SceneID);
                return;
            }

            if (accessFailed)
            {
                m_Log.WarnFormat("Attaching item {0} / asset {1} not possible since it is missing", item.ID, item.AssetID);
                SendAlertMessage("ALERT: CantFindInvItem", SceneID);
                return;
            }

            new RezAttachmentHandler(
                Circuits[SceneID].Scene,
                itemID,
                item.AssetID,
                AssetService,
                Owner,
                attachpointFlagged,
                m_AttachmentsList).QueueWorkItem();
        }

        private void DetachAttachment(DetachEntry entry)
        {
            var grp = Circuits[entry.SceneID].Scene.ObjectGroups[entry.ObjectID];
            try
            {
                Circuits[entry.SceneID].Scene.Remove(grp);
            }
            catch
            {
                m_AttachmentsList.Remove(entry.ItemID);
                return;
            }
            m_AttachmentsList.Remove(entry.ItemID);

            /* only serialize changed and/or scripted attachments */
            var isChanged = false;
            var isScripted = false;
            foreach(var part in grp.Values)
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
                    var data = grp.Asset();
                    newAssetID = data.ID;
                    AssetService.Store(data);
                }
                catch
                {
                    SendAlertMessage(this.GetLanguageString(CurrentCulture, "CouldNotStoreAttachmentData", "Could not store attachment data"), SceneID);
                    return;
                }
                try
                {
                    var item = InventoryService.Item[ID, entry.ItemID];
                    if (item.AssetType != AssetType.Object)
                    {
                        SendAlertMessage(this.GetLanguageString(CurrentCulture, "CouldNotStoreAttachmentData", "Could not store attachment data"), SceneID);
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
                    SendAlertMessage(this.GetLanguageString(CurrentCulture, "CouldNotStoreAttachmentDataWithinItem", "Could not store attachment data within item"), SceneID);
                    return;
                }
            }
        }
        #endregion
    }
}

/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.LL.Messages;
using SilverSim.Main.Common.Transfer;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;
using System.Threading;
using ThreadedClasses;

namespace SilverSim.LL.Core
{
    public partial class LLAgent
    {
        readonly RwLockedDoubleDictionary<UUID /* ItemID */, UInt32 /* LocalID */, KeyValuePair<UUID /* SceneID */, UUID /* ObjectID */>> m_AttachmentsList = new RwLockedDoubleDictionary<UUID,UInt32,KeyValuePair<UUID, UUID>>();

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

        void HandleRezAttachment(Message m)
        {
            switch(m.Number)
            {
                case MessageType.RezMultipleAttachmentsFromInv:
                    {
                        SilverSim.LL.Messages.Object.RezMultipleAttachmentsFromInv req = (SilverSim.LL.Messages.Object.RezMultipleAttachmentsFromInv)m;
                        if(req.SessionID != SessionID || req.AgentID != ID)
                        {
                            return;
                        }
                        if(req.FirstDetachAll)
                        {
                            /* get rid of all current attachments */
                            DetachAllAttachments();
                        }

                        foreach (SilverSim.LL.Messages.Object.RezMultipleAttachmentsFromInv.ObjectDataS d in req.ObjectData)
                        {
                            RezAttachment(d.ItemID, d.AttachmentPoint);
                        }
                    }
                    break;

                case MessageType.RezSingleAttachmentFromInv:
                    {
                        SilverSim.LL.Messages.Object.RezSingleAttachmentFromInv req = (SilverSim.LL.Messages.Object.RezSingleAttachmentFromInv)m;
                        if (req.SessionID != SessionID || req.AgentID != ID)
                        {
                            return;
                        }
                        RezAttachment(req.ItemID, req.AttachmentPoint);
                    }
                    break;
            }
        }

        void HandleDetachAttachment(Message m)
        {
            List<DetachEntry> detachList = new List<DetachEntry>();
            if(m.Number == MessageType.ObjectDetach)
            {
                SilverSim.LL.Messages.Object.ObjectDetach req = (SilverSim.LL.Messages.Object.ObjectDetach)m;
                if (req.SessionID != SessionID || req.AgentID != ID)
                {
                    return;
                }

                foreach(UInt32 localid in req.ObjectList)
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
                SilverSim.LL.Messages.Object.DetachAttachmentIntoInv req = (SilverSim.LL.Messages.Object.DetachAttachmentIntoInv)m;
                if (req.AgentID != ID)
                {
                    return;
                }

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
        void DetachAllAttachments()
        {
            List<DetachEntry> detachList = new List<DetachEntry>();
            m_AttachmentsList.ForEach(delegate(KeyValuePair<UUID, KeyValuePair<UUID, UUID>> kvp)
            {
                detachList.Add(new DetachEntry(kvp.Key, kvp.Value.Key, kvp.Value.Value));
            });
            foreach (DetachEntry entry in detachList)
            {
                DetachAttachment(entry);
            }
        }

        class RezAttachmentHandler : AssetTransferWorkItem
        {
            SceneInterface m_Scene;
            UUID m_ItemID;
            UUI m_RezzingAgent;
            AttachmentPoint m_AttachPoint;

            public RezAttachmentHandler(SceneInterface scene, UUID itemid, UUID assetid, AssetServiceInterface source, UUI rezzingagent, AttachmentPoint attachPoint)
                : base(scene.AssetService, source, assetid, ReferenceSource.Destination)
            {
                m_Scene = scene;
                m_RezzingAgent = rezzingagent;
                m_ItemID = itemid;
                m_AttachPoint = attachPoint;
            }

            protected void SendAlertMessage(string msg)
            {
                try
                {
                    IAgent agent = m_Scene.Agents[m_RezzingAgent.ID];
                    agent.SendAlertMessage(msg, m_Scene.ID);
                }
                catch
                {

                }

            }

            public override void AssetTransferComplete()
            {
                AssetData data;
                List<ObjectGroup> objgroups;
                try
                {
                    data = m_Scene.AssetService[m_AssetID];
                }
                catch
                {
                    SendAlertMessage("ALERT: CantFindObject");
                    return;
                }

                try
                {
                    objgroups = ObjectXML.fromAsset(data, m_RezzingAgent);
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

                foreach (ObjectPart part in grp.Values)
                {
                    if (part.Shape.PCode == Types.Primitive.PrimitiveCode.Grass ||
                        part.Shape.PCode == Types.Primitive.PrimitiveCode.Tree ||
                        part.Shape.PCode == Types.Primitive.PrimitiveCode.NewTree)
                    {
                        SendAlertMessage("ALERT: WhyAreYouTryingToWearShrubbery");
                        return;
                    }
                    UUID oldID = part.ID;
                    part.ID = UUID.Random;
                    grp.ChangeKey(part.ID, oldID);
                }

                AttachmentPoint attachAt = m_AttachPoint & AttachmentPoint.PositionMask;
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

                try
                {
                    m_Scene.Add(grp);
                }
                catch
                {
                    SendAlertMessage("ALERT: RezAttemptFailed");
                    return;
                }
            }

            public override void AssetTransferFailed(Exception e)
            {
                try
                {
                    IAgent agent = m_Scene.Agents[m_RezzingAgent.ID];
                    agent.SendAlertMessage("ALERT: CantFindObject", m_Scene.ID);
                }
                catch
                {

                }
            }
        }

        void RezAttachment(UUID itemID, AttachmentPoint attachpointFlagged)
        {
            InventoryItem item;
            try
            {
                item = InventoryService.Item[ID, itemID];
            }
            catch
            {
                SendAlertMessage("ALERT: CantFindInvItem", m_CurrentSceneID);
                return;
            }
            if(item.AssetType != AssetType.Object)
            {
                SendAlertMessage("ALERT: InvalidObjectParams", m_CurrentSceneID);
                return;
            }
            AssetData data;
            try
            {
                data = AssetService[item.AssetID];
            }
            catch(Exception e)
            {
                m_Log.WarnFormat("Attaching item {0} / asset {1} not possible due {2}: {3}", item.ID, item.AssetID, e.GetType().FullName, e.ToString());
                SendAlertMessage("ALERT: CantFindInvItem", m_CurrentSceneID);
                return;
            }

            RezAttachmentHandler rezAttachHandler = new RezAttachmentHandler(Circuits[m_CurrentSceneID].Scene, itemID, item.AssetID, AssetService, Owner, attachpointFlagged);
            ThreadPool.UnsafeQueueUserWorkItem(HandleAssetTransferWorkItem, rezAttachHandler);
        }

        void DetachAttachment(DetachEntry entry)
        {
            ObjectGroup grp = Circuits[entry.SceneID].Scene.ObjectGroups[entry.ObjectID];
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
            bool isChanged = false;
            bool isScripted = false;
            foreach(ObjectPart part in grp.Values)
            {
                isChanged = isChanged || part.IsChanged;
                isScripted = isScripted || part.Inventory.CountScripts != 0;
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
                    SendAlertMessage("Could not store attachment data", m_CurrentSceneID);
                    return;
                }
                try
                {
                    InventoryItem item = InventoryService.Item[ID, entry.ItemID];
                    if (item.AssetType != AssetType.Object)
                    {
                        SendAlertMessage("Could not store attachment data", m_CurrentSceneID);
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
                    SendAlertMessage("Could not store attachment data within item", m_CurrentSceneID);
                    return;
                }
            }
        }
        #endregion
    }
}

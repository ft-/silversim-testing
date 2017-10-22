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

using SilverSim.Scene.Types.Object;
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
                    ObjectGroup grp;
                    if(Attachments.TryGetValueByLocalID(localid, out grp))
                    {
                        detachList.Add(new DetachEntry(grp.FromItemID, req.CircuitSceneID, grp.ID));
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

                ObjectGroup grp;
                if(Attachments.TryGetValueByInventoryID(req.ItemID, out grp))
                {
                    detachList.Add(new DetachEntry(req.ItemID, m.CircuitSceneID.AsUUID, grp.ID));
                }
            }

            foreach(DetachEntry entry in detachList)
            {
                DetachAttachment(entry);
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
            if (item.AssetType != AssetType.Object)
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
                Attachments).QueueWorkItem();
        }

        protected override void DetachAttachment(DetachEntry entry)
        {
            var grp = Circuits[entry.SceneID].Scene.ObjectGroups[entry.ObjectID];
            try
            {
                Circuits[entry.SceneID].Scene.Remove(grp);
            }
            catch
            {
                return;
            }
            Attachments.Remove(grp.ID);

            /* only serialize changed and/or scripted attachments */
            var isChanged = false;
            var isScripted = false;
            foreach (var part in grp.Values)
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
    }
}

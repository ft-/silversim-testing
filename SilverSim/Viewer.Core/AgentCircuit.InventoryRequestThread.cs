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
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Inventory;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Alert;
using SilverSim.Viewer.Messages.Gestures;
using SilverSim.Viewer.Messages.Inventory;
using SilverSim.Viewer.Messages.Transfer;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
    {
        #region Fetch Inventory Thread
        private const int MAX_FOLDERS_PER_PACKET = 6;
        private const int MAX_ITEMS_PER_PACKET = 5;

        private void SendAssetNotFound(TransferRequest req)
        {
            var res = new TransferInfo()
            {
                ChannelType = 2,
                Status = -2,
                TargetType = (int)req.SourceType,
                Params = req.Params,
                Size = 0,
                TransferID = req.TransferID
            };
            SendMessage(res);
        }

        private void SendAssetInsufficientPermissions(Messages.Transfer.TransferRequest req)
        {
            var res = new TransferInfo()
            {
                ChannelType = 2,
                Status = -5,
                TargetType = (int)req.SourceType,
                Params = req.Params,
                Size = 0,
                TransferID = req.TransferID
            };
            SendMessage(res);
        }

        private void FetchInventoryThread(object param)
        {
            Thread.CurrentThread.Name = string.Format("LLUDP:Inventory Fetch for CircuitCode {0} / IP {1}", CircuitCode, RemoteEndPoint.ToString());

            while (true)
            {
                Message m;
                if (!m_InventoryThreadRunning)
                {
                    return;
                }
                try
                {
                    m = m_InventoryRequestQueue.Dequeue(1000);
                }
                catch
                {
                    continue;
                }

                try
                {
                    switch (m.Number)
                    {
                        case MessageType.ChangeInventoryItemFlags:
                            FetchInventoryThread_ChangeInventoryItemFlags(m);
                            break;

                        case MessageType.TransferRequest:
                            FetchInventoryThread_TransferRequest(m);
                            break;

                        case MessageType.CopyInventoryItem:
                            FetchInventoryThread_CopyInventoryItem(m);
                            break;

                        case MessageType.CreateInventoryItem:
                            FetchInventoryThread_CreateInventoryItem((CreateInventoryItem)m);
                            break;

                        case MessageType.CreateInventoryFolder:
                            FetchInventoryThread_CreateInventoryFolder(m);
                            break;

                        case MessageType.FetchInventory:
                            FetchInventoryThread_FetchInventory(m);
                            break;

                        case MessageType.FetchInventoryDescendents:
                            FetchInventoryThread_FetchInventoryDescendents(m);
                            break;

                        case MessageType.LinkInventoryItem:
                            FetchInventoryThread_LinkInventoryItem(m);
                            break;

                        case MessageType.MoveInventoryFolder:
                            FetchInventoryThread_MoveInventoryFolder(m);
                            break;

                        case MessageType.MoveInventoryItem:
                            FetchInventoryThread_MoveInventoryItem(m);
                            break;

                        case MessageType.PurgeInventoryDescendents:
                            FetchInventoryThread_PurgeInventoryDescendents(m);
                            break;

                        case MessageType.RemoveInventoryFolder:
                            FetchInventoryThread_RemoveInventoryFolder(m);
                            break;

                        case MessageType.RemoveInventoryItem:
                            FetchInventoryThread_RemoveInventoryItem(m);
                            break;

                        case MessageType.RemoveInventoryObjects:
                            FetchInventoryThread_RemoveInventoryObjects(m);
                            break;

                        case MessageType.UpdateInventoryFolder:
                            FetchInventoryThread_UpdateInventoryFolder(m);
                            break;

                        case MessageType.UpdateInventoryItem:
                            FetchInventoryThread_UpdateInventoryItem(m);
                            break;

                        case MessageType.ActivateGestures:
                            FetchInventoryThread_ActivateGestures(m);
                            break;

                        case MessageType.DeactivateGestures:
                            FetchInventoryThread_DeactivateGestures(m);
                            break;

                        default:
                            break;
                    }
                }
                catch(Exception e)
                {
                    m_Log.ErrorFormat("Encountered exception in inventory handling: {0}: {1}\n{2}", e.GetType().FullName, e.Message, e.StackTrace);
                }
            }
        }

        private void FetchInventoryThread_ChangeInventoryItemFlags(Message m)
        {
            var req = (ChangeInventoryItemFlags)m;
            if (req.SessionID != SessionID || req.AgentID != AgentID)
            {
                return;
            }

            foreach (var d in req.InventoryData)
            {
                InventoryItem item;
                try
                {
                    item = Agent.InventoryService.Item[AgentID, d.ItemID];
                    item.Flags = d.Flags;
                    Agent.InventoryService.Item.Update(item);
                }
                catch
                {
                    /* no useful action possible */
                }
            }
        }

        private void FetchInventoryThread_TransferRequest(Message m)
        {
            UUID assetID;
            bool denySpecificAssetsViaDirectAssetId = false;
            var req = (TransferRequest)m;
            if (req.SourceType == SourceType.SimInventoryItem)
            {
                var taskID = new UUID(req.Params, 48);
                var itemID = new UUID(req.Params, 64);
                assetID = new UUID(req.Params, 80);
                if (taskID == UUID.Zero)
                {
                    InventoryItem item;
                    try
                    {
                        item = Agent.InventoryService.Item[AgentID, itemID];
                    }
                    catch (Exception e)
                    {
                        m_Log.DebugFormat("Failed to request inventory asset (TransferRequest) for Agent {0}: {1}", AgentID, e.Message);
                        SendAssetNotFound(req);
                        return;
                    }

                    if (item.AssetType == AssetType.LSLText)
                    {
                        if (0 == ((item.Permissions.Current | item.Permissions.EveryOne) & InventoryPermissionsMask.Modify))
                        {
                            SendAssetInsufficientPermissions(req);
                            return;
                        }
                    }
                    else if(item.AssetType == AssetType.Object)
                    {
                        SendAssetNotFound(req);
                        return;
                    }
                    else if (item.AssetID != assetID)
                    {
                        m_Log.DebugFormat("Failed to request inventory asset (TransferRequest) for Agent {0}: Provided AssetID != Item AssetID", AgentID);
                        SendAssetNotFound(req);
                        return;
                    }
                }
                else
                {
                    ObjectPart part;
                    ObjectPartInventoryItem item;
                    if(!Scene.Primitives.TryGetValue(taskID, out part) ||
                        !part.Inventory.TryGetValue(itemID, out item))
                    {
                        SendAssetNotFound(req);
                        return;
                    }

                    switch(item.AssetType)
                    {
                        case AssetType.LSLText:
                            if(!item.CheckPermissions(Agent.Owner, Agent.Group, InventoryPermissionsMask.Modify))
                            {
                                SendAssetInsufficientPermissions(req);
                                return;
                            }
                            break;

                        case AssetType.Object:
                            SendAssetInsufficientPermissions(req);
                            return;

                        default:
                            if (item.AssetID != assetID)
                            {
                                m_Log.DebugFormat("Failed to request sim inventory asset (TransferRequest) for Agent {0}: Provided AssetID != Item AssetID", AgentID);
                                SendAssetNotFound(req);
                                return;
                            }
                            break;
                    }
                }
            }
            else if (req.SourceType == SourceType.Asset)
            {
                assetID = new UUID(req.Params, 0);
                denySpecificAssetsViaDirectAssetId = true;
            }
            else
            {
                m_Log.DebugFormat("Failed to request (TransferRequest) for Agent {0}: Provided AssetID != Item AssetID", AgentID);
                SendAssetNotFound(req);
                return;
            }

            /* let us prefer the scene's asset service */
            AssetData asset;
            try
            {
                asset = Scene.AssetService[assetID];
            }
            catch (Exception e1)
            {
                /* let's try the user's asset server */
                try
                {
                    asset = Agent.AssetService[assetID];
                    try
                    {
                        /* let us try to store the asset locally */
                        Scene.AssetService.Store(asset);
                    }
                    catch (Exception e3)
                    {
                        m_Log.DebugFormat("Failed to store asset {0} locally (TransferPacket): {1}", assetID, e3.Message);
                    }
                }
                catch (Exception e2)
                {
                    if (Server.LogAssetFailures)
                    {
                        m_Log.DebugFormat("Failed to download asset {0} (TransferPacket): {1} or {2}", assetID, e1.Message, e2.Message);
                    }
                    SendAssetNotFound(req);
                    return;
                }
            }

            if(asset == null)
            {
                /* safe guard here */
                SendAssetNotFound(req);
                return;
            }

            if (Server.LogTransferPacket)
            {
                m_Log.DebugFormat("Starting to download asset {0} (TransferPacket)", assetID);
            }
            if (denySpecificAssetsViaDirectAssetId)
            {
                switch(asset.Type)
                {
                    case AssetType.LSLText:
                    case AssetType.Notecard:
                    case AssetType.Object:
                        m_Log.DebugFormat("Failed to request (TransferRequest) for Agent {0}: Insufficient permissions for {1}", AgentID, asset.Type.ToString());
                        SendAssetInsufficientPermissions(req);
                        return;
                }
            }

            var ti = new TransferInfo()
            {
                Params = req.Params,
                ChannelType = 2,
                Status = 0,
                TargetType = 0,
                TransferID = req.TransferID,
                Size = asset.Data.Length
            };
            if (req.SourceType == SourceType.Asset)
            {
                ti.Params = new byte[20];
                assetID.ToBytes(ti.Params, 0);
                var assetType = (int)asset.Type;
                byte[] b = BitConverter.GetBytes(assetType);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(b);
                }
                Array.Copy(b, 0, ti.Params, 16, 4);
            }
            else if (req.SourceType == SourceType.SimInventoryItem)
            {
                ti.Params = req.Params;
            }
            SendMessage(ti);

            const int MAX_PACKET_SIZE = 1100;
            int packetNumber = 0;
            int assetOffset = 0;
            while (assetOffset < asset.Data.Length)
            {
                var tp = new TransferPacket()
                {
                    Packet = packetNumber++,
                    ChannelType = 2,
                    TransferID = req.TransferID
                };
                if (asset.Data.Length - assetOffset > MAX_PACKET_SIZE)
                {
                    tp.Data = new byte[MAX_PACKET_SIZE];
                    Buffer.BlockCopy(asset.Data, assetOffset, tp.Data, 0, MAX_PACKET_SIZE);
                    assetOffset += MAX_PACKET_SIZE;
                    tp.Status = TransferPacket.StatusCode.Success;
                }
                else
                {
                    tp.Data = new byte[asset.Data.Length - assetOffset];
                    Buffer.BlockCopy(asset.Data, assetOffset, tp.Data, 0, asset.Data.Length - assetOffset);
                    tp.Status = TransferPacket.StatusCode.Done;
                    assetOffset = asset.Data.Length;
                }
                SendMessage(tp);
            }
            if (Server.LogTransferPacket)
            {
                m_Log.DebugFormat("Completed download of asset {0} (TransferPacket)", assetID);
            }
        }

        private void FetchInventoryThread_CopyInventoryItem(Message m)
        {
            var req = (CopyInventoryItem)m;
            if (req.SessionID != SessionID || req.AgentID != AgentID)
            {
                return;
            }

            foreach (var reqd in req.InventoryData)
            {
                InventoryItem item;
                try
                {
                    item = Agent.InventoryService.Item[reqd.OldAgentID, reqd.OldItemID];
                    if(!item.CheckPermissions(Agent.Owner, Agent.Group, InventoryPermissionsMask.Copy))
                    {
                        /* skip item */
                        continue;
                    }
                    item.SetNewID(UUID.Random);
                    if (reqd.NewName.Length != 0)
                    {
                        item.Name = reqd.NewName;
                    }
                    if (item.Owner.ID != Agent.Owner.ID)
                    {
                        if(!item.CheckPermissions(Agent.Owner, Agent.Group, InventoryPermissionsMask.Transfer))
                        {
                            continue;
                        }
                        item.Permissions.Current = item.Permissions.NextOwner;
                        item.Permissions.Base = item.Permissions.NextOwner;
                        item.Permissions.EveryOne &= item.Permissions.NextOwner;
                    }
                    item.Owner = Agent.Owner;
                }
                catch
                {
                    continue;
                }

                try
                {
                    Agent.InventoryService.Item.Add(item);
                    SendMessage(new UpdateCreateInventoryItem(AgentID, true, UUID.Zero, item, reqd.CallbackID));
                }
                catch
                {
                    var res = new AlertMessage()
                    {
                        Message = this.GetLanguageString(Agent.CurrentCulture, "FailedToCopyItem", "Failed to copy item")
                    };
                    SendMessage(res);
                }
            }
        }

        private void FetchInventoryThread_CreateInventoryFolder(Message m)
        {
            var req = (CreateInventoryFolder)m;
            if (req.SessionID != SessionID || req.AgentID != AgentID)
            {
                return;
            }

            try
            {
                InventoryFolder folder;
                if(!Agent.InventoryService.Folder.ContainsKey(AgentID, req.ParentFolderID))
                {
                    SendMessage(new AlertMessage("ALERT: CantCreateRequestedInvFolder"));
                    return;
                }
                folder = new InventoryFolder()
                {
                    ID = req.FolderID,
                    ParentFolderID = req.ParentFolderID,
                    InventoryType = req.FolderType,
                    Name = req.FolderName,
                    Owner = Agent.Owner,
                    Version = 1
                };
                Agent.InventoryService.Folder.Add(folder);

                var res = new BulkUpdateInventory()
                {
                    AgentID = req.AgentID,
                    TransactionID = UUID.Zero
                };
                res.AddInventoryFolder(folder);
                Agent.SendMessageAlways(res, Scene.ID);
            }
            catch (Exception e)
            {
                m_Log.DebugFormat("Cannot create inventory folder: {0} {1}\n{2}", e.GetType().FullName, e.Message, e.StackTrace);
                SendMessage(new AlertMessage("ALERT: CantCreateRequestedInvFolder"));
            }
        }

        private void FetchInventoryThread_FetchInventory(Message m)
        {
            var req = (FetchInventory)m;
            if (req.SessionID != SessionID || req.AgentID != AgentID)
            {
                return;
            }

            FetchInventoryReply res = null;
            InventoryItem item;
            foreach (var d in req.InventoryData)
            {
                try
                {
                    item = Agent.InventoryService.Item[d.OwnerID, d.ItemID];
                }
                catch
                {
                    continue;
                }

                if (res == null)
                {
                    res = new FetchInventoryReply()
                    {
                        AgentID = req.AgentID
                    };
                }

                var rd = new FetchInventoryReply.ItemDataEntry()
                {
                    ItemID = item.ID,
                    FolderID = item.ParentFolderID,
                    CreatorID = item.Creator.ID,
                    OwnerID = item.Owner.ID,
                    GroupID = item.Group.ID,
                    BaseMask = item.Permissions.Current,
                    OwnerMask = item.Permissions.Current,
                    GroupMask = item.Permissions.Group,
                    EveryoneMask = item.Permissions.EveryOne,
                    NextOwnerMask = item.Permissions.NextOwner,
                    IsGroupOwned = false,
                    AssetID = item.AssetID,
                    Type = item.AssetType,
                    InvType = item.InventoryType,
                    Flags = item.Flags,
                    SaleType = item.SaleInfo.Type,
                    SalePrice = item.SaleInfo.Price,
                    Name = item.Name,
                    Description = item.Description,
                    CreationDate = (uint)item.CreationDate.DateTimeToUnixTime()
                };
                res.ItemData.Add(rd);

                if (res.ItemData.Count == MAX_ITEMS_PER_PACKET)
                {
                    SendMessage(res);
                    res = null;
                }
            }

            if (res != null)
            {
                SendMessage(res);
            }
        }

        private void FetchInventoryThread_FetchInventoryDescendents(Message m)
        {
            var req = (FetchInventoryDescendents)m;
            if (req.SessionID != SessionID || req.AgentID != AgentID)
            {
                return;
            }

            InventoryFolder thisfolder;

            try
            {
                thisfolder = Agent.InventoryService.Folder[req.OwnerID, req.FolderID];
            }
            catch
            {
                return;
            }

            List<InventoryFolder> folders;
            List<InventoryItem> items;

            try
            {
                folders = Agent.InventoryService.Folder.GetFolders(req.OwnerID, req.FolderID);
            }
            catch
            {
                folders = new List<InventoryFolder>();
            }

            try
            {
                items = Agent.InventoryService.Folder.GetItems(req.OwnerID, req.FolderID);
            }
            catch
            {
                items = new List<InventoryItem>();
            }

            InventoryDescendents res = null;
            bool message_sent = false;

            if (req.FetchFolders)
            {
                foreach (InventoryFolder folder in folders)
                {
                    if (res == null)
                    {
                        res = new InventoryDescendents()
                        {
                            AgentID = req.AgentID,
                            FolderID = req.FolderID,
                            OwnerID = thisfolder.Owner.ID,
                            Version = thisfolder.Version,
                            Descendents = folders.Count + items.Count
                        };
                    }
                    var d = new InventoryDescendents.FolderDataEntry()
                    {
                        FolderID = folder.ID,
                        ParentID = folder.ParentFolderID,
                        Type = folder.InventoryType,
                        Name = folder.Name
                    };
                    res.FolderData.Add(d);
                    if (res.FolderData.Count == MAX_FOLDERS_PER_PACKET)
                    {
                        SendMessage(res);
                        message_sent = true;
                        res = null;
                    }
                }
                if (res != null)
                {
                    SendMessage(res);
                    message_sent = true;
                    res = null;
                }
            }

            if (req.FetchItems)
            {
                foreach (InventoryItem item in items)
                {
                    if (res == null)
                    {
                        res = new InventoryDescendents()
                        {
                            AgentID = req.AgentID,
                            FolderID = req.FolderID,
                            OwnerID = thisfolder.Owner.ID,
                            Version = thisfolder.Version,
                            Descendents = folders.Count + items.Count
                        };
                    }
                    var d = new InventoryDescendents.ItemDataEntry()
                    {
                        ItemID = item.ID,
                        FolderID = item.ParentFolderID,
                        CreatorID = item.Creator.ID,
                        OwnerID = item.Owner.ID,
                        GroupID = item.Group.ID,
                        BaseMask = item.Permissions.Current,
                        OwnerMask = item.Permissions.Current,
                        GroupMask = item.Permissions.Group,
                        EveryoneMask = item.Permissions.EveryOne,
                        NextOwnerMask = item.Permissions.NextOwner,
                        IsGroupOwned = item.IsGroupOwned,
                        AssetID = item.AssetID,
                        Type = item.AssetType,
                        InvType = item.InventoryType,
                        Flags = item.Flags,
                        SaleType = item.SaleInfo.Type,
                        SalePrice = item.SaleInfo.Price,
                        Name = item.Name,
                        Description = item.Description,
                        CreationDate = (uint)item.CreationDate.DateTimeToUnixTime()
                    };
                    res.ItemData.Add(d);

                    if (res.ItemData.Count == MAX_ITEMS_PER_PACKET)
                    {
                        SendMessage(res);
                        message_sent = true;
                        res = null;
                    }
                }
                if (res != null)
                {
                    SendMessage(res);
                    message_sent = true;
                    res = null;
                }
            }

            if (!message_sent)
            {
                res = new InventoryDescendents()
                {
                    AgentID = req.AgentID,
                    FolderID = req.FolderID,
                    OwnerID = thisfolder.Owner.ID,
                    Version = thisfolder.Version,
                    Descendents = folders.Count + items.Count
                };
                SendMessage(res);
            }
        }

        private void FetchInventoryThread_LinkInventoryItem(Message m)
        {
            var req = (LinkInventoryItem)m;
            if (req.SessionID != SessionID || req.AgentID != AgentID)
            {
                return;
            }

            var item = new InventoryItem()
            {
                Owner = Agent.Owner,
                Creator = Agent.Owner,
                ParentFolderID = req.FolderID,
                Name = req.Name,
                Description = req.Description,
                Flags = 0,
                AssetID = req.OldItemID,
                AssetType = req.AssetType,
                InventoryType = req.InvType
            };
            item.Permissions.Base = InventoryPermissionsMask.All | InventoryPermissionsMask.Export;
            item.Permissions.Current = InventoryPermissionsMask.All | InventoryPermissionsMask.Export;
            item.Permissions.EveryOne = InventoryPermissionsMask.All;
            item.Permissions.NextOwner = InventoryPermissionsMask.All | InventoryPermissionsMask.Export;
            item.Permissions.Group = InventoryPermissionsMask.All | InventoryPermissionsMask.Export;
            try
            {
                Agent.InventoryService.Item.Add(item);
                SendMessage(new UpdateCreateInventoryItem(AgentID, true, req.TransactionID, item, req.CallbackID));
            }
            catch (Exception e)
            {
                m_Log.DebugFormat("LinkInventoryItem failed {0} {1}\n{2}", e.GetType().FullName, e.Message, e.StackTrace);

                var res = new AlertMessage()
                {
                    Message = "ALERT: CantCreateInventory"
                };
                SendMessage(res);
            }
        }

        private void FetchInventoryThread_MoveInventoryFolder(Message m)
        {
            var req = (MoveInventoryFolder)m;
            if (req.SessionID != SessionID || req.AgentID != AgentID)
            {
                return;
            }

            foreach (var d in req.InventoryData)
            {
                try
                {
                    Agent.InventoryService.Folder.Move(AgentID, d.FolderID, d.ParentID);
                }
                catch (Exception e)
                {
                    m_Log.DebugFormat("MoveInventoryFolder failed {0} {1}\n{2}", e.GetType().FullName, e.Message, e.StackTrace);
                }
            }
        }

        private void FetchInventoryThread_MoveInventoryItem(Message m)
        {
            var req = (MoveInventoryItem)m;
            if (req.SessionID != SessionID || req.AgentID != AgentID)
            {
                return;
            }

            foreach (var d in req.InventoryData)
            {
                try
                {
                    Agent.InventoryService.Item.Move(AgentID, d.ItemID, d.FolderID);
                }
                catch (Exception e)
                {
                    m_Log.DebugFormat("MoveInventoryItem failed {0} {1}\n{2}", e.GetType().FullName, e.Message, e.StackTrace);
                }
            }
        }

        private void FetchInventoryThread_PurgeInventoryDescendents(Message m)
        {
            var req = (PurgeInventoryDescendents)m;
            if (req.SessionID != SessionID || req.AgentID != AgentID)
            {
                return;
            }

            Agent.InventoryService.Folder.Purge(AgentID, req.FolderID);
        }

        private void FetchInventoryThread_RemoveInventoryFolder(Message m)
        {
            var req = (RemoveInventoryFolder)m;
            if (req.SessionID != SessionID || req.AgentID != AgentID)
            {
                return;
            }

            foreach (var id in req.FolderData)
            {
                try
                {
                    Agent.InventoryService.Folder.Delete(AgentID, id);
                }
                catch
                {
                    /* no action possible */
                }
            }
        }

        private void FetchInventoryThread_RemoveInventoryItem(Message m)
        {
            var req = (RemoveInventoryItem)m;
            if (req.SessionID != SessionID || req.AgentID != AgentID)
            {
                return;
            }

            foreach (var id in req.InventoryData)
            {
                try
                {
                    Agent.InventoryService.Item.Delete(AgentID, id);
                }
                catch
                {
                    /* no action possible */
                }
            }
        }

        private void FetchInventoryThread_RemoveInventoryObjects(Message m)
        {
            var req = (RemoveInventoryObjects)m;
            if (req.SessionID != SessionID || req.AgentID != AgentID)
            {
                return;
            }

            Agent.InventoryService.Folder.Delete(AgentID, req.FolderIDs);
            Agent.InventoryService.Item.Delete(AgentID, req.ItemIDs);
        }

        private void FetchInventoryThread_UpdateInventoryFolder(Message m)
        {
            var req = (UpdateInventoryFolder)m;
            if (req.SessionID != SessionID || req.AgentID != AgentID)
            {
                return;
            }

            foreach (UpdateInventoryFolder.InventoryDataEntry d in req.InventoryData)
            {
                try
                {
                    InventoryFolder folder = Agent.InventoryService.Folder[AgentID, d.FolderID];
                    folder.Name = d.Name;
                    folder.InventoryType = d.Type;
                    folder.ParentFolderID = d.ParentID;
                    Agent.InventoryService.Folder.Update(folder);
                }
                catch
                {
                    var res = new AlertMessage()
                    {
                        Message = string.Format("Could not update folder {0}", d.Name)
                    };
                    SendMessage(res);
                }
            }
        }

        private void FetchInventoryThread_ActivateGestures(Message m)
        {
            var req = (ActivateGestures)m;
            if(req.SessionID != SessionID || req.AgentID != AgentID)
            {
                return;
            }

            foreach(var d in req.Data)
            {
                InventoryItem item;
                try
                {
                    item = Agent.InventoryService.Item[AgentID, d.ItemID];
                }
                catch
                {
                    continue;
                }

                if (item.Owner.ID != AgentID)
                {
                    continue;
                }
                if (item.InventoryType == InventoryType.Gesture)
                {
                    item.Flags |= InventoryFlags.GestureActive;
                    SendMessage(new BulkUpdateInventory(AgentID, UUID.Zero, 0, item));
                }
            }
        }

        private void FetchInventoryThread_DeactivateGestures(Message m)
        {
            var req = (DeactivateGestures)m;
            if (req.SessionID != SessionID || req.AgentID != AgentID)
            {
                return;
            }

            foreach (var d in req.Data)
            {
                InventoryItem item;
                try
                {
                    item = Agent.InventoryService.Item[AgentID, d.ItemID];
                }
                catch
                {
                    continue;
                }

                if (item.Owner.ID != AgentID)
                {
                    continue;
                }
                if (item.InventoryType == InventoryType.Gesture)
                {
                    item.Flags &= ~InventoryFlags.GestureActive;
                    SendMessage(new BulkUpdateInventory(AgentID, UUID.Zero, 0, item));
                }
            }
        }

        private void FetchInventoryThread_UpdateInventoryItem(Message m)
        {
            var req = (UpdateInventoryItem)m;
            if (req.SessionID != SessionID || req.AgentID != AgentID)
            {
                return;
            }

            foreach (var d in req.InventoryData)
            {
                InventoryItem item;
                try
                {
                    item = Agent.InventoryService.Item[AgentID, d.ItemID];
                }
                catch
                {
                    continue;
                }

                if (item.Owner.ID != AgentID)
                {
                    continue;
                }

                item.Name = d.Name;
                item.Description = d.Description;

                bool sendUpdate = false;
                if (d.NextOwnerMask != 0)
                {
                    var p = new InventoryPermissionsData()
                    {
                        Base = d.BaseMask,
                        Current = d.OwnerMask,
                        NextOwner = d.NextOwnerMask,
                        EveryOne = d.EveryoneMask,
                        Group = d.GroupMask
                    };
                    if ((item.Permissions.Base & (InventoryPermissionsMask.All | InventoryPermissionsMask.Export)) != (InventoryPermissionsMask.All | InventoryPermissionsMask.Export) ||
                        (item.Permissions.Current & InventoryPermissionsMask.Export) == 0 ||
                        item.Creator.ID != item.Owner.ID)
                    {
                        // If we are not allowed to change it, then force it to the
                        // original item's setting and if it was on, also force full perm
                        if ((item.Permissions.EveryOne & InventoryPermissionsMask.Export) != 0)
                        {
                            p.NextOwner = InventoryPermissionsMask.All;
                            p.EveryOne |= InventoryPermissionsMask.Export;
                        }
                        else
                        {
                            p.EveryOne &= ~InventoryPermissionsMask.Export;
                        }
                    }
                    else
                    {
                        // If the new state is exportable, force full perm
                        if ((p.EveryOne & InventoryPermissionsMask.Export) != 0)
                        {
                            p.NextOwner = InventoryPermissionsMask.All;
                        }
                    }

                    if (item.Permissions.NextOwner != (p.NextOwner & item.Permissions.Base))
                    {
                        item.Permissions.NextOwner = p.NextOwner & item.Permissions.Base;
                    }

                    if (item.Permissions.EveryOne != (p.EveryOne & item.Permissions.Base))
                    {
                        item.Permissions.EveryOne = p.EveryOne & item.Permissions.Base;
                    }

                    if (item.Permissions.Group != (p.Group & item.Permissions.Base))
                    {
                        item.Permissions.Group = p.Group & item.Permissions.Base;
                    }

                    try
                    {
                        item.Group = Agent.GroupsService.Groups[Agent.Owner, new UGI(d.GroupID)].ID;
                    }
                    catch
                    {
                        item.Group.ID = d.GroupID;
                    }
                    item.IsGroupOwned = d.IsGroupOwned;

                    item.CreationDate = (d.CreationDate == 0) ?
                        new Date() :
                        Date.UnixTimeToDateTime(d.CreationDate);

                    item.InventoryType = d.InvType;

                    item.SaleInfo.Price = d.SalePrice;
                    item.SaleInfo.Type = d.SaleType;

                    if (item.InventoryType == InventoryType.Wearable && ((uint)d.Flags & 0xf) == 0 && ((uint)d.Flags & 0xf) != 0)
                    {
                        item.Flags = (InventoryFlags)(((uint)item.Flags & 0xfffffff0) | ((uint)d.Flags & 0xf));
                        sendUpdate = true;
                    }

                    try
                    {
                        Agent.InventoryService.Item.Update(item);
                        SendMessage(new UpdateCreateInventoryItem(AgentID, true, req.TransactionID, item, 0));
                    }
                    catch
                    {
                        /* no action possible, missing update will signal no action */
                    }
                }

                if (UUID.Zero != req.TransactionID)
                {
                    Agent.SetAssetUploadAsUpdateInventoryItem(req.TransactionID, item, Scene.ID, d.CallbackID);
                }
                else
                {
                    // In other situations we cannot send out a bulk update here, since this will cause editing of clothing to start 
                    // failing frequently.  Possibly this is a race with a separate transaction that uploads the asset.
                    if (sendUpdate)
                    {
                        SendMessage(new BulkUpdateInventory(AgentID, UUID.Zero, 0, item));
                    }
                }
            }
        }

        private UUID CreateLandmarkForInventory(InventoryItem item)
        {
            Vector3 pos = Agent.GlobalPosition;
            UUID curSceneID = Agent.SceneID;
            SceneInterface curScene;
            try
            {
                curScene = Agent.Circuits[curSceneID].Scene;
            }
            catch
            {
                SendMessage(new AlertMessage("ALERT: CantCreateLandmark"));
                return UUID.Zero;
            }

            var lm = new Landmark();
            if (!string.IsNullOrEmpty(GatekeeperURI))
            {
                lm.GatekeeperURI = new URI(GatekeeperURI);
            }
            lm.LocalPos = pos;
            lm.RegionID = curSceneID;
            lm.Location = curScene.GridPosition;

            AssetData asset = lm;
            asset.Name = item.Name;
            asset.ID = UUID.Random;
            try
            {
                Agent.AssetService.Store(asset);
            }
            catch (Exception e)
            {
                SendMessage(new Messages.Alert.AlertMessage("ALERT: CantCreateLandmark"));
                m_Log.Error("Failed to create asset for landmark", e);
                return UUID.Zero;
            }
            return asset.ID;
        }

        private UUID CreateDefaultScriptForInventory(InventoryItem item) =>
            /* this is the KAN-Ed llSay script */
            new UUID("366ac8e9-b391-11dc-8314-0800200c9a66");

        private UUID CreateDefaultGestureForInventory(InventoryItem item)
        {
            AssetData asset = new Gesture();
            asset.Name = "New Gesture";
            asset.ID = new UUID("cf83499a-6547-4b07-8669-ff1d567071d3");
            try
            {
                Agent.AssetService.Store(asset);
            }
            catch (Exception e)
            {
                SendMessage(new AlertMessage("ALERT: CantCreateRequestedInv"));
                m_Log.Error("Failed to create asset for gesture", e);
                return UUID.Zero;
            }
            return asset.ID;
        }

        private UUID CreateDefaultNotecardForInventory(InventoryItem item)
        {
            AssetData asset = new Notecard();
            asset.Name = "New Note";
            asset.ID = new UUID("43b761c3-5e3f-43c5-8bc9-d048f8df496f");
            try
            {
                Agent.AssetService.Store(asset);
            }
            catch (Exception e)
            {
                SendMessage(new AlertMessage("ALERT: CantCreateRequestedInv"));
                m_Log.Error("Failed to create asset for notecard", e);
                return UUID.Zero;
            }
            return asset.ID;
        }

        private void FetchInventoryThread_CreateInventoryItem(CreateInventoryItem req)
        {
            if (req.SessionID != SessionID || req.AgentID != AgentID)
            {
                return;
            }
            InventoryFolder folder;
            InventoryItem item;
            try
            {
                /* check availability for folder first before doing anything else */
                if (req.FolderID == UUID.Zero)
                {
                    folder = Agent.InventoryService.Folder[AgentID, req.AssetType];
                }
                else
                {
                    folder = Agent.InventoryService.Folder[AgentID, req.FolderID];
                }
            }
            catch
#if DEBUG
                (Exception e)
#endif
            {
#if DEBUG
                m_Log.DebugFormat("Failed to create inventory: {0}: {1}\n{2}", e.GetType().FullName, e.Message, e.StackTrace);
#endif
                SendMessage(new AlertMessage("ALERT: CantCreateInventory"));
                return;
            }

            item = new InventoryItem()
            {
                InventoryType = req.InvType,
                AssetType = req.AssetType,
                Description = req.Description,
                Name = req.Name,
                Owner = Agent.Owner,
                Creator = Agent.Owner,
                ParentFolderID = folder.ID
            };

            if(item.AssetType == AssetType.Clothing || item.AssetType == AssetType.Bodypart)
            {
                item.Flags = (InventoryFlags)(byte)req.WearableType;
            }
            item.SaleInfo.Type = InventoryItem.SaleInfoData.SaleType.NoSale;
            item.SaleInfo.Price = 0;
            item.SaleInfo.PermMask = InventoryPermissionsMask.All;

            item.Permissions.Base = InventoryPermissionsMask.All | InventoryPermissionsMask.Export;
            item.Permissions.Current = InventoryPermissionsMask.All | InventoryPermissionsMask.Export;
            item.Permissions.Group = InventoryPermissionsMask.None;
            item.Permissions.EveryOne = InventoryPermissionsMask.None;
            item.Permissions.NextOwner = req.NextOwnerMask;


            if (req.TransactionID == UUID.Zero)
            {
                try
                {
                    switch (item.InventoryType)
                    {
                        case InventoryType.Landmark:
                            item.AssetID = CreateLandmarkForInventory(item);
                            break;

                        case InventoryType.LSLText:
                            item.AssetID = CreateDefaultScriptForInventory(item);
                            break;

                        case InventoryType.Notecard:
                            item.AssetID = CreateDefaultNotecardForInventory(item);
                            break;

                        case InventoryType.Gesture:
                            item.AssetID = CreateDefaultGestureForInventory(item);
                            break;

                        case InventoryType.Animation:
                            item.AssetID = new UUID("ddc2400f-ecdb-b00e-aee7-442ff99d5fb7"); /* this is the handshake animation */
                            break;

                        default:
                            item.AssetID = UUID.Zero;
                            break;
                    }
                }
                catch (Exception e)
                {
                    SendMessage(new AlertMessage("ALERT: CantCreateInventory"));
                    m_Log.ErrorFormat("Failed to create asset for type {0}: {1}: {2}\n{3}", item.InventoryType.ToString(), e.GetType().FullName, e.Message, e.StackTrace);
                    return;
                }
                if (UUID.Zero == item.AssetID && item.AssetType != AssetType.CallingCard)
                {
                    SendMessage(new AlertMessage("ALERT: CantCreateInventory"));
                    m_Log.ErrorFormat("Failed to create asset for type {0}", item.InventoryType.ToString());
                    return;
                }

                try
                {
                    Agent.InventoryService.Item.Add(item);
                }
                catch (Exception e)
                {
                    SendMessage(new AlertMessage(item.InventoryType == InventoryType.Landmark ?
                        "ALERT: CantCreateLandmark" :
                        "ALERT: CantCreateInventory"));
                    m_Log.Error("Failed to create inventory item for inventory", e);
                    return;
                }
                SendMessage(new UpdateCreateInventoryItem(AgentID, true, req.TransactionID, item, req.CallbackID));
            }
            else
            {
                Agent.SetAssetUploadAsCreateInventoryItem(req.TransactionID, item, Scene.ID, req.CallbackID);
            }
        }
        #endregion
    }
}

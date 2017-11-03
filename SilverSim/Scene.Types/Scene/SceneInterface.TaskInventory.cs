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
using SilverSim.ServiceInterfaces;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using SilverSim.Types.ServerURIs;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.TaskInventory;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace SilverSim.Scene.Types.Scene
{
    [ServerParam("DoNotAddScriptsToTrashFolder", ParameterType = typeof(bool), DefaultValue = false)]
    public partial class SceneInterface
    {
        private bool m_DoNotAddScriptsToTrashFolderUpdatedLocal;
        private bool m_DoNotAddScriptsToTrashFolderUpdatedGlobal;
        private bool m_DoNotAddScriptsToTrashFolderUpdatedSetToLocal;

        [ServerParam("DoNotAddScriptsToTrashFolder", ParameterType = typeof(bool))]
        public void DoNotAddScriptsToTrashFolderUpdated(UUID regionID, string value)
        {
            ParameterUpdatedHandler(
                ref m_DoNotAddScriptsToTrashFolderUpdatedLocal,
                ref m_DoNotAddScriptsToTrashFolderUpdatedGlobal,
                ref m_DoNotAddScriptsToTrashFolderUpdatedSetToLocal,
                regionID, value);
        }

        private bool DoNotAddScriptsToTrashFolder => m_DoNotAddScriptsToTrashFolderUpdatedSetToLocal ? m_DoNotAddScriptsToTrashFolderUpdatedLocal : m_DoNotAddScriptsToTrashFolderUpdatedGlobal;

        private bool TryGetServices(UUI targetAgentId, out InventoryServiceInterface inventoryService, out AssetServiceInterface assetService)
        {
            UserAgentServiceInterface userAgentService = null;
            inventoryService = null;
            assetService = null;
            if (targetAgentId.HomeURI == null)
            {
                return false;
            }
            var homeUri = targetAgentId.HomeURI.ToString();
            var heloheaders = ServicePluginHelo.HeloRequest(homeUri);
            foreach (IUserAgentServicePlugin userAgentPlugin in UserAgentServicePlugins)
            {
                if (userAgentPlugin.IsProtocolSupported(homeUri, heloheaders))
                {
                    userAgentService = userAgentPlugin.Instantiate(homeUri);
                }
            }

            if (userAgentService == null)
            {
                return false;
            }

            ServerURIs serverurls = userAgentService.GetServerURLs(targetAgentId);
            string inventoryServerURI = serverurls.InventoryServerURI;
            string assetServerURI = serverurls.AssetServerURI;

            heloheaders = ServicePluginHelo.HeloRequest(inventoryServerURI);
            foreach (IInventoryServicePlugin inventoryPlugin in InventoryServicePlugins)
            {
                if (inventoryPlugin.IsProtocolSupported(inventoryServerURI, heloheaders))
                {
                    inventoryService = inventoryPlugin.Instantiate(inventoryServerURI);
                    break;
                }
            }

            heloheaders = ServicePluginHelo.HeloRequest(assetServerURI);
            foreach (IAssetServicePlugin assetPlugin in AssetServicePlugins)
            {
                if (assetPlugin.IsProtocolSupported(assetServerURI, heloheaders))
                {
                    assetService = assetPlugin.Instantiate(assetServerURI);
                    break;
                }
            }

            return inventoryService != null && assetService != null;
        }

        [PacketHandler(MessageType.UpdateTaskInventory)]
        internal void HandleUpdateTaskInventory(Message m)
        {
            var req = (UpdateTaskInventory)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            IAgent agent;
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            ObjectPart part;
            if (!Primitives.TryGetValue(req.LocalID, out part))
            {
                return;
            }

            if (part.IsLocked)
            {
                /* locked prims are not allowed */
                return;
            }

#if DEBUG
            m_Log.DebugFormat("UpdateTaskInventory key={0}", req.Key.ToString());
#endif
            if(req.Key != UpdateTaskInventory.KeyType.InventoryId)
            {
                return;
            }

            if(part.Inventory.ContainsKey(req.ItemID))
            {
                UpdateTaskInventoryItem(req, agent, part);
            }
            else
            {
                AddTaskInventoryItem(req, agent, part);
            }
        }

        private void AddTaskInventoryItem(UpdateTaskInventory req, IAgent agent, ObjectPart part)
        {
            switch(req.InvType)
            {
                case InventoryType.Animation:
                case InventoryType.Attachable:
                case InventoryType.CallingCard:
                case InventoryType.Gesture:
                case InventoryType.Landmark:
                case InventoryType.Notecard:
                case InventoryType.Object:
                case InventoryType.Snapshot:
                case InventoryType.Sound:
                case InventoryType.Texture:
                case InventoryType.Wearable:
                    break;

                default:
                    /* do not allow anything else than the ones above */
                    return;
            }

            switch(req.AssetType)
            {
                case AssetType.LSLText:
                case AssetType.LSLBytecode:
                case AssetType.Link:
                case AssetType.LinkFolder:
                    /* no addition here of scripts, item links or folder links */
                    return;

                default:
                    break;
            }

            if (part.IsAllowedDrop)
            {
                /* llAllowInventoryDrop active, so we can drop anything except scripts */
            }
            else if(!CanEdit(agent, part.ObjectGroup, part.ObjectGroup.GlobalPosition))
            {
                /* not allowed */
                return;
            }

            InventoryItem agentItem;
            if(agent.InventoryService.Item.TryGetValue(agent.Owner.ID, req.ItemID, out agentItem) &&
                agentItem.AssetType == req.AssetType &&
                agentItem.InventoryType == req.InvType)
            {
                var item = new ObjectPartInventoryItem(UUID.Random, agentItem);
                AdjustPermissionsAccordingly(agent, part.Owner, item);
                item.LastOwner = item.Owner;
                item.Owner = part.Owner;

                if (AssetService.Exists(agentItem.AssetID))
                {
                    /* no need for an assettransferer here */

                    part.Inventory.Add(item);

                    part.SendObjectUpdate();

                    SendObjectPropertiesToAgent(agent, part);

                    if (agentItem.CheckPermissions(agent.Owner, agent.Group, InventoryPermissionsMask.Copy))
                    {
                        agent.InventoryService.Item.Delete(agent.Owner.ID, agentItem.ID);
                    }
                }
                else
                {
                    new AddToObjectTransferItem(agent, this, item.AssetID, part, item).QueueWorkItem();
                }
            }
        }

        private void AdjustPermissionsAccordingly(IAgent agent, UUI newOwner, ObjectPartInventoryItem item)
        {
            if(agent.Owner != newOwner && !agent.IsActiveGod)
            {
                item.Permissions.AdjustToNextOwner();
            }
        }

        private sealed class UpdateTaskInventoryItemHandler
        {
            public SceneInterface Scene;
            public IAgent Agent;
            public ObjectPart Part;
            public UUID ItemID;

            public void OnCompletion(UUID assetid)
            {
                ObjectPartInventoryItem item;
                if(Part.Inventory.TryGetValue(ItemID, out item))
                {
                    item.AssetID = assetid;

                    Interlocked.Increment(ref Part.Inventory.InventorySerial);
                    Part.SendObjectUpdate();
                    Scene.SendObjectPropertiesToAgent(Agent, Part);
                }
            }
        }

        private void UpdateTaskInventoryItem(UpdateTaskInventory req, IAgent agent, ObjectPart part)
        {
            ObjectPartInventoryItem item;

            if(!CanEdit(agent, part.ObjectGroup, part.ObjectGroup.GlobalPosition))
            {
                return;
            }

            if(!part.Inventory.TryGetValue(req.ItemID, out item))
            {
                return;
            }

            if(req.TransactionID != UUID.Zero)
            {
                if (item.AssetType == AssetType.LSLText || item.AssetType == AssetType.LSLBytecode ||
                    item.InventoryType == InventoryType.LSL)
                {
                    /* do not allow editing scripts through this */
                    return;
                }

                /* asset upload follows */
                agent.SetAssetUploadAsCompletionAction(req.TransactionID, req.CircuitSceneID, 
                    new UpdateTaskInventoryItemHandler { Agent = agent, ItemID = req.ItemID, Part = part, Scene = this }.OnCompletion);
            }
            else
            {
                /* no asset upload */
                part.Inventory.Rename(req.Name, item.ID);
                item.Description = req.Description;
                if(agent.IsActiveGod)
                {
                    item.Permissions.Base = req.BaseMask | InventoryPermissionsMask.Move;
                    item.Permissions.Current = req.OwnerMask;
                    item.Permissions.EveryOne = req.EveryoneMask;
                    item.Permissions.Group = req.GroupMask;
                    item.Permissions.NextOwner = req.NextOwnerMask;
                }
                else if(part.Owner.EqualsGrid(agent.Owner))
                {
                    item.Permissions.EveryOne = req.EveryoneMask & item.Permissions.Base;
                    item.Permissions.Group = req.GroupMask & item.Permissions.Base;
                    item.Permissions.Current = req.OwnerMask & item.Permissions.Base;
                    item.Permissions.NextOwner = req.OwnerMask & item.Permissions.Base;
                }
                Interlocked.Increment(ref part.Inventory.InventorySerial);
                part.SendObjectUpdate();
                SendObjectPropertiesToAgent(agent, part);
            }
        }

        public void SendObjectPropertiesToAgent(IAgent agent, ObjectPart part)
        {
            if (agent.SelectedObjects(ID).Contains(part.ID))
            {
                using (var propHandler = new ObjectPropertiesSendHandler(agent, ID))
                {
                    propHandler.Send(part);
                }
            }
        }

        [PacketHandler(MessageType.MoveTaskInventory)]
        internal void HandleMoveTaskInventory(Message m)
        {
            var req = (MoveTaskInventory)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            IAgent agent;
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            ObjectPart part;
            if (!Primitives.TryGetValue(req.LocalID, out part))
            {
                return;
            }

            ObjectPartInventoryItem item;
            if(!part.Inventory.TryGetValue(req.ItemID, out item))
            {
                return;
            }

            var newItem = new InventoryItem(item);
            if (item.CheckPermissions(agent.Owner, agent.Group, InventoryPermissionsMask.Copy))
            {
                /* permissions okay */
            }
            else if(CanEdit(agent, part.ObjectGroup, part.ObjectGroup.GlobalPosition))
            {
                part.Inventory.Remove(item.ID);
            }
            else
            {
                /* we cannot edit */
                return;
            }
            new ObjectTransferItem(agent,
                this,
                newItem.AssetID,
                new List<InventoryItem>(new InventoryItem[] { newItem }),
                req.FolderID, newItem.AssetType).QueueWorkItem();
        }

        [PacketHandler(MessageType.RemoveTaskInventory)]
        internal void HandleRemoveTaskInventory(Message m)
        {
            var req = (RemoveTaskInventory)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            IAgent agent;
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            ObjectPart part;
            if (!Primitives.TryGetValue(req.LocalID, out part))
            {
                return;
            }

            ObjectPartInventoryItem item;

            if(CanEdit(agent, part.ObjectGroup, part.ObjectGroup.GlobalPosition) &&
                part.Inventory.TryGetValue(req.ItemID, out item))
            {
                UUI owner = item.Owner;
                var deletedItem = new InventoryItem(item);
                part.Inventory.Remove(req.ItemID);
                InventoryServiceInterface inventoryService;
                AssetServiceInterface assetService;
                if(deletedItem.AssetType == AssetType.LSLText && DoNotAddScriptsToTrashFolder)
                {
                    /* do not add scripts to trash if option is set */
                    return;
                }

                IAgent targetAgent;
                if(Agents.TryGetValue(owner.ID, out targetAgent) && agent.Owner.EqualsGrid(owner))
                {
                    new ObjectTransferItem(targetAgent,
                        this,
                        new List<UUID>(new UUID[] { deletedItem.AssetID }),
                        new List<InventoryItem>(new InventoryItem[] { deletedItem }),
                        AssetType.TrashFolder).QueueWorkItem();
                }
                else if (TryGetServices(owner, out inventoryService, out assetService))
                {
                    new ObjectTransferItem(inventoryService,
                        assetService,
                        deletedItem.Owner,
                        this,
                        new List<UUID>(new UUID[] { deletedItem.AssetID }),
                        new List<InventoryItem>(new InventoryItem[] { deletedItem }),
                        AssetType.TrashFolder).QueueWorkItem();
                }
            }
        }

        [PacketHandler(MessageType.RequestTaskInventory)]
        internal void HandleRequestTaskInventory(Message m)
        {
            var req = (RequestTaskInventory)m;
            if(req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            IAgent agent;
            if(!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            ObjectPart part;
            if(!Primitives.TryGetValue(req.LocalID, out part))
            {
                return;
            }

            bool canEdit = CanEdit(agent, part.ObjectGroup, part.ObjectGroup.GlobalPosition);

            List<ObjectPartInventoryItem> items = part.Inventory.Values;
            if(items.Count == 0)
            {
                var res = new ReplyTaskInventoryNone
                {
                    Serial = (short)part.Inventory.InventorySerial,
                    TaskID = part.ID
                };
                agent.SendMessageAlways(res, ID);
            }
            else
            {
                using(var ms = new MemoryStream())
                {
                    using(var w = ms.UTF8StreamWriter())
                    {
                        w.Write(InvFile_Header);
                        w.Write(string.Format(InvFile_NameValueLine, "obj_id", part.ID));
                        w.Write(string.Format(InvFile_NameValueLine, "parent_id", UUID.Zero));
                        w.Write(string.Format(InvFile_NameValueLine, "type", "category"));
                        w.Write(string.Format(InvFile_NameValueLine, "name", "Contents|"));
                        w.Write(InvFile_SectionEnd);
                        foreach (ObjectPartInventoryItem item in items)
                        {
                            w.Write(string.Format(
                                "\tinv_item\t0\n" +
                                "\t{{\n" +
                                "\t\titem_id\t{0}\n" +
                                "\t\tparent_id\t{1}\n" +
                                "\t\tpermissions 0\n" +
                                "\t\t{{\n" +
                                "\t\t\tbase_mask\t{2:x8}\n" +
                                "\t\t\towner_mask\t{3:x8}\n" +
                                "\t\t\tgroup_mask\t{4:x8}\n" +
                                "\t\t\teveryone_mask\t{5:x8}\n" +
                                "\t\t\tnext_owner_mask\t{6:x8}\n" +
                                "\t\t\tcreator_id\t{7}\n" +
                                "\t\t\towner_id\t{8}\n" +
                                "\t\t\tlast_owner_id\t{9}\n" +
                                "\t\t\tgroup_id\t{10}\n" +
                                "\t\t}}\n" +
                                "\t\tasset_id\t{11}\n" +
                                "\t\ttype\t{12}\n" +
                                "\t\tinv_type\t{13}\n" +
                                "\t\tflags\t{14:x8}\n" +
                                "\t\tsale_info 0\n" +
                                "\t\t{{\n" +
                                "\t\t\tsale_type\t{15}\n" +
                                "\t\t\tsale_price\t{16}\n" +
                                "\t\t}}\n" +
                                "\t\tname\t{17}|\n" +
                                "\t\tdesc\t{18}|\n" +
                                "\t\tcreation_date\t{19}\n" +
                                "\t}}\n", item.ID, part.ID,
                                            (uint)item.Permissions.Base,
                                            (uint)item.Permissions.Current,
                                            (uint)item.Permissions.Group,
                                            (uint)item.Permissions.EveryOne,
                                            (uint)item.Permissions.NextOwner,
                                            item.Creator.ID.ToString(),
                                            item.Owner.ID.ToString(),
                                            item.LastOwner.ID.ToString(),
                                            item.Group.ID.ToString(),
                                            canEdit ? item.AssetID.ToString() : UUID.Zero,
                                            item.AssetTypeName,
                                            item.InventoryTypeName,
                                            (uint)item.Flags,
                                            item.SaleInfo.TypeName,
                                            item.SaleInfo.Price,
                                            item.Name,
                                            item.Description,
                                            item.CreationDate.AsULong));
                        }
                    }

                    string fname = "inventory_" + UUID.Random.ToString() + ".tmp";
                    agent.AddNewFile(fname, ms.ToArray());

                    var res = new ReplyTaskInventory
                    {
                        Serial = (short)part.Inventory.InventorySerial,
                        Filename = fname,
                        TaskID = part.ID
                    };
                    agent.SendMessageAlways(res, ID);
                }
            }
        }

        private const string InvFile_Header = "\tinv_object\t0\n\t{\n";
        private const string InvFile_SectionEnd = "\t}\n";
        private const string InvFile_NameValueLine = "\t\t{0}\t{1}\n";
    }
}

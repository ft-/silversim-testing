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
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Scene.Types.Transfer;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Object;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SilverSim.Scene.Types.Scene
{
    public partial class SceneInterface
    {

        [PacketHandler(MessageType.ObjectSaleInfo)]
        public void HandleObjectSaleInfo(Message m)
        {
            var req = (ObjectSaleInfo)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }

            IAgent agent;
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            using (var propHandler = new ObjectPropertiesSendHandler(agent, ID))
            {
                foreach (ObjectSaleInfo.Data d in req.ObjectData)
                {
#if DEBUG
                    m_Log.DebugFormat("ObjectSaleInfo localid={0}", d.ObjectLocalID);
#endif

                    ObjectPart prim;
                    Object.ObjectGroup grp;
                    if (!Primitives.TryGetValue(d.ObjectLocalID, out prim))
                    {
                        continue;
                    }

                    grp = prim.ObjectGroup;
                    if(grp == null)
                    {
                        continue;
                    }

                    if (!CanEdit(agent, grp, grp.GlobalPosition))
                    {
                        continue;
                    }

                    if(d.SaleType == InventoryItem.SaleInfoData.SaleType.Original &&
                        prim.CheckPermissions(agent.Owner, agent.Group, InventoryPermissionsMask.Copy))
                    {
                        agent.SendAlertMessage(this.GetLanguageString(agent.CurrentCulture, "UnableToSellNoCopyItemAsACopy", "Unable to sell no copy item as a copy."), ID);
                        continue;
                    }

                    if(d.SaleType == InventoryItem.SaleInfoData.SaleType.Copy || d.SaleType == InventoryItem.SaleInfoData.SaleType.Original)
                    {
                        bool foundNoTransfer = false;
                        foreach (ObjectPart part in grp.Values)
                        {
                            if(!part.CheckPermissions(agent.Owner, agent.Group, InventoryPermissionsMask.Transfer))
                            {
                                agent.SendAlertMessage(this.GetLanguageString(agent.CurrentCulture, "UnableToSellNoTransferItem", "Unable to sell no transfer item."), ID);
                                foundNoTransfer = true;
                                break;
                            }
                            foreach (ObjectPartInventoryItem item in part.Inventory.Values)
                            {
                                if (item.CheckPermissions(agent.Owner, agent.Group, InventoryPermissionsMask.Transfer))
                                {
                                    agent.SendAlertMessage(this.GetLanguageString(agent.CurrentCulture, "UnableToSellNoTransferItem", "Unable to sell no transfer item."), ID);
                                    foundNoTransfer = true;
                                    break;
                                }
                            }
                        }
                        if (foundNoTransfer)
                        {
                            continue;
                        }

                    }

                    if (d.SaleType != InventoryItem.SaleInfoData.SaleType.NoSale)
                    {
                        bool foundNoCopyInventory = false;
                        bool foundNoTransfer = false;
                        foreach(ObjectPartInventoryItem item in prim.Inventory.Values)
                        {
                            if(item.CheckPermissions(agent.Owner, agent.Group, InventoryPermissionsMask.Transfer))
                            {
                                agent.SendAlertMessage(this.GetLanguageString(agent.CurrentCulture, "UnableToSellNoTransferItem", "Unable to sell no transfer item."), ID);
                                foundNoTransfer = true;
                                break;
                            }
                            if (!item.CheckPermissions(agent.Owner, agent.Group, InventoryPermissionsMask.Copy))
                            {
                                foundNoCopyInventory = true;
                            }
                        }
                        if (foundNoTransfer)
                        {
                            continue;
                        }

                        if (foundNoCopyInventory)
                        {
                            agent.SendAlertMessage(this.GetLanguageString(agent.CurrentCulture, "WarningInventoryWillBeSoldAsOriginalAndNotAsCopy", "Warning! Object inventory will be sold as original and not as copy."), ID);
                            continue;
                        }
                    }

                    prim.ObjectGroup.SalePrice = d.SalePrice;
                    prim.ObjectGroup.SaleType = d.SaleType;
                    propHandler.Send(prim);
                }
            }
        }

        struct ObjectBuyListen
        {
            public UUID PrimitiveID;
            public UUID ItemID;

            public string Key
            {
                get
                {
                    return GetKey(PrimitiveID, ItemID);
                }
            }

            public static string GetKey(UUID primID, UUID itemID)
            {
                return primID.ToString() + "-" + itemID.ToString();
            }
        }

        readonly RwLockedDictionary<string, ObjectBuyListen> m_ObjectBuyListeners = new RwLockedDictionary<string, ObjectBuyListen>();

        public void AddObjectBuyListen(ScriptInstance instance)
        {
            ObjectPart part = instance.Part;
            ObjectPartInventoryItem item = instance.Item;
            if(part != null && item != null)
            {
                UUID partID = part.ID;
                UUID itemID = item.ID;
                ObjectBuyListen listen = new ObjectBuyListen();
                listen.PrimitiveID = partID;
                listen.ItemID = itemID;
                m_ObjectBuyListeners.Add(listen.Key, listen);
            }
        }

        public void RemoveObjectBuyListen(ScriptInstance instance)
        {
            ObjectPart part = instance.Part;
            ObjectPartInventoryItem item = instance.Item;
            if (part != null && item != null)
            {
                UUID partID = part.ID;
                UUID itemID = item.ID;
                m_ObjectBuyListeners.Remove(ObjectBuyListen.GetKey(partID, itemID));
            }
        }

        [PacketHandler(MessageType.ObjectBuy)]
        public void HandleObjectBuy(Message m)
        {
            var req = (ObjectBuy)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }

            /* make object buy handle outside of the UDP Receive */
            ThreadPool.UnsafeQueueUserWorkItem(HandleObjectBuyWorkItem, req);
        }

        public void HandleObjectBuyWorkItem(object o)
        {
            var req = (ObjectBuy)o;
            IAgent agent;
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            foreach (ObjectBuy.Data data in req.ObjectData)
            {
                ObjectPart part;
                if(!Primitives.TryGetValue(data.ObjectLocalID, out part))
                {
                    agent.SendAlertMessage(this.GetLanguageString(agent.CurrentCulture, "UnableToBuyDueNotFoundObject", "Unable to buy. The object was not found."), ID);
                }
                else
                {
                    Object.ObjectGroup grp = part.ObjectGroup;
                    if(grp.SalePrice != data.SalePrice || grp.SaleType != data.SaleType)
                    {
                        agent.SendAlertMessage(this.GetLanguageString(agent.CurrentCulture, "BuyingCurrentlyNotPossibleDueInvalidRequest", "Buying currently not possible since the viewer request is invalid. You might have to relog."), ID);
                    }
                    else if(grp.SalePrice != 0 && EconomyService == null)
                    {
                        agent.SendAlertMessage(this.GetLanguageString(agent.CurrentCulture, "BuyingForAnyOtherPriceThanZeroIsNotPossible", "Buying for any other price than zero is not possible without economy system."), ID);
                    }
                    else
                    {
                        var assetids = new List<UUID>();
                        var items = new List<InventoryItem>();
                        bool foundNoTransfer = false;
                        AssetData newAsset;
                        switch (grp.SaleType)
                        {
                            case InventoryItem.SaleInfoData.SaleType.NoSale:
                            default:
                                continue;

                            case InventoryItem.SaleInfoData.SaleType.Original:
                            case InventoryItem.SaleInfoData.SaleType.Copy:
                                UUID assetID;
                                if (grp.Owner == agent.Owner)
                                {
                                    assetID = grp.OriginalAssetID;
                                    if (assetID == UUID.Zero)
                                    {
                                        newAsset = grp.Asset(XmlSerializationOptions.WriteXml2 | XmlSerializationOptions.WriteOwnerInfo);
                                        assetID = UUID.Random;
                                        newAsset.ID = assetID;
                                        AssetService.Store(newAsset);
                                        assetID = newAsset.ID;
                                        grp.OriginalAssetID = assetID;
                                    }
                                }
                                else
                                {
                                    assetID = grp.NextOwnerAssetID;
                                    if (assetID == UUID.Zero)
                                    {
                                        newAsset = grp.Asset(UUI.Unknown, XmlSerializationOptions.WriteXml2 | XmlSerializationOptions.AdjustForNextOwner);
                                        assetID = UUID.Random;
                                        newAsset.ID = assetID;
                                        AssetService.Store(newAsset);
                                        assetID = newAsset.ID;
                                        grp.NextOwnerAssetID = assetID;
                                    }
                                }
                                assetids.Add(assetID);

                                if(grp.SaleType == InventoryItem.SaleInfoData.SaleType.Original)
                                {
                                    Remove(grp);
                                }

                                bool foundNoCopy = false;
                                foreach (ObjectPart checkpart in grp.Values)
                                {
                                    if (!checkpart.CheckPermissions(checkpart.Owner, checkpart.Group, InventoryPermissionsMask.Transfer))
                                    {
                                        agent.SendAlertMessage(this.GetLanguageString(agent.CurrentCulture, "UnableToSellNoTransferItem", "Unable to sell no transfer item."), ID);
                                        foundNoTransfer = true;
                                        break;
                                    }
                                    if (grp.SaleType == InventoryItem.SaleInfoData.SaleType.Copy &&
                                        !checkpart.CheckPermissions(checkpart.Owner, checkpart.Group, InventoryPermissionsMask.Copy))
                                    {
                                        agent.SendAlertMessage(this.GetLanguageString(agent.CurrentCulture, "UnableToSellNoCopyItemAsACopy", "Unable to sell no copy item as a copy."), ID);
                                        foundNoCopy = true;
                                        break;
                                    }
                                    foreach (ObjectPartInventoryItem item in checkpart.Inventory.Values)
                                    {
                                        if (item.CheckPermissions(item.Owner, item.Group, InventoryPermissionsMask.Transfer))
                                        {
                                            agent.SendAlertMessage(this.GetLanguageString(agent.CurrentCulture, "UnableToSellNoTransferItem", "Unable to sell no transfer item."), ID);
                                            foundNoTransfer = true;
                                            break;
                                        }
                                    }
                                }
                                if (foundNoTransfer || foundNoCopy)
                                {
                                    continue;
                                }
                                var objectItem = new InventoryItem();
                                objectItem.Permissions.Base = agent.Owner.EqualsGrid(grp.Owner) ? grp.RootPart.NextOwnerMask : grp.RootPart.BaseMask;
                                objectItem.Permissions.EveryOne = grp.RootPart.EveryoneMask;
                                objectItem.Permissions.Group = InventoryPermissionsMask.None;
                                objectItem.Permissions.NextOwner = grp.RootPart.NextOwnerMask;
                                objectItem.Permissions.Current = objectItem.Permissions.Base;
                                objectItem.Name = grp.Name;
                                objectItem.Description = grp.Description;
                                objectItem.LastOwner = grp.Owner;
                                objectItem.Owner = agent.Owner;
                                objectItem.AssetID = assetID;
                                objectItem.AssetType = AssetType.Object;
                                objectItem.InventoryType = InventoryType.Object;
                                items.Add(objectItem);
                                break;

                            case InventoryItem.SaleInfoData.SaleType.Content:
                                foreach (ObjectPartInventoryItem item in part.Inventory.Values)
                                {
                                    if (!item.CheckPermissions(item.Owner, item.Group, InventoryPermissionsMask.Transfer))
                                    {
                                        agent.SendAlertMessage(this.GetLanguageString(agent.CurrentCulture, "UnableToSellNoTransferItem", "Unable to sell no transfer item."), ID);
                                        foundNoTransfer = true;
                                        break;
                                    }
                                }

                                if(foundNoTransfer)
                                {
                                    continue;
                                }

                                foreach (ObjectPartInventoryItem item in part.Inventory.Values)
                                {
                                    assetID = item.NextOwnerAssetID;
                                    if(assetID == UUID.Zero && !item.Owner.EqualsGrid(agent.Owner))
                                    {
                                        /* create next owner asset id */
                                        item.NextOwnerAssetID = AssetService.GenerateNextOwnerAsset(assetID);
                                    }
                                    var newItem = new InventoryItem(item);
                                    if(!item.Owner.EqualsGrid(agent.Owner))
                                    {
                                        newItem.AssetID = item.NextOwnerAssetID;
                                        newItem.LastOwner = item.Owner;
                                        newItem.Owner = agent.Owner;
                                        newItem.Permissions.Base = newItem.Permissions.NextOwner;
                                    }
                                    newItem.Permissions.Group = InventoryPermissionsMask.None;
                                    assetids.Add(newItem.AssetID);

                                    items.Add(newItem);
                                    if(!item.CheckPermissions(item.Owner, item.Group, InventoryPermissionsMask.Copy))
                                    {
                                        part.Inventory.Remove(item.ID);
                                    }
                                }
                                break;
                        }

                        if (grp.SalePrice == 0)
                        {
                            new ObjectBuyTransferItem(
                                agent,
                                this,
                                assetids,
                                items,
                                grp.SaleType == InventoryItem.SaleInfoData.SaleType.Content ? part.Name : string.Empty,
                                part.ID).QueueWorkItem();
                        }
                    }
                }
            }
        }

        public class ObjectBuyTransferItem : ObjectTransferItem
        {
            readonly UUID m_SellingPrimitiveID;

            public ObjectBuyTransferItem(
                IAgent agent,
                SceneInterface scene,
                List<UUID> assetids,
                List<InventoryItem> items,
                string destinationFolder,
                UUID sellingPrimitiveID)
                : base(agent, scene, assetids, items, destinationFolder)
            {
                m_SellingPrimitiveID = sellingPrimitiveID;
            }

            public override void AssetTransferComplete()
            {
                base.AssetTransferComplete();

                ObjectPart part;
                SceneInterface scene;
                UUI sellOwner;

                if (TryGetScene(m_SceneID, out scene) &&
                    scene.Primitives.TryGetValue(m_SellingPrimitiveID, out part))
                {
                    sellOwner = part.Owner;
                    foreach(ObjectBuyListen lt in scene.m_ObjectBuyListeners.Values)
                    {
                        ObjectPartInventoryItem item;
                        if(scene.Primitives.TryGetValue(lt.PrimitiveID, out part) &&
                            part.Inventory.TryGetValue(lt.ItemID, out item) &&
                            part.Owner.EqualsGrid(sellOwner))
                        {
                            ScriptInstance script = item.ScriptInstance;
                            if (script != null)
                            {
                                ItemSoldEvent ev = new ItemSoldEvent();
                                ev.Agent = m_DestinationAgent;
                                ev.ObjectID = m_SellingPrimitiveID;
                                ev.ObjectName = part.Name;
                                script.PostEvent(ev);
                            }
                        }
                    }
                }
            }
        }

        [PacketHandler(MessageType.BuyObjectInventory)]
        public void HandleBuyObjectInventory(Message m)
        {
            var req = (BuyObjectInventory)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }
    }
}

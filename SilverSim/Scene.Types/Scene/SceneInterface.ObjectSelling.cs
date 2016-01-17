// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Transfer;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Object;
using System.Collections.Generic;
using System.Threading;

namespace SilverSim.Scene.Types.Scene
{
    public partial class SceneInterface
    {

        [PacketHandler(MessageType.ObjectSaleInfo)]
        public void HandleObjectSaleInfo(Message m)
        {
            ObjectSaleInfo req = (ObjectSaleInfo)m;
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

            using (ObjectPropertiesSendHandler propHandler = new ObjectPropertiesSendHandler(agent, ID))
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

        [PacketHandler(MessageType.ObjectBuy)]
        public void HandleObjectBuy(Message m)
        {
            ObjectBuy req = (ObjectBuy)m;
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
            ObjectBuy req = (ObjectBuy)o;
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
                        List<UUID> assetids = new List<UUID>();
                        List<InventoryItem> items = new List<InventoryItem>();
                        UUID assetID;
                        bool foundNoTransfer = false;
                        AssetData newAsset;
                        if (grp.NextOwnerAssetID == UUID.Zero && !grp.Owner.EqualsGrid(agent.Owner))
                        {
                            AssetService.GenerateNextOwnerAssets(grp);
                        }
                        switch (grp.SaleType)
                        {
                            case InventoryItem.SaleInfoData.SaleType.NoSale:
                                continue;

                            case InventoryItem.SaleInfoData.SaleType.Original:
                            case InventoryItem.SaleInfoData.SaleType.Copy:
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
                                InventoryItem objectItem = new InventoryItem();
                                objectItem.Permissions.Base = agent.Owner.EqualsGrid(grp.Owner) ? grp.RootPart.NextOwnerMask : grp.RootPart.BaseMask;
                                objectItem.Permissions.EveryOne = grp.RootPart.EveryoneMask;
                                objectItem.Permissions.Group = InventoryPermissionsMask.None;
                                objectItem.Permissions.NextOwner = grp.RootPart.NextOwnerMask;
                                objectItem.Permissions.Current = objectItem.Permissions.Base;
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
                                    InventoryItem newItem = new InventoryItem(item);
                                    if(!item.Owner.EqualsGrid(agent.Owner))
                                    {
                                        newItem.AssetID = item.NextOwnerAssetID;
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
                            new ObjectTransferItem(
                                agent,
                                this,
                                assetids,
                                items,
                                grp.SaleType == InventoryItem.SaleInfoData.SaleType.Content ? part.Name : string.Empty).QueueWorkItem();
                        }
                    }
                }
            }
        }

        [PacketHandler(MessageType.BuyObjectInventory)]
        public void HandleBuyObjectInventory(Message m)
        {
            BuyObjectInventory req = (BuyObjectInventory)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }
    }
}

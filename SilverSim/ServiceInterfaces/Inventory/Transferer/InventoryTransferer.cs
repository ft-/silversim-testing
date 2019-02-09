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

using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Asset.Transferer;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.IM;
using SilverSim.Types.Inventory;
using SilverSim.Types.ServerURIs;
using System;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.Inventory.Transferer
{
    public static class InventoryTransferer
    {
        public static bool StartTransferLocal(
            UUID transactionID,
            UGUI dstAgent,
            IReadOnlyList<IUserAgentServicePlugin> userAgentServicePlugins,
            IReadOnlyList<IInventoryServicePlugin> inventoryServicePlugins,
            IReadOnlyList<IAssetServicePlugin> assetServicePlugins,
            UGUIWithName srcAgent,
            InventoryServiceInterface srcInventoryService,
            AssetServiceInterface srcAssetService,
            AssetType givenAssetType,
            UUID givenInventoryID,
            IMServiceInterface imService,
            Action<UUID> inventoryCreate = null) => StartTransfer(
                transactionID,
                dstAgent,
                userAgentServicePlugins,
                inventoryServicePlugins,
                assetServicePlugins,
                srcAgent,
                null,
                srcInventoryService,
                srcAssetService,
                givenAssetType,
                givenInventoryID,
                imService,
                inventoryCreate);

        public static bool StartTransfer(
            UUID transactionID,
            UGUI dstAgent,
            IReadOnlyList<IUserAgentServicePlugin> userAgentServicePlugins,
            IReadOnlyList<IInventoryServicePlugin> inventoryServicePlugins,
            IReadOnlyList<IAssetServicePlugin> assetServicePlugins,
            UGUIWithName srcAgent,
            UserAgentServiceInterface srcUserAgentService,
            InventoryServiceInterface srcInventoryService,
            AssetServiceInterface srcAssetService,
            AssetType givenAssetType,
            UUID givenInventoryID,
            IMServiceInterface imService,
            Action<UUID> inventoryCreate = null)
        {
            InventoryServiceInterface dstInventoryService = null;
            AssetServiceInterface dstAssetService = null;
            UserAgentServiceInterface dstUserAgentService = null;

            var homeUri = dstAgent.HomeURI.ToString();
            var heloheaders = ServicePluginHelo.HeloRequest(homeUri);
            foreach (IUserAgentServicePlugin userAgentPlugin in userAgentServicePlugins)
            {
                if (userAgentPlugin.IsProtocolSupported(homeUri, heloheaders))
                {
                    dstUserAgentService = userAgentPlugin.Instantiate(homeUri);
                }
            }

            if (dstUserAgentService == null)
            {
                return false;
            }

            ServerURIs uris = dstUserAgentService.GetServerURLs(dstAgent);

            heloheaders = ServicePluginHelo.HeloRequest(uris.AssetServerURI);
            foreach (IAssetServicePlugin assetPlugin in assetServicePlugins)
            {
                if (assetPlugin.IsProtocolSupported(homeUri, heloheaders))
                {
                    dstAssetService = assetPlugin.Instantiate(homeUri);
                }
            }
            if (dstAssetService == null)
            {
                return false;
            }

            heloheaders = ServicePluginHelo.HeloRequest(uris.InventoryServerURI);
            foreach (IInventoryServicePlugin inventoryPlugin in inventoryServicePlugins)
            {
                if (inventoryPlugin.IsProtocolSupported(homeUri, heloheaders))
                {
                    dstInventoryService = inventoryPlugin.Instantiate(homeUri);
                }
            }

            if (dstInventoryService == null)
            {
                return false;
            }

            StartTransfer(
                transactionID,
                dstAgent,
                dstUserAgentService,
                dstInventoryService,
                dstAssetService,
                srcAgent,
                srcUserAgentService,
                srcInventoryService,
                srcAssetService,
                givenAssetType,
                givenInventoryID,
                imService,
                inventoryCreate);
            return true;
        }

        public static void StartTransferLocal(
            UUID transactionID,
            UGUI dstAgent,
            UserAgentServiceInterface dstUserAgentService,
            InventoryServiceInterface dstInventoryService,
            AssetServiceInterface dstAssetService,
            UGUIWithName srcAgent,
            InventoryServiceInterface srcInventoryService,
            AssetServiceInterface srcAssetService,
            AssetType givenAssetType,
            UUID givenInventoryID,
            IMServiceInterface imService,
            Action<UUID> inventoryCreate = null) => StartTransfer(
                transactionID,
                dstAgent,
                dstUserAgentService,
                dstInventoryService,
                dstAssetService,
                srcAgent,
                null,
                srcInventoryService,
                srcAssetService,
                givenAssetType,
                givenInventoryID,
                imService,
                inventoryCreate);

        public static void StartTransfer(
            UUID transactionID,
            UGUI dstAgent,
            UserAgentServiceInterface dstUserAgentService,
            InventoryServiceInterface dstInventoryService,
            AssetServiceInterface dstAssetService,
            UGUIWithName srcAgent,
            UserAgentServiceInterface srcUserAgentService,
            InventoryServiceInterface srcInventoryService,
            AssetServiceInterface srcAssetService,
            AssetType givenAssetType,
            UUID givenInventoryID,
            IMServiceInterface imService,
            Action<UUID> inventoryCreate = null)
        {
            if(dstUserAgentService == null)
            {
                throw new ArgumentNullException(nameof(dstUserAgentService));
            }

            if (dstInventoryService == null)
            {
                throw new ArgumentNullException(nameof(dstInventoryService));
            }

            if (dstAssetService == null)
            {
                throw new ArgumentNullException(nameof(dstAssetService));
            }

            if (srcInventoryService == null)
            {
                throw new ArgumentNullException(nameof(srcInventoryService));
            }

            if (srcAssetService == null)
            {
                throw new ArgumentNullException(nameof(srcAssetService));
            }

            if (imService == null)
            {
                throw new ArgumentNullException(nameof(imService));
            }

            if (srcUserAgentService != null && srcUserAgentService.SupportsInventoryTransfer)
            {
                srcUserAgentService.InitiateInventoryTransfer(dstAgent, srcAgent, givenAssetType, givenInventoryID, transactionID);
                return;
            }

            if (givenAssetType == AssetType.Folder)
            {
                InventoryFolder folder;
                if (srcInventoryService.Folder.TryGetValue(srcAgent.ID, givenInventoryID, out folder))
                {
                    /* this has a whole bunch of such entries, but later only the first 17 bytes are kept and adjusted */
                    var assetids = new List<UUID>();
                    TransferInventoryFolder transferFolder = GetTree(srcInventoryService, folder, assetids);
                    if (transferFolder != null)
                    {
                        new InventoryTransferWorkItem(
                            transactionID,
                            dstAgent,
                            dstUserAgentService,
                            dstInventoryService,
                            dstAssetService,
                            srcAgent,
                            srcInventoryService,
                            srcAssetService,
                            assetids,
                            transferFolder,
                            imService,
                            inventoryCreate).QueueWorkItem();
                    }
                }
            }
            else
            {
                /* just some item */
                InventoryItem item;
                if (srcInventoryService.Item.TryGetValue(srcAgent.ID, givenInventoryID, out item) && 
                    item.AssetType != AssetType.Link && item.AssetType != AssetType.LinkFolder &&
                    item.CheckPermissions(srcAgent, UGI.Unknown, InventoryPermissionsMask.Transfer))
                {
                    new InventoryTransferWorkItem(
                        transactionID,
                        dstAgent,
                        dstUserAgentService,
                        dstInventoryService,
                        dstAssetService,
                        srcAgent,
                        srcInventoryService,
                        dstAssetService,
                        item.AssetID,
                        item,
                        imService,
                        inventoryCreate).QueueWorkItem();
                }
            }
        }

        private static TransferInventoryFolder GetTree(InventoryServiceInterface inventoryService, InventoryFolder baseFolder, List<UUID> assetids)
        {
            TransferInventoryFolder catBase = new TransferInventoryFolder(baseFolder);

            List<TransferInventoryFolder> collect = new List<TransferInventoryFolder>
            {
                catBase
            };
            while (collect.Count != 0)
            {
                InventoryFolderContent content;
                TransferInventoryFolder cat = collect[0];
                if (inventoryService.Folder.Content.TryGetValue(cat.Owner.ID, cat.ID, out content))
                {
                    foreach (InventoryItem item in content.Items)
                    {
                        if (item.AssetType != AssetType.Link && item.AssetType != AssetType.LinkFolder)
                        {
                            if (!item.CheckPermissions(cat.Owner, UGI.Unknown, InventoryPermissionsMask.Transfer))
                            {
                                return null;
                            }
                            assetids.Add(item.AssetID);
                            cat.Items.Add(item);
                        }
                    }

                    foreach (InventoryFolder folder in content.Folders)
                    {
                        var childFolder = new TransferInventoryFolder(folder);
                        collect.Add(childFolder);
                        cat.Folders.Add(childFolder);
                    }
                }

                cat.ID = UUID.Random;
                collect.RemoveAt(0);
            }

            return catBase;
        }

        public class TransferInventoryFolder : InventoryFolder
        {
            public readonly List<InventoryItem> Items = new List<InventoryItem>();
            public readonly List<TransferInventoryFolder> Folders = new List<TransferInventoryFolder>();

            public TransferInventoryFolder(InventoryFolder folder) : base(folder)
            {
            }
        }

        public class InventoryTransferWorkItem : AssetTransferWorkItem
        {
            private readonly UserAgentServiceInterface m_DstUserAgentService;
            private readonly InventoryServiceInterface m_DstInventoryService;
            private readonly InventoryServiceInterface m_SrcInventoryService;
            private readonly UGUI m_DestinationAgent;
            private readonly UGUIWithName m_SrcAgent;
            private readonly TransferInventoryFolder m_InventoryTree;
            private readonly InventoryItem m_Item;
            private readonly UUID m_TransactionID;
            private readonly IMServiceInterface m_IMService;
            private readonly Action<UUID> m_InventoryCreate;

            public InventoryTransferWorkItem(
                UUID transactionID,
                UGUI dstAgent,
                UserAgentServiceInterface dstUserAgentService,
                InventoryServiceInterface dstInventoryService,
                AssetServiceInterface dstAssetService,
                UGUIWithName srcAgent,
                InventoryServiceInterface srcInventoryService,
                AssetServiceInterface srcAssetService,
                List<UUID> assetids,
                TransferInventoryFolder inventoryTree,
                IMServiceInterface imService,
                Action<UUID> inventoryCreate)
                : base(dstAssetService, srcAssetService, assetids, ReferenceSource.Source)
            {
                m_TransactionID = transactionID;
                m_DstUserAgentService = dstUserAgentService;
                m_DstInventoryService = dstInventoryService;
                m_SrcInventoryService = srcInventoryService;
                m_DestinationAgent = dstAgent;
                m_InventoryTree = inventoryTree;
                m_SrcAgent = srcAgent;
                m_IMService = imService;
                m_InventoryCreate = inventoryCreate;
            }

            public InventoryTransferWorkItem(
                UUID transactionID,
                UGUI dstAgent,
                UserAgentServiceInterface dstUserAgentService,
                InventoryServiceInterface dstInventoryService,
                AssetServiceInterface dstAssetService,
                UGUIWithName srcAgent,
                InventoryServiceInterface srcInventoryService,
                AssetServiceInterface srcAssetService,
                UUID assetid,
                InventoryItem item,
                IMServiceInterface imService,
                Action<UUID> inventoryCreate)
                : base(dstAssetService, srcAssetService, assetid, ReferenceSource.Source)
            {
                m_TransactionID = transactionID;
                m_DstUserAgentService = dstUserAgentService;
                m_DstInventoryService = dstInventoryService;
                m_SrcInventoryService = srcInventoryService;
                m_DestinationAgent = dstAgent;
                m_Item = item;
                m_SrcAgent = srcAgent;
                m_IMService = imService;
                m_InventoryCreate = inventoryCreate;
            }

            public override void AssetTransferComplete()
            {
                AssetType assetType = AssetType.Folder;
                UUID givenID;
                string givenName;
                if (m_InventoryTree != null)
                {
                    InventoryFolder folder;
                    if (!m_DstInventoryService.Folder.TryGetValue(m_DestinationAgent.ID, AssetType.RootFolder, out folder))
                    {
                        return;
                    }
                    var folderCreator = new List<TransferInventoryFolder>
                    {
                        m_InventoryTree
                    };
                    m_InventoryTree.ParentFolderID = folder.ID;
                    m_InventoryTree.Owner = m_DestinationAgent;
                    m_InventoryTree.Version = 1;
                    givenID = m_InventoryTree.ID;
                    givenName = m_InventoryTree.Name;
                    List<UUID> noCopyItems = new List<UUID>();
                    while (folderCreator.Count != 0)
                    {
                        TransferInventoryFolder transferFolder = folderCreator[0];
                        m_DstInventoryService.Folder.Add(transferFolder);
                        foreach (InventoryItem item in transferFolder.Items)
                        {
                            if (!item.CheckPermissions(m_SrcAgent, UGI.Unknown, InventoryPermissionsMask.Copy))
                            {
                                noCopyItems.Add(item.ID);
                            }
                            item.SetNewID(UUID.Random);
                            item.LastOwner = item.Owner;
                            item.Owner = m_DestinationAgent;
                            item.AdjustToNextOwner();
                            m_DstInventoryService.Item.Add(item);
                        }
                        foreach (TransferInventoryFolder childfolder in transferFolder.Folders)
                        {
                            childfolder.Owner = m_DestinationAgent;
                            childfolder.ParentFolderID = transferFolder.ID;
                            childfolder.Version = 1;
                            folderCreator.Add(childfolder);
                        }
                    }

                    if (noCopyItems.Count != 0)
                    {
                        m_SrcInventoryService.Item.Delete(m_SrcAgent.ID, noCopyItems);
                    }
                }
                else
                {
                    InventoryFolder folder;
                    if (!m_DstInventoryService.Folder.TryGetValue(m_DestinationAgent.ID, m_Item.AssetType, out folder) &&
                        !m_DstInventoryService.Folder.TryGetValue(m_DestinationAgent.ID, AssetType.RootFolder, out folder))
                    {
                        return;
                    }
                    UUID oldItemID = m_Item.ID;
                    bool noCopyItem = !m_Item.CheckPermissions(m_SrcAgent, UGI.Unknown, InventoryPermissionsMask.Copy);
                    m_Item.SetNewID(UUID.Random);
                    assetType = m_Item.AssetType;
                    m_Item.AdjustToNextOwner();
                    m_Item.LastOwner = m_Item.Owner;
                    m_Item.Owner = m_DestinationAgent;
                    m_Item.ParentFolderID = folder.ID;
                    givenName = m_Item.Name;
                    m_DstInventoryService.Item.Add(m_Item);
                    givenID = m_Item.ID;
                    if (noCopyItem)
                    {
                        m_SrcInventoryService.Item.Delete(m_SrcAgent.ID, oldItemID);
                    }
                }

                var binbuck = new byte[17];
                binbuck[0] = (byte)assetType;
                givenID.ToBytes(binbuck, 1);

                m_InventoryCreate?.Invoke(givenID);

                var im = new GridInstantMessage
                {
                    ToAgent = m_DestinationAgent,
                    FromAgent = m_SrcAgent,
                    Message = givenName,
                    IMSessionID = m_DstUserAgentService.RequiresInventoryIDAsIMSessionID ? givenID : m_TransactionID,
                    Dialog = GridInstantMessageDialog.InventoryOffered,
                    BinaryBucket = binbuck,
                };
                m_IMService.Send(im);
            }

            public override void AssetTransferFailed(Exception e)
            {
                /* intentionally left empty for now */
            }
        }
    }
}

// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Transfer;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;

namespace SilverSim.Scene.Types.Scene
{
    public partial class SceneInterface
    {
        sealed class ObjectTransferItem : AssetTransferWorkItem
        {
            readonly InventoryServiceInterface m_InventoryService;
            readonly UUI m_DestinationAgent;
            readonly UUID m_SceneID;
            readonly List<InventoryItem> m_Items;
            readonly string m_DestinationFolder;
            readonly TryGetSceneDelegate TryGetScene;

            public ObjectTransferItem(
                IAgent agent, 
                SceneInterface scene, 
                UUID assetid,
                List<InventoryItem> items,
                string destinationFolder = "")
                : base(agent.AssetService, scene.AssetService, assetid, ReferenceSource.Source)
            {
                m_InventoryService = agent.InventoryService;
                m_DestinationAgent = agent.Owner;
                m_SceneID = scene.ID;
                m_Items = items;
                m_DestinationFolder = destinationFolder;
                TryGetScene = scene.TryGetScene;
            }

            public ObjectTransferItem(
                IAgent agent, 
                SceneInterface scene, 
                List<UUID> assetids, 
                List<InventoryItem> items, 
                string destinationFolder = "")
                : base(agent.AssetService, scene.AssetService, assetids, ReferenceSource.Source)
            {
                m_InventoryService = agent.InventoryService;
                m_DestinationAgent = agent.Owner;
                m_SceneID = scene.ID;
                m_Items = items;
                m_DestinationFolder = destinationFolder;
                TryGetScene = scene.TryGetScene;
            }

            public ObjectTransferItem(
                InventoryServiceInterface inventoryService,
                AssetServiceInterface assetService,
                UUI agentOwner,
                SceneInterface scene,
                List<UUID> assetids,
                List<InventoryItem> items,
                string destinationFolder = "")
                : base(assetService, scene.AssetService, assetids, ReferenceSource.Source)
            {
                m_InventoryService = inventoryService;
                m_DestinationAgent = agentOwner;
                m_SceneID = scene.ID;
                m_Items = items;
                m_DestinationFolder = destinationFolder;
                TryGetScene = scene.TryGetScene;
            }

            public override void AssetTransferComplete()
            {
                InventoryFolder folder;
                if(m_DestinationFolder.Length == 0)
                {
                    if (!m_InventoryService.Folder.TryGetValue(m_DestinationAgent.ID, AssetType.Object, out folder))
                    {
                        return;
                    }
                }
                else
                {
                    if(!m_InventoryService.Folder.TryGetValue(m_DestinationAgent.ID, AssetType.RootFolder, out folder))
                    {
                        return;
                    }
                    UUID rootFolderID = folder.ID;
                    folder = new InventoryFolder();
                    folder.Owner = m_DestinationAgent;
                    folder.ParentFolderID = rootFolderID;
                    folder.InventoryType = InventoryType.Unknown;
                    folder.Version = 1;
                    folder.Name = m_DestinationFolder;
                    folder.ID = UUID.Random;
                    m_InventoryService.Folder.Add(folder);
                }
                
                foreach(ObjectPartInventoryItem sellItem in m_Items)
                {
                    InventoryItem item = new InventoryItem(sellItem);
                    item.Owner = m_DestinationAgent;
                    item.ParentFolderID = folder.ID;
                    m_InventoryService.Item.Add(item);
                }
            }

            public override void AssetTransferFailed(Exception e)
            {
                SceneInterface scene;
                IAgent agent;
                if(!TryGetScene(m_DestinationAgent.ID, out scene) &&
                    scene.Agents.TryGetValue(m_DestinationAgent.ID, out agent))
                {

                }
            }
        }
    }
}

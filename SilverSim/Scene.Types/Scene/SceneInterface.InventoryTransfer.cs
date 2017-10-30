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
using SilverSim.Scene.Types.Transfer;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using SilverSim.Viewer.Messages.Inventory;
using System;
using System.Collections.Generic;

namespace SilverSim.Scene.Types.Scene
{
    public partial class SceneInterface
    {
        public class AddToObjectTransferItem : AssetTransferWorkItem
        {
            private readonly ObjectPart m_Part;
            protected readonly UUI m_SourceAgent;
            protected readonly UUID m_SceneID;
            private readonly ObjectPartInventoryItem m_Item;
            protected readonly TryGetSceneDelegate TryGetScene;

            public AddToObjectTransferItem(
                IAgent agent,
                SceneInterface scene,
                UUID assetid,
                ObjectPart part,
                ObjectPartInventoryItem item)
                : base(scene.AssetService, agent.AssetService, assetid, ReferenceSource.Source)
            {
                m_Part = part;
                m_Item = item;
                m_Part = part;
                m_SceneID = scene.ID;
            }

            public override void AssetTransferComplete()
            {
                m_Part.Inventory.Add(m_Item);
            }

            public override void AssetTransferFailed(Exception e)
            {
                SceneInterface scene;
                IAgent agent;
                if (!TryGetScene(m_SourceAgent.ID, out scene) &&
                    scene.Agents.TryGetValue(m_SourceAgent.ID, out agent))
                {

                }
            }
        }

        public class ObjectTransferItem : AssetTransferWorkItem
        {
            private readonly InventoryServiceInterface m_InventoryService;
            protected readonly UUI m_DestinationAgent;
            protected readonly UUID m_SceneID;
            private readonly List<InventoryItem> m_Items;
            private readonly string m_DestinationFolder = string.Empty;
            private readonly UUID m_DestinationFolderID = UUID.Zero;
            protected readonly TryGetSceneDelegate TryGetScene;
            private readonly AssetType m_DestinationFolderType = AssetType.Object;

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
                UUID assetid,
                List<InventoryItem> items,
                UUID destinationFolderID,
                AssetType destinationFolderType)
                : base(agent.AssetService, scene.AssetService, assetid, ReferenceSource.Source)
            {
                m_InventoryService = agent.InventoryService;
                m_DestinationAgent = agent.Owner;
                m_SceneID = scene.ID;
                m_Items = items;
                m_DestinationFolderID = destinationFolderID;
                m_DestinationFolderType = destinationFolderType;
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
                IAgent agent,
                SceneInterface scene,
                List<UUID> assetids,
                List<InventoryItem> items,
                AssetType destinationFolderType)
                : base(agent.AssetService, scene.AssetService, assetids, ReferenceSource.Source)
            {
                m_InventoryService = agent.InventoryService;
                m_DestinationAgent = agent.Owner;
                m_SceneID = scene.ID;
                m_Items = items;
                m_DestinationFolderType = destinationFolderType;
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

            public ObjectTransferItem(
                InventoryServiceInterface inventoryService,
                AssetServiceInterface assetService,
                UUI agentOwner,
                SceneInterface scene,
                List<UUID> assetids,
                List<InventoryItem> items,
                AssetType destinationFolderType)
                : base(assetService, scene.AssetService, assetids, ReferenceSource.Source)
            {
                m_InventoryService = inventoryService;
                m_DestinationAgent = agentOwner;
                m_SceneID = scene.ID;
                m_Items = items;
                m_DestinationFolderType = destinationFolderType;
                TryGetScene = scene.TryGetScene;
            }

            public override void AssetTransferComplete()
            {
                InventoryFolder folder;
                SceneInterface scene = null;
                IAgent agent = null;
                if (!TryGetScene(m_SceneID, out scene) ||
                    !scene.Agents.TryGetValue(m_DestinationAgent.ID, out agent))
                {
                    agent = null;
                }

                if (m_DestinationFolder.Length == 0)
                {
                    if(!(m_DestinationFolderID != UUID.Zero && m_InventoryService.Folder.TryGetValue(m_DestinationAgent.ID, m_DestinationFolderID, out folder)) &&
                        !m_InventoryService.Folder.TryGetValue(m_DestinationAgent.ID, m_DestinationFolderType, out folder))
                    {
                        return;
                    }
                }
                else
                {
                    if(!m_InventoryService.Folder.TryGetValue(m_DestinationAgent.ID, m_DestinationFolderType, out folder))
                    {
                        return;
                    }
                    var rootFolderID = folder.ID;
                    folder = new InventoryFolder
                    {
                        Owner = m_DestinationAgent,
                        ParentFolderID = rootFolderID,
                        DefaultType = AssetType.Unknown,
                        Version = 1,
                        Name = m_DestinationFolder,
                        ID = UUID.Random
                    };
                    m_InventoryService.Folder.Add(folder);

                    if (agent != null)
                    {
                        var msg = new BulkUpdateInventory
                        {
                            AgentID = m_DestinationAgent.ID,
                            TransactionID = UUID.Zero
                        };
                        msg.AddInventoryFolder(folder);
                        agent.SendMessageAlways(msg, m_SceneID);
                    }
                }

                foreach(var sellItem in m_Items)
                {
                    var item = new InventoryItem(UUID.Random, sellItem);
                    item.LastOwner = item.Owner;
                    item.Owner = m_DestinationAgent;
                    item.ParentFolderID = folder.ID;
                    item.IsGroupOwned = false;
                    m_InventoryService.Item.Add(item);
                    if (agent != null)
                    {
                        var msg = new UpdateCreateInventoryItem
                        {
                            AgentID = m_DestinationAgent.ID,
                            SimApproved = true
                        };
                        msg.AddItem(item, 0);
                        agent.SendMessageAlways(msg, m_SceneID);
                    }
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

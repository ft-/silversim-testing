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

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.ServiceInterfaces.Inventory.Transferer;
using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.IM;
using SilverSim.Types.Inventory;
using SilverSim.Viewer.Core;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.IM;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.Viewer.InventoryTransfer
{
    [Description("Viewer Inventory Transfer Handler")]
    [PluginName("ViewerInventoryTransfer")]
    public sealed class ViewerInventoryTransfer : IPlugin, IPacketHandlerExtender
    {
        private SceneList m_Scenes;
        private List<IUserAgentServicePlugin> m_UserAgentServicePlugins;
        private List<IAssetServicePlugin> m_AssetServicePlugins;
        private List<IInventoryServicePlugin> m_InventoryServicePlugins;
        private IMServiceInterface m_IMService;
        private readonly string m_IMServiceName;

        public ViewerInventoryTransfer(IConfig ownSection)
        {
            m_IMServiceName = ownSection.GetString("IMService", "IMService");
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_Scenes = loader.Scenes;
            m_UserAgentServicePlugins = loader.GetServicesByValue<IUserAgentServicePlugin>();
            m_AssetServicePlugins = loader.GetServicesByValue<IAssetServicePlugin>();
            m_InventoryServicePlugins = loader.GetServicesByValue<IInventoryServicePlugin>();
            m_IMService = loader.GetService<IMServiceInterface>(m_IMServiceName);
        }

        [IMMessageHandler(GridInstantMessageDialog.InventoryAccepted)]
        public void HandleInventoryAccepted(ViewerAgent dstAgent, AgentCircuit circuit, Message m)
        {
            var im = (ImprovedInstantMessage)m;

            UserAgentServiceInterface userAgentService = dstAgent.UserAgentService;
            if(userAgentService == null)
            {
                return;
            }

            if(userAgentService.SupportsInventoryTransfer)
            {
                userAgentService.AcceptInventoryTransfer(dstAgent.ID, im.ID);
            }
            else
            {
                SceneInterface scene = circuit.Scene;
                if (scene == null)
                {
                    return;
                }

                UGUI toAgent;
                if (!scene.AvatarNameService.TryGetValue(im.ToAgentID, out toAgent))
                {
                    /* pass the unresolved here */
                    toAgent = new UGUI(im.ToAgentID);
                }

                m_IMService.Send(new GridInstantMessage
                {
                    ToAgent = toAgent,
                    FromAgent = dstAgent.NamedOwner,
                    Message = string.Empty,
                    IMSessionID = im.ID,
                    Dialog = GridInstantMessageDialog.InventoryAccepted
                });
            }
        }

        [IMMessageHandler(GridInstantMessageDialog.InventoryDeclined)]
        public void HandleInventoryDeclined(ViewerAgent dstAgent, AgentCircuit circuit, Message m)
        {
            var im = (ImprovedInstantMessage)m;

            UserAgentServiceInterface userAgentService = dstAgent.UserAgentService;
            InventoryServiceInterface inventorySerice = dstAgent.InventoryService;
            if (userAgentService == null)
            {
                return;
            }

            if (userAgentService.SupportsInventoryTransfer)
            {
                userAgentService.DeclineInventoryTransfer(dstAgent.ID, im.ID);
            }
            else
            {
                SceneInterface scene = circuit.Scene;
                if (scene == null)
                {
                    return;
                }

                UGUI toAgent;
                if (!scene.AvatarNameService.TryGetValue(im.ToAgentID, out toAgent))
                {
                    /* pass the unresolved here */
                    toAgent = new UGUI(im.ToAgentID);
                }

                InventoryFolder trashFolder;
                if (userAgentService.RequiresInventoryIDAsIMSessionID &&
                    inventorySerice != null &&
                    inventorySerice.Folder.TryGetValue(dstAgent.ID, AssetType.TrashFolder, out trashFolder))
                {
                    if(inventorySerice.Item.ContainsKey(dstAgent.ID, im.ID))
                    {
                        inventorySerice.Item.Move(dstAgent.ID, im.ID, trashFolder.ID);
                    }
                    else
                    {
                        inventorySerice.Folder.Move(dstAgent.ID, im.ID, trashFolder.ID);
                    }
                }

                m_IMService.Send(new GridInstantMessage
                {
                    ToAgent = toAgent,
                    FromAgent = dstAgent.NamedOwner,
                    Message = string.Empty,
                    IMSessionID = im.ID,
                    Dialog = GridInstantMessageDialog.InventoryDeclined
                });
            }
        }

        [IMMessageHandler(GridInstantMessageDialog.InventoryOffered)]
        public void HandleInventoryOffered(ViewerAgent srcAgent, AgentCircuit circuit, Message m)
        {
            var im = (ImprovedInstantMessage)m;

            UGUI dstAgent;
            SceneInterface scene = circuit.Scene;
            if (scene == null)
            {
                return;
            }

            if (im.BinaryBucket.Length < 17)
            {
                return;
            }

            AssetType type = (AssetType)im.BinaryBucket[0];
            UUID inventoryId = new UUID(im.BinaryBucket, 1);

            if (!scene.AvatarNameService.TryGetValue(im.ToAgentID, out dstAgent))
            {
                /* pass the unresolved here */
                dstAgent = new UGUI(im.ToAgentID);
            }

            InventoryTransferer.StartTransfer(
                im.ID,
                dstAgent,
                m_UserAgentServicePlugins,
                m_InventoryServicePlugins,
                m_AssetServicePlugins,
                srcAgent.NamedOwner,
                srcAgent.UserAgentService,
                srcAgent.InventoryService,
                srcAgent.AssetService,
                type,
                inventoryId,
                m_IMService);
        }
    }
}

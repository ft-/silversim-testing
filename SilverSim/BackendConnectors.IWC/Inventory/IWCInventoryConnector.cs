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

using log4net;
using Nini.Config;
using SilverSim.BackendConnectors.IWC.Common;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;

namespace SilverSim.BackendConnectors.IWC.Inventory
{
    #region Service Implementation
    public class IWCInventoryConnector : InventoryServiceInterface, IPlugin
    {
        private static readonly ILog m_Log = LogManager.GetLogger("IWC INVENTORY");

        private string m_InventoryURI;
        private IWCInventoryFolderConnector m_FolderService;
        private IWCInventoryItemConnector m_ItemService;
        private GroupsServiceInterface m_GroupsService;
        private int m_TimeoutMs = 20000;

        #region Constructor
        public IWCInventoryConnector(string uri)
        {
            m_InventoryURI = uri;
            m_ItemService = new IWCInventoryItemConnector(uri, null);
            m_ItemService.TimeoutMs = m_TimeoutMs;
            m_FolderService = new IWCInventoryFolderConnector(uri, null);
            m_FolderService.TimeoutMs = m_TimeoutMs;
        }

        public IWCInventoryConnector(string uri, GroupsServiceInterface groupsService)
        {
            m_GroupsService = groupsService;
            m_InventoryURI = uri;
            m_ItemService = new IWCInventoryItemConnector(uri, m_GroupsService);
            m_ItemService.TimeoutMs = m_TimeoutMs;
            m_FolderService = new IWCInventoryFolderConnector(uri, m_GroupsService);
            m_FolderService.TimeoutMs = m_TimeoutMs;
        }

        public void Startup(ConfigurationLoader loader)
        {

        }
        #endregion

        #region Accessors
        public int TimeoutMs
        {
            get
            {
                return m_TimeoutMs;
            }
            set
            {
                m_TimeoutMs = value;
                m_FolderService.TimeoutMs = value;
                m_ItemService.TimeoutMs = value;
            }
        }

        public override InventoryFolderServiceInterface Folder
        {
            get
            {
                return m_FolderService;
            }
        }

        public override InventoryItemServiceInterface Item
        {
            get
            {
                return m_ItemService;
            }
        }

        public override List<InventoryItem> getActiveGestures(UUID PrincipalID)
        {
            throw new NotImplementedException();
#if NOT_IMPLEMENTED
            List<InventoryItem> item = new List<InventoryItem>();
            Dictionary<string, string> post = new Dictionary<string, string>();
            post["RequestMethod"] = "GetUser";
            post["UserID"] = PrincipalID;

            Map res = SimianGrid.PostToService(m_InventoryURI, m_InventoryCapability, post, TimeoutMs);
            if (res["Success"].AsBoolean && res.ContainsKey("Gestures") && res["Gestures"] is AnArray)
            {
                AnArray gestures = (AnArray)res["Gestures"];
                foreach(IValue v in gestures)
                {
                    try
                    {
                        item.Add(Item[PrincipalID, v.AsUUID]);
                    }
                    catch
                    {

                    }
                }
            }
            throw new InventoryInaccessible();
#endif
        }
        #endregion
    }
    #endregion


    #region Factory
    [PluginName("Inventory")]
    public class IWCInventoryConnectorFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("IWC INVENTORY CONNECTOR");
        public IWCInventoryConnectorFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            if (!ownSection.Contains("URI"))
            {
                m_Log.FatalFormat("Missing 'URI' in section {0}", ownSection.Name);
                throw new ConfigurationLoader.ConfigurationError();
            }
            return new IWCInventoryConnector(ownSection.GetString("URI"));
        }
    }
    #endregion

}

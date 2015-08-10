// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Inventory;

namespace SilverSim.BackendConnectors.IWC
{
    public class IWCInventoryPlugin : IInventoryServicePlugin, IPlugin
    {
        public IWCInventoryPlugin()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        public InventoryServiceInterface Instantiate(string url)
        {
            return new Inventory.IWCInventoryConnector(url);
        }

        public bool IsProtocolSupported(string url)
        {
#warning Determine how to do the IsProtocolSupported on IWC
            return false;
        }

        public string Name
        {
            get
            {
                return "iwc";
            }
        }
    }
    [PluginName("InventoryPlugin")]
    public class SimianInventoryPluginFactory : IPluginFactory
    {
        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new IWCInventoryPlugin();
        }
    }

    public class IWCAssetPlugin : IAssetServicePlugin, IPlugin
    {
        public IWCAssetPlugin()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        public AssetServiceInterface Instantiate(string url)
        {
            return new Asset.IWCAssetConnector(url);
        }

        public bool IsProtocolSupported(string url)
        {
#warning Determine how to do the IsProtocolSupported on IWC
            return false;
        }

        public string Name
        {
            get
            {
                return "iwc";
            }
        }
    }

    [PluginName("AssetPlugin")]
    public class IWCAssetPluginFactory : IPluginFactory
    {
        public IWCAssetPluginFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new IWCAssetPlugin();
        }
    }
}

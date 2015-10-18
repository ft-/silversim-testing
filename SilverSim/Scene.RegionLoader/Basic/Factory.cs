// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Main.Common;

namespace SilverSim.Scene.RegionLoader.Basic
{
    [PluginName("RegionLoader")]
    public class Factory : IPluginFactory
    {
        public Factory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownConfig)
        {
            string regionCfgName = ownConfig.GetString("region_config_source", string.Empty);

            return new RegionLoaderService(ownConfig.GetString("RegionStorage"), regionCfgName);
        }
    }
}

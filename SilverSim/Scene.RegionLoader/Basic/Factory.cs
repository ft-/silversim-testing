// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Main.Common;

namespace SilverSim.Scene.RegionLoader.Basic
{
    [PluginName("Basic")]
    public sealed class Factory : IPluginFactory
    {
        public Factory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownConfig)
        {
            return new RegionLoaderService(ownConfig.GetString("RegionStorage"));
        }
    }
}

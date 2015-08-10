// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Main.Common;
using System;

namespace SilverSim.LoadStore.Terrain
{
    [PluginName("TerrainFileSupport")]
    public class TerrainLoadStoreFactory : IPluginFactory
    {
        public TerrainLoadStoreFactory()
        {

        }
        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new TerrainLoadStore(loader);
        }
    }
}

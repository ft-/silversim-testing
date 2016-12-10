// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Grid;

namespace SilverSim.Main.Cmd.MapServer
{
    public class MapServerCommands : IPlugin
    {
        readonly string m_GridServiceName;
        readonly string m_RegionDefaultFlagsServiceName;
        GridServiceInterface m_GridService;
        RegionDefaultFlagsServiceInterface m_RegionDefaultFlagsService;

        public MapServerCommands(IConfig ownSection)
        {
            m_GridServiceName = ownSection.GetString("GridService", "GridService");
            m_RegionDefaultFlagsServiceName = ownSection.GetString("RegionDefaultFlagsService", "RegionDefaultFlagsService");
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_GridService = loader.GetService<GridServiceInterface>(m_GridServiceName);
            m_RegionDefaultFlagsService = loader.GetService<RegionDefaultFlagsServiceInterface>(m_RegionDefaultFlagsServiceName);
        }
    }

    [PluginName("MapServerCommands")]
    public class MapServerCommandsFactory : IPluginFactory
    {
        public MapServerCommandsFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MapServerCommands(ownSection);
        }
    }
}

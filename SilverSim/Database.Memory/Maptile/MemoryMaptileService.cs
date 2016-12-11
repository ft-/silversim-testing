// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Maptile;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Maptile;
using System.Collections.Generic;
using Nini.Config;
using System;

namespace SilverSim.Database.Memory.Maptile
{
    public class MemoryMaptileService : MaptileServiceInterface, IPlugin
    {
        readonly RwLockedDictionary<string, MaptileData> m_Maptiles = new RwLockedDictionary<string, MaptileData>();

        public MemoryMaptileService()
        {

        }

        static string GetKey(GridVector loc, int zoomlevel)
        {
            return string.Format("{0}-{1}-{2}", loc.X, loc.Y, zoomlevel);
        }

        static string GetKey(MaptileData data)
        {
            return GetKey(data.Location, data.ZoomLevel);
        }

        public override List<MaptileInfo> GetUpdateTimes(UUID scopeid, GridVector minloc, GridVector maxloc, int zoomlevel)
        {
            List<MaptileInfo> infos = new List<MaptileInfo>();
            foreach(MaptileData data in m_Maptiles.Values)
            {
                if(data.Location.X >= minloc.X && data.Location.Y >= minloc.Y && data.Location.X <= maxloc.X && data.Location.Y <= maxloc.Y)
                {
                    infos.Add(new MaptileInfo(data));
                }
            }
            return infos;
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        public override void Store(MaptileData data)
        {
            m_Maptiles[GetKey(data)] = new MaptileData(data);
        }

        public override bool TryGetValue(GridVector location, int zoomlevel, out MaptileData data)
        {
            return m_Maptiles.TryGetValue(GetKey(location, zoomlevel), out data);
        }
    }

    [PluginName("Maptile")]
    public class MemoryMaptileServiceFactory : IPluginFactory
    {
        public MemoryMaptileServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MemoryMaptileService();
        }
    }
}

// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Maptile;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Maptile;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.Database.Memory.Maptile
{
    [Description("Memory Maptile Backend")]
    public class MemoryMaptileService : MaptileServiceInterface, IPlugin
    {
        readonly RwLockedDictionary<string, MaptileData> m_Maptiles = new RwLockedDictionary<string, MaptileData>();

        public MemoryMaptileService()
        {

        }

        static string GetKey(UUID scopeid, GridVector loc, int zoomlevel)
        {
            return string.Format("{0}-{1}-{2}-{3}", scopeid.ToString(), loc.X, loc.Y, zoomlevel);
        }

        static string GetKey(MaptileData data)
        {
            return GetKey(data.ScopeID, data.Location, data.ZoomLevel);
        }

        public override List<MaptileInfo> GetUpdateTimes(UUID scopeid, GridVector minloc, GridVector maxloc, int zoomlevel)
        {
            List<MaptileInfo> infos = new List<MaptileInfo>();
            foreach(MaptileData data in m_Maptiles.Values)
            {
                if(data.ScopeID == scopeid && data.ZoomLevel == zoomlevel && data.Location.X >= minloc.X && data.Location.Y >= minloc.Y && data.Location.X <= maxloc.X && data.Location.Y <= maxloc.Y)
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
            MaptileData ndata = new MaptileData(data);
            ndata.LastUpdate = Date.Now;
            m_Maptiles[GetKey(data)] = ndata;
        }

        public override bool Remove(UUID scopeid, GridVector location, int zoomlevel)
        {
            return m_Maptiles.Remove(GetKey(scopeid, location, zoomlevel));
        }

        public override bool TryGetValue(UUID scopeid, GridVector location, int zoomlevel, out MaptileData data)
        {
            return m_Maptiles.TryGetValue(GetKey(scopeid, location, zoomlevel), out data);
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

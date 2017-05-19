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

        static string GetKey(UUID scopeid, GridVector loc, int zoomlevel) =>
            string.Format("{0}-{1}-{2}-{3}", scopeid.ToString(), loc.X, loc.Y, zoomlevel);

        static string GetKey(MaptileData data) =>
            GetKey(data.ScopeID, data.Location, data.ZoomLevel);

        public override List<MaptileInfo> GetUpdateTimes(UUID scopeid, GridVector minloc, GridVector maxloc, int zoomlevel)
        {
            var infos = new List<MaptileInfo>();
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
            var ndata = new MaptileData(data);
            ndata.LastUpdate = Date.Now;
            m_Maptiles[GetKey(data)] = ndata;
        }

        public override bool Remove(UUID scopeid, GridVector location, int zoomlevel) =>
            m_Maptiles.Remove(GetKey(scopeid, location, zoomlevel));

        public override bool TryGetValue(UUID scopeid, GridVector location, int zoomlevel, out MaptileData data) =>
            m_Maptiles.TryGetValue(GetKey(scopeid, location, zoomlevel), out data);
    }

    [PluginName("Maptile")]
    public class MemoryMaptileServiceFactory : IPluginFactory
    {
        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection) =>
            new MemoryMaptileService();
    }
}

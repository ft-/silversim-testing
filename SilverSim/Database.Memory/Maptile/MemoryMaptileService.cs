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
    [PluginName("Maptile")]
    public class MemoryMaptileService : MaptileServiceInterface, IPlugin
    {
        private readonly RwLockedDictionary<string, MaptileData> m_Maptiles = new RwLockedDictionary<string, MaptileData>();

        private static string GetKey(GridVector loc, int zoomlevel) =>
            string.Format("{0}-{1}-{2}", loc.X, loc.Y, zoomlevel);

        private static string GetKey(MaptileData data) =>
            GetKey(data.Location, data.ZoomLevel);

        public override List<MaptileInfo> GetUpdateTimes(GridVector minloc, GridVector maxloc, int zoomlevel)
        {
            var infos = new List<MaptileInfo>();
            foreach(MaptileData data in m_Maptiles.Values)
            {
                if(data.ZoomLevel == zoomlevel && data.Location.X >= minloc.X && data.Location.Y >= minloc.Y && data.Location.X <= maxloc.X && data.Location.Y <= maxloc.Y)
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

        public override bool Remove(GridVector location, int zoomlevel) =>
            m_Maptiles.Remove(GetKey(location, zoomlevel));

        public override bool TryGetValue(GridVector location, int zoomlevel, out MaptileData data) =>
            m_Maptiles.TryGetValue(GetKey(location, zoomlevel), out data);
    }
}

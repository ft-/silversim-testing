// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Maptile;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.Maptile
{
    public abstract class MaptileServiceInterface
    {
        public MaptileServiceInterface()
        {

        }

        public MaptileData this[GridVector location, int zoomlevel]
        {
            get
            {
                MaptileData data;
                if(TryGetValue(location, zoomlevel, out data))
                {
                    return data;
                }
                throw new KeyNotFoundException();
            }
        }

        public abstract bool TryGetValue(GridVector location, int zoomlevel, out MaptileData data);

        public abstract void Store(MaptileData data);

        public abstract List<MaptileInfo> GetUpdateTimes(UUID scopeid, GridVector minloc, GridVector maxloc, int zoomlevel);
    }
}

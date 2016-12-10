// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Grid;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.Grid
{
    public abstract class RegionDefaultFlagsServiceInterface
    {
        public RegionDefaultFlagsServiceInterface()
        {

        }

        /* function does not fail if no data is defined */
        public abstract RegionFlags GetRegionDefaultFlags(UUID regionId);
        public abstract void ChangeRegionDefaultFlags(UUID regionId, RegionFlags addFlags, RegionFlags removeFlags);

        public abstract Dictionary<UUID, RegionFlags> GetAllRegionDefaultFlags();
    }
}

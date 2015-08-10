// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;
using SilverSim.Types.Grid;

namespace SilverSim.ServiceInterfaces.Neighbor
{
    public abstract class NeighborServiceInterface
    {
        public NeighborServiceInterface()
        {

        }

        public abstract void notifyNeighborStatus(RegionInfo fromRegion, RegionInfo toRegion);

        public enum ServiceTypeEnum
        {
            Local,
            Remote
        }

        public abstract ServiceTypeEnum ServiceType { get; }
    }
}

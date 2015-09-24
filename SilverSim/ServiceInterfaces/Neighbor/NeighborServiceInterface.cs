// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types.Grid;

namespace SilverSim.ServiceInterfaces.Neighbor
{
    public abstract class NeighborServiceInterface
    {
        public NeighborServiceInterface()
        {

        }

        public abstract void notifyNeighborStatus(RegionInfo fromRegion);
    }
}

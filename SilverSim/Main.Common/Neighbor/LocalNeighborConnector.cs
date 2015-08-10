// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Management.Scene;
using SilverSim.ServiceInterfaces.Neighbor;
using SilverSim.Types.Grid;

namespace SilverSim.Main.Common.Neighbor
{
    public class LocalNeighborConnector : NeighborServiceInterface, IPlugin
    {
        public LocalNeighborConnector()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        public override void notifyNeighborStatus(RegionInfo fromRegion, RegionInfo toRegion)
        {
            if ((fromRegion.Flags & RegionFlags.RegionOnline) == RegionFlags.RegionOnline)
            {
                SceneManager.Neighbors.Add(fromRegion);
            }
            else
            {
                SceneManager.Neighbors.Remove(fromRegion);
            }
        }

        public override ServiceTypeEnum ServiceType
        {
            get
            {
                return ServiceTypeEnum.Local;
            }
        }
    }
}

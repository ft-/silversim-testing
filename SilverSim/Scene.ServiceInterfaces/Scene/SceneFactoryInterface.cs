// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Scene;
using SilverSim.Types.Grid;

namespace SilverSim.Scene.ServiceInterfaces.Scene
{
    public abstract class SceneFactoryInterface
    {
        protected SceneFactoryInterface()
        {

        }

        public abstract SceneInterface Instantiate(RegionInfo ri);
    }
}

// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using EnvironmentController = SilverSim.Scene.Types.Scene.SceneInterface.EnvironmentController;

namespace SilverSim.Scene.Types.Scene
{
    /* this class provides a simple no wind model for having at least some Wind model hooked to EnvironmentController */
    public class NoWindModel : IWindModel
    {
        public Vector3 this[Vector3 pos]
        {
            get
            {
                return new Vector3();
            }

            set
            {
                /* intentionally left empty */
            }
        }

        public Vector3 PrevailingWind
        {
            get
            {
                return new Vector3();
            }
        }

        public void UpdateModel(EnvironmentController.SunData sunData)
        {
            /* intentionally left empty */
        }
    }
}

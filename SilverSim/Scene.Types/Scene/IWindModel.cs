// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using EnvironmentController = SilverSim.Scene.Types.Scene.SceneInterface.EnvironmentController;

namespace SilverSim.Scene.Types.Scene
{
    public interface IWindModel
    {
        Vector3 this[Vector3 pos] { get; set; }
        Vector3 PrevailingWind { get; }

        void UpdateModel(EnvironmentController.SunData sunData);
    }
}

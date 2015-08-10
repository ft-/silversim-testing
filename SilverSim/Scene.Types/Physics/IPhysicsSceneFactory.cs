// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Scene;

namespace SilverSim.Scene.Types.Physics
{
    public interface IPhysicsSceneFactory
    {
        IPhysicsScene InstantiatePhysicsScene(SceneInterface scene);
    }
}

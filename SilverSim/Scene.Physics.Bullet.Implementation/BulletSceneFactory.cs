// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Scene;
using System;

namespace SilverSim.Scene.Physics.Bullet.Implementation
{
    public class BulletSceneFactory : IPlugin, IPhysicsSceneFactory
    {
        public BulletSceneFactory(IConfig ownSection)
        {

        }

        public void Startup(ConfigurationLoader loader)
        {
        }

        public IPhysicsScene InstantiatePhysicsScene(SceneInterface scene)
        {
            return new BulletScene(scene);
        }
    }
}

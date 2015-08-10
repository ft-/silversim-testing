// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Main.Common;

namespace SilverSim.Scene.Physics.Bullet.Implementation
{
    public class PluginFactory : IPluginFactory
    {
        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new BulletSceneFactory(ownSection);
        }
    }
}

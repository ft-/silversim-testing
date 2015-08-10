// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;

namespace SilverSim.Main.Common
{
    /* Factory class for object creation */
    public interface IPluginFactory
    {
        IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection);
    }
}

// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.Main.Common
{
    public interface IPlugin
    {
        void Startup(ConfigurationLoader loader);
    }
}

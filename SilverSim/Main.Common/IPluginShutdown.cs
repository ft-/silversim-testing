// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.Main.Common
{
    public enum ShutdownOrder
    {
        LogoutAgents = -20,
        LogoutRegion = -10,
        Any = 0,
        LogoutDatabase = 10
    }

    public interface IPluginShutdown
    {
        ShutdownOrder ShutdownOrder { get; }
        void Shutdown();
    }
}

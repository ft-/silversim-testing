// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Tests.Extensions;
using SilverSim.Types.Asset.Format;

namespace SilverSim.Tests.Assets
{
    public class References : ITest
    {
        public bool Run()
        {
            Notecard notecard = new Notecard();
            return true;
        }

        public void Startup(ConfigurationLoader loader)
        {
        }
    }
}

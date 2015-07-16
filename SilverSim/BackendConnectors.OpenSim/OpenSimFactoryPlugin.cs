﻿/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces;
using SilverSim.ServiceInterfaces.Profile;

namespace SilverSim.BackendConnectors.OpenSim
{
    public class ProfileInventoryPlugin : ServicePluginHelo, IProfileServicePlugin, IPlugin
    {
        string m_ProfileName;
        public ProfileInventoryPlugin(string profileName)
        {
            m_ProfileName = profileName;
        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        public ProfileServiceInterface Instantiate(string url)
        {
            return new Profile.ProfileConnector(url);
        }

        public override string Name
        {
            get
            {
                return m_ProfileName;
            }
        }
    }
    [PluginName("SimianProfilePlugin")]
    public class SimianProfilePluginFactory : IPluginFactory
    {
        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new ProfileInventoryPlugin("opensim-simian");
        }
    }

    [PluginName("RobustProfilePlugin")]
    public class RobustProfilePluginFactory : IPluginFactory
    {
        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new ProfileInventoryPlugin("opensim-robust");
        }
    }

}

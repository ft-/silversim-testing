// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using System.Collections.Generic;

namespace SilverSim.WebIF.Admin
{
    public static class AdminExtensionMethods
    {
        public static IAdminWebIF GetAdminWebIF(this ConfigurationLoader loader)
        {
            List<IAdminWebIF> webIF = loader.GetServicesByValue<IAdminWebIF>();
            if (webIF.Count == 0)
            {
                throw new ConfigurationLoader.ConfigurationErrorException("No Admin WebIF service configured");
            }
            return webIF[0];
        }

    }
}

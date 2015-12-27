// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using System.Collections.Generic;

namespace SilverSim.WebIF.Admin
{
    public static class AdminExtensionMethods
    {
        public static AdminWebIF GetAdminWebIF(this ConfigurationLoader loader)
        {
            List<AdminWebIF> webIF = loader.GetServicesByValue<AdminWebIF>();
            if (webIF.Count == 0)
            {
                throw new ConfigurationLoader.ConfigurationErrorException("No Admin WebIF service configured");
            }
            return webIF[0];
        }

    }
}

// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.Grid
{
    public interface IForeignGridConnectorPlugin
    {
        ForeignGridConnector Instantiate(string url);
        string Name { get; }
        string DisplayName { get; } /* name for display in error messages */
        bool IsProtocolSupported(string url);
        bool IsAgentSupported(List<GridType> gridTypes);
    }
}

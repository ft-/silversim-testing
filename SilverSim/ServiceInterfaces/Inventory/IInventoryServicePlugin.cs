// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.ServiceInterfaces.Inventory
{
    public interface IInventoryServicePlugin
    {
        InventoryServiceInterface Instantiate(string url);
        string Name { get; }
        bool IsProtocolSupported(string url);
    }
}

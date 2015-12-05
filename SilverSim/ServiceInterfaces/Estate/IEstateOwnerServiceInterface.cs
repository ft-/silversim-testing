// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.ServiceInterfaces.Estate
{
    public interface IEstateOwnerServiceInterface
    {
        UUI this[uint estateID] { get; set; }
        bool TryGetValue(uint estateID, out UUI uui);
    }
}

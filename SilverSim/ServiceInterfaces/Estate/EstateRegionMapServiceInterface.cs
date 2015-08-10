// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.Estate
{
    public interface EstateRegionMapServiceInterface
    {
        List<UUID> this[uint EstateID] { get; }
        uint this[UUID regionID] { get; set; }
    }
}

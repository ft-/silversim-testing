﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.Estate
{
    public interface IEstateRegionMapServiceInterface
    {
        List<UUID> this[uint estateID] { get; }
        uint this[UUID regionID] { get; set; }
        bool TryGetValue(UUID regionID, out uint estateID);
    }
}

// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types.Estate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.ServiceInterfaces.Estate
{
    public abstract class EstateServiceInterface
    {
        public EstateServiceInterface()
        {

        }

        public abstract EstateInfo this[uint estateID]
        {
            get;
            set;
        }

        public abstract void Add(EstateInfo estateInfo);

        public abstract bool TryGetValue(uint estateID, out EstateInfo estateInfo);
        public abstract bool TryGetValue(string estateName, out EstateInfo estateInfo);
        public abstract bool ContainsKey(uint estateID);
        public abstract bool ContainsKey(string estateName);

        public abstract List<EstateInfo> All { get; }

        public abstract List<uint> AllIDs { get; }

        public abstract IEstateManagerServiceInterface EstateManager { get; }
        public abstract IEstateOwnerServiceInterface EstateOwner { get; }
        public abstract IEstateAccessServiceInterface EstateAccess { get; }
        public abstract IEstateBanServiceInterface EstateBans { get; }
        public abstract IEstateGroupsServiceInterface EstateGroup { get; }
        public abstract IEstateRegionMapServiceInterface RegionMap { get; }
    }
}

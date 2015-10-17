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

        public abstract List<EstateInfo> All { get; }

        public abstract List<uint> AllIDs { get; }

        public abstract EstateManagerServiceInterface EstateManager { get; }
        public abstract IEstateOwnerServiceInterface EstateOwner { get; }
        public abstract EstateAccessServiceInterface EstateAccess { get; }
        public abstract EstateGroupsServiceInterface EstateGroup { get; }
        public abstract EstateRegionMapServiceInterface RegionMap { get; }
    }
}

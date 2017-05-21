// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using SilverSim.Types.Estate;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.Estate
{
    public abstract class EstateServiceInterface
    {
        public abstract EstateInfo this[uint estateID] { get; set; }

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

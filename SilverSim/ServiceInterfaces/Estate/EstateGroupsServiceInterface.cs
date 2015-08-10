// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.Estate
{
    public abstract class EstateGroupsServiceInterface
    {
        public interface ListAccess
        {
            List<UGI> this[uint estateID] { get; }
        }

        public EstateGroupsServiceInterface()
        {

        }

        public abstract bool this[uint estateID, UGI group] { get; set; }

        public abstract ListAccess All { get; }
    }
}

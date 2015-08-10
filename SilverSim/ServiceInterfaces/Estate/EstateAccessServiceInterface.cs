// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.Estate
{
    public abstract class EstateAccessServiceInterface
    {
        public interface ListAccess
        {
            List<UUI> this[uint estateID] { get; }
        }

        public EstateAccessServiceInterface()
        {

        }

        public abstract bool this[uint estateID, UUI agent] { get; set; }

        public abstract ListAccess All { get; }
    }
}

// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.ServiceInterfaces.Estate
{
    public abstract class EstateGroupsServiceInterface
    {
        [SuppressMessage("Gendarme.Rules.Design", "ConsiderAddingInterfaceRule")]
        public interface IListAccess
        {
            List<UGI> this[uint estateID] { get; }
        }

        public EstateGroupsServiceInterface()
        {

        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public abstract bool this[uint estateID, UGI group] { get; set; }

        public abstract IListAccess All { get; }
    }
}

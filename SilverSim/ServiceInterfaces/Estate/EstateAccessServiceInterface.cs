// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.ServiceInterfaces.Estate
{
    public abstract class EstateAccessServiceInterface
    {
        [SuppressMessage("Gendarme.Rules.Design", "ConsiderAddingInterfaceRule")]
        public interface IListAccess
        {
            List<UUI> this[uint estateID] { get; }
        }

        public EstateAccessServiceInterface()
        {

        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public abstract bool this[uint estateID, UUI agent] { get; set; }

        public abstract IListAccess All { get; }
    }
}

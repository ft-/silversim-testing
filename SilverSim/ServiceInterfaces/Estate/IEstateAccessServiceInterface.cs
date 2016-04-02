// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.ServiceInterfaces.Estate
{
    public interface IEstateAccessServiceListAccessInterface
    {
        List<UUI> this[uint estateID] { get; }
    }

    public interface IEstateAccessServiceInterface
    {
        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        bool this[uint estateID, UUI agent] { get; set; }

        IEstateAccessServiceListAccessInterface All { get; }
    }
}

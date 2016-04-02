// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.ServiceInterfaces.Estate
{
    public interface IEstateGroupsServiceListAccessInterface
    {
        List<UGI> this[uint estateID] { get; }
    }

    public interface IEstateGroupsServiceInterface
    {
        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        bool this[uint estateID, UGI group] { get; set; }

        IEstateGroupsServiceListAccessInterface All { get; }
    }
}

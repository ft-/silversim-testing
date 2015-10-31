// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Types.Grid
{
    [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
    public enum RegionAccess : byte
    {
        Unknown = 0,
        Trial = 7,
        PG = 13,
        Mature = 21,
        Adult = 42,
        Down = 254,
        NonExistent = 255
    }
}

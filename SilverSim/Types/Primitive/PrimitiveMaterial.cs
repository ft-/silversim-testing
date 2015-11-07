// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Types.Primitive
{
    [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
    public enum PrimitiveMaterial
    {
        Stone = 0,
        Metal = 1,
        Glass = 2,
        Wood = 3,
        Flesh = 4,
        Plastic = 5,
        Rubber = 6,
        Light = 7
    }
}

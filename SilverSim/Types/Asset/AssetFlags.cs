// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.Types.Asset
{
    [Flags]
    public enum AssetFlags : uint
    {
        Normal = 0,
        Maptile = 1,
        Rewritable = 2,
        Collectable = 4
    }
}

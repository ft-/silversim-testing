// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Types.Parcel
{
    [Flags]
    [SuppressMessage("Gendarme.Rules.Design", "FlagsShouldNotDefineAZeroValueRule")]
    public enum ParcelOverlayType
    {
        Public = 0,
        OwnedByOther = 1,
        OwnedByGroup = 2,
        OwnedBySelf = 3,
        ForSale = 4,
        Auction = 5,
        Private = 32,
        BorderWest = 64,
        BorderSouth = 128
    }
}

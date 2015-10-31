// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Types.Grid
{
    [Flags]
    [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
    [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    public enum RegionFlags : uint
    {
        None = 0,
        DefaultRegion = 1,
        FallbackRegion = 2,
        RegionOnline = 4,
        NoDirectLogin = 8,
        Persistent = 16,
        LockedOut = 32,
        NoMove = 64,
        Reservation = 128,
        Authenticate = 256,
        Hyperlink = 512,
        DefaultHGRegion = 1024
    }
}
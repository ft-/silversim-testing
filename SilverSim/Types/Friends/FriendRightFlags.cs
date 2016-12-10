// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.Types.Friends
{
    [Flags]
    public enum FriendRightFlags : uint
    {
        None = 0,
        SeeOnline = 1,
        SeeOnMap = 2,
        ModifyItems = 4
    }
}

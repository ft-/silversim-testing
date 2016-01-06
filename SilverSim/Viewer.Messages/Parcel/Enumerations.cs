// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.Viewer.Messages.Parcel
{
    [Flags]
    public enum ParcelAccessList : uint
    {
        Access = 1,
        Ban = 2,
        AllowExperience = 8,
        BlockExperience = 16
    }
}

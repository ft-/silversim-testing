// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.Scene.Types.Physics.Vehicle
{
    [Flags]
    public enum VehicleFlags : int
    {
        None = 0,
        NoDeflectionUp = 0x0001,
        LimitRollOnly = 0x0002,
        HoverWaterOnly = 0x0004,
        HoverTerrainOnly = 0x0008,
        HoverGlobalHeight = 0x0010,
        HoverUpOnly = 0x0020,
        LimitMotorUp = 0x0040,
        MouselookSteer = 0x0080,
        MouselookBank = 0x0100,
        CameraDecoupled = 0x0200
    }
}

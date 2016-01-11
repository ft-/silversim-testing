// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.Scene.Types.Object
{
    [Flags]
    public enum DetectedTypeFlags
    {
        Agent = 1 << 0,
        Active = 1 << 1,
        Passive = 1 << 2,
        Scripted = 1 << 3,
        Npc = 1 << 5
    }
}

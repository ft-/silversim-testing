// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.Scene.Npc
{
    [Flags]
    public enum NpcOptions
    {
        None = 0,
        SenseAsAgent = 1,
        Persistent = 2
    }
}

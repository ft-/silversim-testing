// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.Types.Estate
{
    [Flags]
    public enum EstateAccessFlags
    {
        AllowedAgents = 1 << 0,
        AllowedGroups = 1 << 1,
        BannedAgents = 1 << 2,
        Managers = 1 << 3
    }
}

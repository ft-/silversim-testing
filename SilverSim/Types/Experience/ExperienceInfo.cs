// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Script;

namespace SilverSim.Types.Experience
{
    public struct ExperiencePermissionsInfo
    {
        public UUID ID;
        public UUI Agent;

        public ScriptPermissions Permissions;
    }

    public struct ExperienceInfo
    {
        public UUID ID;
        public string Name;
    }
}

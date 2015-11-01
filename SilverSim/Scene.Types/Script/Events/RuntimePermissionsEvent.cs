// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Script;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Types.Script.Events
{
    [SuppressMessage("Gendarme.Rules.Performance", "AvoidLargeStructureRule")]
    public struct RuntimePermissionsEvent : IScriptEvent
    {
        public ScriptPermissions Permissions;
        public UUI PermissionsKey;
    }
}

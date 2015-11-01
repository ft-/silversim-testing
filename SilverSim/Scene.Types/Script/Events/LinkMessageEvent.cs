// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Types.Script.Events
{
    [SuppressMessage("Gendarme.Rules.Performance", "AvoidLargeStructureRule")]
    public struct LinkMessageEvent : IScriptEvent
    {
        public int SenderNumber;
        public int TargetNumber;    /* specific extension to specify destination */

        public int Number;
        public string Data;
        public string Id; /* LSL does not limit key do UUIDs */
    }
}

// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Types.Script.Events
{
    [SuppressMessage("Gendarme.Rules.Performance", "AvoidLargeStructureRule")]
    public struct AtRotTargetEvent : IScriptEvent
    {
        public int Handle;
        public Quaternion TargetRotation;
        public Quaternion OurRotation;
    }
}

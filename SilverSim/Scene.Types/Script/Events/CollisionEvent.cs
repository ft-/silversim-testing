// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Types.Script.Events
{
    [SuppressMessage("Gendarme.Rules.Performance", "AvoidLargeStructureRule")]
    public struct CollisionEvent : IScriptDetectedEvent
    {
        public enum CollisionType
        {
            Continuous,
            Start,
            End
        }
        public CollisionType Type;
        public List<DetectInfo> Detected { get; set; }
    }
}

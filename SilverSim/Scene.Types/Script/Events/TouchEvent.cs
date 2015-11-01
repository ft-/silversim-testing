// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Types.Script.Events
{
    [SuppressMessage("Gendarme.Rules.Performance", "AvoidLargeStructureRule")]
    public struct TouchEvent : IScriptEvent
    {
        public enum TouchType
        {
            Continuous,
            Start,
            End
        }
        public TouchType Type;
        public List<DetectInfo> Detected;
    }
}

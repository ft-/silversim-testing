// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Collections.Generic;

namespace SilverSim.Scene.Types.Script.Events
{
    public struct CollisionEvent : IScriptEvent
    {
        public enum CollisionType
        {
            Continuous,
            Start,
            End
        }
        public CollisionType Type;
        public List<DetectInfo> Detected;
    }
}

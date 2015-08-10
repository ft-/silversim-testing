// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Collections.Generic;

namespace SilverSim.Scene.Types.Script.Events
{
    public struct SensorEvent : IScriptEvent
    {
        public List<DetectInfo> Data;
    }
}

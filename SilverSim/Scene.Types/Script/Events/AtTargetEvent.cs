// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Scene.Types.Script.Events
{
    public struct AtTargetEvent : IScriptEvent
    {
        public int Handle;
        public Vector3 TargetPosition;
        public Vector3 OurPosition;
    }
}

// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.Scene.Types.Script.Events
{
    public struct OnRezEvent : IScriptEvent
    {
        public int StartParam;

        public OnRezEvent(int startParam)
        {
            StartParam = startParam;
        }
    }
}

// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Scene.Types.Script.Events
{
    public struct LinkMessageEvent : IScriptEvent
    {
        public int SenderNumber;
        public int TargetNumber;    /* specific extension to specify destination */

        public int Number;
        public string Data;
        public string Id; /* LSL does not limit key do UUIDs */
    }
}

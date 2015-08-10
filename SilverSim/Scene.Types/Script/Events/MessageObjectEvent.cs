// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Scene.Types.Script.Events
{
    public class MessageObjectEvent : IScriptEvent
    {
        public UUID ObjectID;
        public string Data;
    }
}

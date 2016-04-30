// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Scene.Types.Script.Events
{
    public struct ItemSoldEvent : IScriptEvent
    {
        public UUI Agent;
        public UUID ObjectID;
        public string ObjectName;
    }
}
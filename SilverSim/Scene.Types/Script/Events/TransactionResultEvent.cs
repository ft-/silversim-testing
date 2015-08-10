// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Scene.Types.Script.Events
{
    public struct TransactionResultEvent : IScriptEvent
    {
        public UUID TransactionID;
        public bool Success;
    }
}

// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Diagnostics.CodeAnalysis;
namespace SilverSim.Scene.Types.Script.Events
{
    [SuppressMessage("Gendarme.Rules.Performance", "AvoidLargeStructureRule")]
    public struct OnRezEvent : IScriptEvent
    {
        public int StartParam;

        public OnRezEvent(int startParam)
        {
            StartParam = startParam;
        }
    }
}

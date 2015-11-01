// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Types.Script.Events
{
    [SuppressMessage("Gendarme.Rules.Performance", "AvoidLargeStructureRule")]
    public struct HttpResponseEvent : IScriptEvent
    {
        public UUID RequestID;
        public int Status;
        public AnArray Metadata;
        public string Body;
    }
}

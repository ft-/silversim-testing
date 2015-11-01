// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Types.Script.Events
{
    [SuppressMessage("Gendarme.Rules.Performance", "AvoidLargeStructureRule")]
    public struct RemoteDataEvent : IScriptEvent
    {
        public Int32 Type;
        public UUID Channel;
        public UUID MessageID;
        public string Sender;
        public Int32 IData;
        public string SData;
    }
}

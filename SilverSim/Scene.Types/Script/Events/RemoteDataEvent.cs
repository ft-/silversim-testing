// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Scene.Types.Script.Events
{
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

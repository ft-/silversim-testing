// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Script
{
    [UDPMessage(MessageType.ScriptControlChange)]
    [Reliable]
    [Trusted]
    public class ScriptControlChange : Message
    {
        public struct DataEntry
        {
            public bool TakeControls;
            public UInt32 Controls;
            public bool PassToAgent;
        }

        public List<DataEntry> Data = new List<DataEntry>();

        public ScriptControlChange()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUInt8((byte)Data.Count);
            foreach(DataEntry d in Data)
            {
                p.WriteBoolean(d.TakeControls);
                p.WriteUInt32(d.Controls);
                p.WriteBoolean(d.PassToAgent);
            }
        }
    }
}

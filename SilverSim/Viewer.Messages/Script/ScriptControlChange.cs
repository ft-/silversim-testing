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
            p.WriteUInt8((byte)Data.Count);
            foreach(DataEntry d in Data)
            {
                p.WriteBoolean(d.TakeControls);
                p.WriteUInt32(d.Controls);
                p.WriteBoolean(d.PassToAgent);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            ScriptControlChange m = new ScriptControlChange();
            uint n = p.ReadUInt8();
            while(n-- != 0)
            {
                DataEntry d = new DataEntry();
                d.TakeControls = p.ReadBoolean();
                d.Controls = p.ReadUInt32();
                d.PassToAgent = p.ReadBoolean();
                m.Data.Add(d);
            }
            return m;
        }
    }
}

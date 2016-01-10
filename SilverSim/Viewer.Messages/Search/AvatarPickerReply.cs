// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Search
{
    [UDPMessage(MessageType.AvatarPickerReply)]
    [Reliable]
    [Trusted]
    public class AvatarPickerReply : Message
    {
        public UUID AgentID;
        public UUID QueryID;

        public struct DataEntry
        {
            public UUID AvatarID;
            public string FirstName;
            public string LastName;
        }

        public List<DataEntry> Data = new List<DataEntry>();

        public AvatarPickerReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(QueryID);
            p.WriteUInt8((byte)Data.Count);
            foreach (DataEntry d in Data)
            {
                p.WriteUUID(d.AvatarID);
                p.WriteStringLen8(d.FirstName);
                p.WriteStringLen8(d.LastName);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            AvatarPickerReply m = new AvatarPickerReply();
            m.AgentID = p.ReadUUID();
            m.QueryID = p.ReadUUID();
            uint n = p.ReadUInt8();
            while(n-- != 0)
            {
                DataEntry d = new DataEntry();
                d.AvatarID = p.ReadUUID();
                d.FirstName = p.ReadStringLen8();
                d.LastName = p.ReadStringLen8();
                m.Data.Add(d);
            }
            return m;
        }
    }
}

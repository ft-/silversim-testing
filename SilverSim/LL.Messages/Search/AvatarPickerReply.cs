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
            p.WriteMessageType(Number);
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
    }
}

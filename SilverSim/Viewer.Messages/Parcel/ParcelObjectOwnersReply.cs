using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Parcel
{
    [UDPMessage(MessageType.ParcelObjectOwnersReply)]
    [Reliable]
    [Trusted]
    public sealed class ParcelObjectOwnersReply : Message
    {
        public class DataEntry
        {
            public UUID OwnerID;
            public bool IsGroupOwned;
            public int Count;
            public bool IsOnline;
        }

        public List<DataEntry> Data = new List<DataEntry>();

        public static Message Decode(UDPPacket p)
        {
            var e = new ParcelObjectOwnersReply();
            uint n = p.ReadUInt8();
            for(uint i = 0; i < n; ++i)
            {
                e.Data.Add(new DataEntry
                {
                    OwnerID = p.ReadUUID(),
                    IsGroupOwned = p.ReadBoolean(),
                    Count = p.ReadInt32(),
                    IsOnline = p.ReadBoolean()
                });
            }

            return e;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUInt8((byte)Data.Count);
            foreach(DataEntry e in Data)
            {
                p.WriteUUID(e.OwnerID);
                p.WriteBoolean(e.IsGroupOwned);
                p.WriteInt32(e.Count);
                p.WriteBoolean(e.IsOnline);
            }
        }
    }
}

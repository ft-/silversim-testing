// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Parcel
{
    [UDPMessage(MessageType.ParcelAccessListReply)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class ParcelAccessListReply : Message
    {
        public UUID AgentID;
        public Int32 SequenceID;
        public ParcelAccessList Flags;
        public Int32 LocalID;

        public struct Data
        {
            public UUID ID;
            public UInt32 Time;
            public ParcelAccessList Flags;
        }

        public List<Data> AccessList = new List<Data>();

        public ParcelAccessListReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteInt32(SequenceID);
            p.WriteUInt32((uint)Flags);
            p.WriteInt32(LocalID);

            p.WriteUInt8((byte)AccessList.Count);
            foreach (Data d in AccessList)
            {
                p.WriteUUID(d.ID);
                p.WriteUInt32(d.Time);
                p.WriteUInt32((uint)d.Flags);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            ParcelAccessListReply m = new ParcelAccessListReply();
            m.AgentID = p.ReadUUID();
            m.SequenceID = p.ReadInt32();
            m.Flags = (ParcelAccessList)p.ReadUInt32();
            m.LocalID = p.ReadInt32();

            uint n = p.ReadUInt8();
            while(n-- != 0)
            {
                Data d = new Data();
                d.ID = p.ReadUUID();
                d.Time = p.ReadUInt32();
                d.Flags = (ParcelAccessList)p.ReadUInt32();
                m.AccessList.Add(d);
            }
            return m;
        }
    }
}

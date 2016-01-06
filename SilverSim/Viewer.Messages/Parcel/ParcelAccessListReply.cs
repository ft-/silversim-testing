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
            p.WriteMessageType(Number);
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
    }
}

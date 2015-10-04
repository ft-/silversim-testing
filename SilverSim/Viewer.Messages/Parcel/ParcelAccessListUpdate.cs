// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Parcel
{
    [UDPMessage(MessageType.ParcelAccessListUpdate)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class ParcelAccessListUpdate : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UInt32 Flags;
        public Int32 LocalID;
        public UUID TransactionID;
        public Int32 SequenceID;
        public Int32 Sections;

        public struct Data
        {
            public UUID ID;
            public UInt32 Time;
            public UInt32 Flags;
        }

        public List<Data> AccessList = new List<Data>();

        public ParcelAccessListUpdate()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ParcelAccessListUpdate m = new ParcelAccessListUpdate();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.Flags = p.ReadUInt32();
            m.LocalID = p.ReadInt32();
            m.TransactionID = p.ReadUUID();
            m.SequenceID = p.ReadInt32();
            m.Sections = p.ReadInt32();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                Data d;
                d.ID = p.ReadUUID();
                d.Time = p.ReadUInt32();
                d.Flags = p.ReadUInt32();
                m.AccessList.Add(d);
            }

            return m;
        }
    }
}

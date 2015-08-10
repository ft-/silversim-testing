// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.LL.Messages.Parcel
{
    [UDPMessage(MessageType.ParcelAccessListRequest)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class ParcelAccessListRequest : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public Int32 SequenceID;
        public UInt32 Flags;
        public Int32 LocalID;

        public ParcelAccessListRequest()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ParcelAccessListRequest m = new ParcelAccessListRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.SequenceID = p.ReadInt32();
            m.Flags = p.ReadUInt32();
            m.LocalID = p.ReadInt32();

            return m;
        }
    }
}

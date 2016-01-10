// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Parcel
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
        public ParcelAccessList Flags;
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
            m.Flags = (ParcelAccessList)p.ReadUInt32();
            m.LocalID = p.ReadInt32();

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteInt32(SequenceID);
            p.WriteUInt32((uint)Flags);
            p.WriteInt32(LocalID);
        }
    }
}

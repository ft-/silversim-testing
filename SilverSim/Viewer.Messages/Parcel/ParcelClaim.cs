// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Parcel
{
    [UDPMessage(MessageType.ParcelClaim)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class ParcelClaim : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public UUID GroupID;
        public bool IsGroupOwned;
        public bool IsFinal;

        public struct ParcelDataEntry
        {
            public double West;
            public double South;
            public double East;
            public double North;
        }

        public List<ParcelDataEntry> ParcelData = new List<ParcelDataEntry>();

        public ParcelClaim()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ParcelClaim m = new ParcelClaim();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            m.GroupID = p.ReadUUID();
            m.IsGroupOwned = p.ReadBoolean();
            m.IsFinal = p.ReadBoolean();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                ParcelDataEntry d;
                d.West = p.ReadFloat();
                d.South = p.ReadFloat();
                d.East = p.ReadFloat();
                d.North = p.ReadFloat();
                m.ParcelData.Add(d);
            }

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(GroupID);
            p.WriteBoolean(IsGroupOwned);
            p.WriteBoolean(IsFinal);

            p.WriteUInt8((byte)ParcelData.Count);
            foreach(ParcelDataEntry d in ParcelData)
            {
                p.WriteFloat((float)d.West);
                p.WriteFloat((float)d.South);
                p.WriteFloat((float)d.East);
                p.WriteFloat((float)d.North);
            }
        }
    }
}

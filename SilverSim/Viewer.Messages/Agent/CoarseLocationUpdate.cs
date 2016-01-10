// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Agent
{
    [UDPMessage(MessageType.CoarseLocationUpdate)]
    [Trusted]
    public class CoarseLocationUpdate : Message
    {
        public Int16 You;
        public Int16 Prey;

        public struct AgentDataEntry
        {
            public byte X;
            public byte Y;
            public byte Z;
            public UUID AgentID;
        }
        public List<AgentDataEntry> AgentData = new List<AgentDataEntry>();

        public CoarseLocationUpdate()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUInt8((byte)AgentData.Count);
            foreach (AgentDataEntry d in AgentData)
            {
                p.WriteUInt8(d.X);
                p.WriteUInt8(d.Y);
                p.WriteUInt8(d.Z);
            }
            p.WriteInt16(You);
            p.WriteInt16(Prey);
            p.WriteUInt8((byte)AgentData.Count);
            foreach (AgentDataEntry d in AgentData)
            {
                p.WriteUUID(d.AgentID);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            CoarseLocationUpdate m = new CoarseLocationUpdate();
            uint cnt = p.ReadUInt8();
            for (uint i = 0; i < cnt; ++i)
            {
                AgentDataEntry d = new AgentDataEntry();
                d.X = p.ReadUInt8();
                d.Y = p.ReadUInt8();
                d.Z = p.ReadUInt8();
                m.AgentData.Add(d);
            }

            m.You = p.ReadInt16();
            m.Prey = p.ReadInt16();

            cnt = p.ReadUInt8();
            for(int i = 0; i < cnt; ++i)
            {
                AgentDataEntry e = m.AgentData[i];
                e.AgentID = p.ReadUUID();
                m.AgentData[i] = e;
            }

            return m;
        }
    }
}

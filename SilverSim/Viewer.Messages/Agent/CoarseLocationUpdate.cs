// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

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
            var m = new CoarseLocationUpdate();
            uint cnt = p.ReadUInt8();
            for (uint i = 0; i < cnt; ++i)
            {
                var d = new AgentDataEntry();
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
                var e = m.AgentData[i];
                e.AgentID = p.ReadUUID();
                m.AgentData[i] = e;
            }

            return m;
        }
    }
}

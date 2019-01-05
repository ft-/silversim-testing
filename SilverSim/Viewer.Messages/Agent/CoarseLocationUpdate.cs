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
using MapType = SilverSim.Types.Map;

namespace SilverSim.Viewer.Messages.Agent
{
    [UDPMessage(MessageType.CoarseLocationUpdate)]
    [EventQueueGet("CoarseLocationUpdate")]
    [Trusted]
    public class CoarseLocationUpdate : Message
    {
        public short You;
        public short Prey;

        public struct AgentDataEntry
        {
            public int X;
            public int Y;
            public int Z;
            public UUID AgentID;
        }
        public List<AgentDataEntry> AgentData = new List<AgentDataEntry>();

        public override void Serialize(UDPPacket p)
        {
            p.WriteUInt8((byte)AgentData.Count);
            foreach (AgentDataEntry d in AgentData)
            {
                p.WriteUInt8((byte)d.X.Clamp(0, 255));
                p.WriteUInt8((byte)d.Y.Clamp(0, 255));
                p.WriteUInt8((byte)d.Z.Clamp(0, 255));
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
                m.AgentData.Add(new AgentDataEntry
                {
                    X = p.ReadUInt8(),
                    Y = p.ReadUInt8(),
                    Z = p.ReadUInt8()
                });
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

        public override IValue SerializeEQG()
        {
            var location = new AnArray();
            var agentdata = new AnArray();
            var index = new AnArray
            {
                new MapType
                {
                    { "Prey", Prey },
                    { "You", You }
                }
            };

            foreach (AgentDataEntry d in AgentData)
            {
                location.Add(new MapType
                {
                    { "X", d.X },
                    { "Y", d.Y },
                    { "Z", d.Z }
                });

                agentdata.Add(new MapType
                {
                    ["AgentID"] = d.AgentID
                });
            }

            return new MapType
            {
                ["Index"] = index,
                ["Location"] = location,
                ["AgentData"] = agentdata
            };
        }

        public static Message DeserializeEQG(IValue value)
        {
            var m = (MapType)value;
            var index = (MapType)((AnArray)m["Index"])[0];
            var location = (AnArray)m["Location"];
            var agentdata = (AnArray)m["AgentData"];
            int n = Math.Min(agentdata.Count, location.Count);

            var res = new CoarseLocationUpdate
            {
                Prey = (short)index["Prey"].AsInt,
                You = (short)index["You"].AsInt
            };

            for(int i = 0; i < n; ++i)
            {
                var l = (MapType)location[i];
                var a = (MapType)agentdata[i];

                res.AgentData.Add(new AgentDataEntry
                {
                    X = l["X"].AsInt,
                    Y = l["Y"].AsInt,
                    Z = l["Z"].AsInt,
                    AgentID = a["AgentID"].AsUUID
                });
            }

            return res;
        }
    }
}

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
using SilverSim.Types.Groups;
using System;
using System.Collections.Generic;
using MapType = SilverSim.Types.Map;

namespace SilverSim.Viewer.Messages.Agent
{
    [UDPMessage(MessageType.AgentGroupDataUpdate)]
    [Reliable]
    [EventQueueGet("AgentGroupDataUpdate")]
    [Trusted]
    public class AgentGroupDataUpdate : Message
    {
        public UUID AgentID;
        public struct GroupDataEntry
        {
            public bool ListInProfile; /* <- not in UDP message */
            public UUID GroupID;
            public GroupPowers GroupPowers;
            public bool AcceptNotices;
            public UUID GroupInsigniaID;
            public Int32 Contribution;
            public string GroupName;
        }

        public List<GroupDataEntry> GroupData = new List<GroupDataEntry>();

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUInt8((byte)GroupData.Count);
            foreach(GroupDataEntry d in GroupData)
            {
                p.WriteUUID(d.GroupID);
                p.WriteUInt64((UInt64)d.GroupPowers);
                p.WriteBoolean(d.AcceptNotices);
                p.WriteUUID(d.GroupInsigniaID);
                p.WriteInt32(d.Contribution);
                p.WriteStringLen8(d.GroupName);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            var m = new AgentGroupDataUpdate
            {
                AgentID = p.ReadUUID()
            };
            uint n = p.ReadUInt8();
            for(uint i = 0; i < n; ++i)
            {
                m.GroupData.Add(new GroupDataEntry
                {
                    GroupID = p.ReadUUID(),
                    GroupPowers = (GroupPowers)p.ReadUInt64(),
                    AcceptNotices = p.ReadBoolean(),
                    GroupInsigniaID = p.ReadUUID(),
                    Contribution = p.ReadInt32(),
                    GroupName = p.ReadStringLen8()
                });
            }
            return m;
        }

        public override IValue SerializeEQG()
        {
            var agentDataMap = new MapType
            {
                ["AgentID"] = AgentID
            };
            var agentDataArray = new AnArray
            {
                agentDataMap
            };
            var body = new MapType
            {
                ["AgentData"] = agentDataArray
            };
            var groupDataArray = new AnArray();
            var newGroupDataArray = new AnArray();
            foreach(GroupDataEntry e in GroupData)
            {
                byte[] groupPowers = BitConverter.GetBytes((ulong)e.GroupPowers);
                if(BitConverter.IsLittleEndian)
                {
                    Array.Reverse(groupPowers);
                }
                groupDataArray.Add(new MapType
                {
                    { "GroupID", e.GroupID },
                    { "GroupInsigniaID", e.GroupInsigniaID },
                    { "Contribution", e.Contribution },
                    { "GroupPowers", new BinaryData(groupPowers) },
                    { "GroupName", e.GroupName },
                    { "AcceptNotices", e.AcceptNotices }
                });
                newGroupDataArray.Add(new MapType
                {
                    { "ListInProfile", e.ListInProfile },
                });
            }
            body.Add("GroupData", groupDataArray);
            body.Add("NewGroupData", newGroupDataArray);

            return body;
        }

        public static Message DeserializeEQG(IValue value)
        {
            var m = (MapType)value;
            var groupDataArray = (AnArray)m["GroupData"];
            var newGroupDataArray = (AnArray)m["NewGroupData"];
            var agentData = (MapType)((AnArray)m["AgentData"])[0];
            var res = new AgentGroupDataUpdate
            {
                AgentID = agentData["AgentID"].AsUUID
            };

            int n = Math.Min(groupDataArray.Count, newGroupDataArray.Count);

            for (int i = 0; i < n; ++i)
            {
                var groupData = (MapType)groupDataArray[i];
                var newGroupData = (MapType)newGroupDataArray[i];
                byte[] grouppowers = (BinaryData)groupData["GroupPowers"];
                if(BitConverter.IsLittleEndian)
                {
                    Array.Reverse(grouppowers);
                }

                res.GroupData.Add(new GroupDataEntry
                {
                    GroupID = groupData["GroupID"].AsUUID,
                    GroupInsigniaID = groupData["GroupInsigniaID"].AsUUID,
                    Contribution = groupData["Contribution"].AsInt,
                    GroupPowers = (GroupPowers)BitConverter.ToUInt64(grouppowers, 0),
                    GroupName = groupData["GroupName"].ToString(),
                    AcceptNotices = groupData["AcceptNotices"].AsBoolean,
                    ListInProfile = groupData["ListInProfile"].AsBoolean
                });
            }

            return res;
        }
    }
}

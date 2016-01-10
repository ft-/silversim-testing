// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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

        public AgentGroupDataUpdate()
        {

        }

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
            AgentGroupDataUpdate m = new AgentGroupDataUpdate();
            m.AgentID = p.ReadUUID();
            uint n = p.ReadUInt8();
            for(uint i = 0; i < n; ++i)
            {
                GroupDataEntry e = new GroupDataEntry();
                e.GroupID = p.ReadUUID();
                e.GroupPowers = (GroupPowers)p.ReadUInt64();
                e.AcceptNotices = p.ReadBoolean();
                e.GroupInsigniaID = p.ReadUUID();
                e.Contribution = p.ReadInt32();
                e.GroupName = p.ReadStringLen8();
                m.GroupData.Add(e);
            }
            return m;
        }

        public override IValue SerializeEQG()
        {
            MapType body = new MapType();
            AnArray agentDataArray = new AnArray();
            MapType agentDataMap = new MapType();
            agentDataMap.Add("AgentID", AgentID);
            agentDataArray.Add(agentDataMap);
            body.Add("AgentData", agentDataArray);
            AnArray groupDataArray = new AnArray();
            foreach(GroupDataEntry e in GroupData)
            {
                MapType groupData = new MapType();
                groupData.Add("ListInProfile", e.ListInProfile);
                groupData.Add("GroupID", e.GroupID);
                groupData.Add("GroupInsigniaID", e.GroupInsigniaID);
                groupData.Add("Contribution", e.Contribution);
                byte[] groupPowers = BitConverter.GetBytes((UInt64)e.GroupPowers);
                if(BitConverter.IsLittleEndian)
                {
                    Array.Reverse(groupPowers);
                }
                groupData.Add("GroupPowers", new BinaryData(groupPowers));
                groupData.Add("GroupName", e.GroupName);
                groupData.Add("AcceptNotices", e.AcceptNotices);
                groupDataArray.Add(groupData);
            }
            body.Add("GroupData", groupDataArray);

            return body;
        }
    }
}

// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Groups;
using System;
using System.Collections.Generic;
using MapType = SilverSim.Types.Map;

namespace SilverSim.Viewer.Messages.Profile
{
    [UDPMessage(MessageType.AvatarGroupsReply)]
    [Reliable]
    [Zerocoded]
    [Trusted]
    [EventQueueGet("AvatarGroupsReply")]
    public class AvatarGroupsReply : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID AvatarID = UUID.Zero;

        public struct GroupDataEntry
        {
            public GroupPowers GroupPowers;
            public bool AcceptNotices;
            public string GroupTitle;
            public UUID GroupID;
            public string GroupName;
            public UUID GroupInsigniaID;
            public bool ListInProfile;
        }

        public List<GroupDataEntry> GroupData = new List<GroupDataEntry>();
        public bool ListInProfile;

        public AvatarGroupsReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteUUID(AvatarID);
            p.WriteUInt8((byte)GroupData.Count);
            foreach(GroupDataEntry d in GroupData)
            {
                p.WriteUInt64((UInt64)d.GroupPowers);
                p.WriteBoolean(d.AcceptNotices);
                p.WriteStringLen8(d.GroupTitle);
                p.WriteUUID(d.GroupID);
                p.WriteStringLen8(d.GroupName);
                p.WriteUUID(d.GroupInsigniaID);
            }
            p.WriteBoolean(ListInProfile);
        }

        public override IValue SerializeEQG()
        {
            MapType llsd = new MapType();

            AnArray agentDataArray = new AnArray();
            MapType agentData = new MapType();
            agentData.Add("AgentID", AgentID);
            agentData.Add("AvatarID", AvatarID);
            agentDataArray.Add(agentData);
            llsd.Add("AgentData", agentDataArray);

            AnArray groupDataArray = new AnArray();
            AnArray newGroupDataArray = new AnArray();

            foreach(GroupDataEntry e in GroupData)
            {
                MapType groupData = new MapType();
                MapType newGroupData = new MapType();
                groupData.Add("GroupPowers", ((ulong)e.GroupPowers).ToString());
                groupData.Add("AcceptNotices", e.AcceptNotices);
                groupData.Add("GroupTitle", e.GroupTitle);
                groupData.Add("GroupID", e.GroupID);
                groupData.Add("GroupName", e.GroupName);
                groupData.Add("GroupInsigniaID", e.GroupInsigniaID);
                newGroupData.Add("ListInProfile", e.ListInProfile);
                groupDataArray.Add(groupData);
                newGroupDataArray.Add(newGroupData);
            }
            llsd.Add("GroupData", groupDataArray);
            llsd.Add("NewGroupData", newGroupDataArray);

            return llsd;
        }
    }
}

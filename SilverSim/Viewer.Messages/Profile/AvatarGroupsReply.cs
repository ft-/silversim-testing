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

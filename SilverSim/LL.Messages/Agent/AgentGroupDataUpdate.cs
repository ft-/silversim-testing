/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.Types;
using MapType = SilverSim.Types.Map;
using System;
using System.Collections.Generic;

namespace SilverSim.LL.Messages.Agent
{
    [UDPMessage(MessageType.AgentGroupDataUpdate)]
    [Reliable]
    [EventQueueGet("AgentGroupDataUpdate")]
    public class AgentGroupDataUpdate : Message
    {
        public UUID AgentID;
        public struct GroupDataEntry
        {
            public bool ListInProfile; /* <- not in UDP message */
            public UUID GroupID;
            public UInt64 GroupPowers;
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
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteUInt8((byte)GroupData.Count);
            foreach(GroupDataEntry d in GroupData)
            {
                p.WriteUUID(d.GroupID);
                p.WriteUInt64(d.GroupPowers);
                p.WriteBoolean(d.AcceptNotices);
                p.WriteUUID(d.GroupInsigniaID);
                p.WriteInt32(d.Contribution);
                p.WriteStringLen8(d.GroupName);
            }
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
                groupData.Add("GroupPowers", ((UInt64)e.GroupPowers).ToString());
                groupData.Add("GroupName", e.GroupName);
                groupData.Add("AcceptNotices", e.AcceptNotices);
                groupDataArray.Add(groupData);
            }
            body.Add("GroupData", groupDataArray);

            return body;
        }
    }
}

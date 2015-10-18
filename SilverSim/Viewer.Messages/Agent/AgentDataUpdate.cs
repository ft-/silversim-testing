// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Groups;
using System;

namespace SilverSim.Viewer.Messages.Agent
{
    [UDPMessage(MessageType.AgentDataUpdate)]
    [Reliable]
    [Trusted]
    public class AgentDataUpdate : Message
    {
        public UUID AgentID;
        public string FirstName = string.Empty;
        public string LastName = string.Empty;
        public string GroupTitle = string.Empty;
        public UUID ActiveGroupID = UUID.Zero;
        public GroupPowers GroupPowers;
        public string GroupName = string.Empty;

        public AgentDataUpdate()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteStringLen8(FirstName);
            p.WriteStringLen8(LastName);
            p.WriteStringLen8(GroupTitle);
            p.WriteUUID(ActiveGroupID);
            p.WriteUInt64((UInt64)GroupPowers);
            if (GroupName.Length == 0)
            {
                p.WriteUInt8(0);
            }
            else
            {
                p.WriteStringLen8(GroupName);
            }
        }
    }
}

// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Groups
{
    [UDPMessage(MessageType.GroupRoleChanges)]
    [Reliable]
    [NotTrusted]
    public class GroupRoleChanges : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID GroupID;

        public UUID RoleID;
        public UUID MemberID;

        public enum ChangeType : uint
        {
            Add = 0,
            Remove = 1
        }

        public ChangeType Change;

        public GroupRoleChanges()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            GroupRoleChanges m = new GroupRoleChanges();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            m.RoleID = p.ReadUUID();
            m.MemberID = p.ReadUUID();
            m.Change = (ChangeType)p.ReadUInt32();

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(GroupID);
            p.WriteUUID(RoleID);
            p.WriteUUID(MemberID);
            p.WriteUInt32((uint)Change);
        }
    }
}

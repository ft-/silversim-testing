// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Groups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Viewer.Messages.Groups
{
    [UDPMessage(MessageType.GroupRoleUpdate)]
    [Reliable]
    [NotTrusted]
    public class GroupRoleUpdate : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UUID GroupID = UUID.Zero;

        public enum RoleUpdateType : byte
        {
            NoUpdate = 0,
            UpdateData = 1,
            UpdatePowers = 2,
            UpdateAll = 3,
            Create = 4,
            Delete = 5
        }

        public struct RoleDataEntry
        {
            public UUID RoleID;
            public string Name;
            public string Description;
            public string Title;
            public GroupPowers Powers;
            public RoleUpdateType UpdateType;
        }

        public List<RoleDataEntry> RoleData = new List<RoleDataEntry>();

        public GroupRoleUpdate()
        {

        }

        public static GroupRoleUpdate Decode(UDPPacket p)
        {
            GroupRoleUpdate m = new GroupRoleUpdate();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.GroupID = p.ReadUUID();

            uint cnt = p.ReadUInt8();
            for(uint i = 0; i < cnt; ++i)
            {
                RoleDataEntry e = new RoleDataEntry();
                e.RoleID = p.ReadUUID();
                e.Name = p.ReadStringLen8();
                e.Description = p.ReadStringLen8();
                e.Title = p.ReadStringLen8();
                e.Powers = (GroupPowers)p.ReadUInt64();
                e.UpdateType = (RoleUpdateType)p.ReadUInt8();
                m.RoleData.Add(e);
            }
            return m;
        }
    }
}

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
using SilverSim.Types.Groups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.LL.Messages.Groups
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
            Updatedata = 1,
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

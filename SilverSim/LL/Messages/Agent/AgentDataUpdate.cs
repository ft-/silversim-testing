﻿/*

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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.LL.Messages.Agent
{
    public class AgentDataUpdate : Message
    {
        public UUID AgentID;
        public string FirstName = string.Empty;
        public string LastName = string.Empty;
        public string GroupTitle = string.Empty;
        public UUID ActiveGroupID = UUID.Zero;
        public UInt64 GroupPowers;
        public string GroupName = string.Empty;

        public AgentDataUpdate()
        {

        }

        public override MessageType Number
        {
            get
            {
                return MessageType.AgentDataUpdate;
            }
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteStringLen8(FirstName);
            p.WriteStringLen8(LastName);
            p.WriteStringLen8(GroupTitle);
            p.WriteUUID(ActiveGroupID);
            p.WriteUInt64(GroupPowers);
            p.WriteStringLen8(GroupName);
        }
    }
}

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

namespace SilverSim.BackendConnectors.Flotsam.Groups
{
    public partial class FlotsamGroupsConnector
    {
        class MembersAccessor : IGroupMembersInterface
        {
            public int TimeoutMs = 20000;
            string m_Uri;

            public MembersAccessor(string uri)
            {
                m_Uri = uri;
            }

            public GroupMember this[UUI requestingAgent, UGI group, UUI principal]
            {
                get { throw new NotImplementedException(); }
            }

            public List<GroupMember> this[UUI requestingAgent, UGI group]
            {
                get { throw new NotImplementedException(); }
            }

            public List<GroupMember> this[UUI requestingAgent, UUI principal]
            {
                get { throw new NotImplementedException(); }
            }

            public GroupMember Add(UUI requestingAgent, UGI group, UUI principal, UUID roleID, string accessToken)
            {
                throw new NotImplementedException();
            }

            public void Delete(UUI requestingAgent, UGI group, UUI principal)
            {
                throw new NotImplementedException();
            }
        }
    }
}

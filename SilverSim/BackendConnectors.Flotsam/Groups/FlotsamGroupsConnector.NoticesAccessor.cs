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

namespace SilverSim.BackendConnectors.Flotsam.Groups
{
    public partial class FlotsamGroupsConnector
    {
        class NoticesAccessor : FlotsamGroupsCommonConnector, IGroupNoticesInterface
        {
            public NoticesAccessor(string uri)
                : base(uri)
            {
            }

            public List<GroupNotice> GetNotices(UUI requestingAgent, UGI group)
            {
                Map m = new Map();
                m.Add("GroupID", group.ID);
                IValue r = FlotsamXmlRpcGetCall(requestingAgent, "groups.getGroupNotices", m);
                if(!(r is AnArray))
                {
                    throw new AccessFailedException();
                }
                List<GroupNotice> notices = new List<GroupNotice>();
                foreach(IValue iv in (AnArray)r)
                {
                    if(iv is Map)
                    {
                        notices.Add(iv.ToGroupNotice());
                    }
                }
                return notices;
            }

            public GroupNotice this[UUI requestingAgent, UUID groupNoticeID]
            {
                get 
                {
                    Map m = new Map();
                    m.Add("NoticeID", groupNoticeID);
                    IValue r = FlotsamXmlRpcGetCall(requestingAgent, "groups.getGroupNotice", m);
                    return r.ToGroupNotice();
                }
            }

            public void Add(UUI requestingAgent, GroupNotice notice)
            {
                Map m = new Map();
                m.Add("GroupID", notice.Group.ID);
                m.Add("NoticeID", notice.ID);
                m.Add("FromName", notice.FromName);
                m.Add("Subject", notice.Subject);
#warning TODO: Binary Bucket conversion
                m.Add("BinaryBucket", new BinaryData());
                m.Add("Message", notice.Message);
                m.Add("TimeStamp", notice.Timestamp.AsULong.ToString());
                FlotsamXmlRpcCall(requestingAgent, "groups.addGroupNotice", m);
            }

            public void Delete(UUI requestingAgent, UUID groupNoticeID)
            {
                throw new NotImplementedException();
            }
        }
    }
}

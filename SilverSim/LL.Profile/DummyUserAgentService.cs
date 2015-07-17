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

using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.LL.Profile
{
    class DummyUserAgentService : UserAgentServiceInterface
    {
        public DummyUserAgentService()
        {

        }


        public override void VerifyAgent(UUID sessionID, string token)
        {
            throw new NotImplementedException();
        }

        public override void VerifyClient(UUID sessionID, string token)
        {
            throw new NotImplementedException();
        }

        public override List<UUID> NotifyStatus(List<KeyValuePair<UUI, string>> friends, UUI user, bool online)
        {
            throw new NotImplementedException();
        }

        public override UserInfo GetUserInfo(UUI user)
        {
            UserInfo dummyInfo = new UserInfo();
            dummyInfo.FirstName = user.FirstName;
            dummyInfo.LastName = user.LastName;
            dummyInfo.UserTitle = "";
            dummyInfo.UserFlags = 0;
            dummyInfo.UserCreated = new Date();
            return dummyInfo;
        }

        public override Dictionary<string, string> GetServerURLs(UUI user)
        {
            throw new NotImplementedException();
        }

        public override string LocateUser(UUI user)
        {
            throw new NotImplementedException();
        }

        public override Types.UUI GetUUI(UUI user, UUI targetUserID)
        {
            throw new NotImplementedException();
        }
    }
}

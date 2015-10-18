// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Viewer.Profile
{
    sealed class DummyUserAgentService : UserAgentServiceInterface
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
            dummyInfo.UserTitle = string.Empty;
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

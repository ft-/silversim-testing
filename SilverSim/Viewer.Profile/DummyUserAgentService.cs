// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.Types;
using SilverSim.Types.Grid;
using SilverSim.Types.ServerURIs;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Profile
{
    sealed class DummyUserAgentService : UserAgentServiceInterface, IDisplayNameAccessor
    {
        public DummyUserAgentService()
        {

        }

        public override IDisplayNameAccessor DisplayName
        {
            get
            {
                return this;
            }
        }

        string IDisplayNameAccessor.this[UUI agent]
        {
            get
            {
                return string.Empty;
            }

            set
            {
                throw new NotSupportedException();
            }
        }

        public override void VerifyAgent(UUID sessionID, string token)
        {
            throw new NotSupportedException();
        }

        public override void VerifyClient(UUID sessionID, string token)
        {
            throw new NotSupportedException();
        }

        public override List<UUID> NotifyStatus(List<KeyValuePair<UUI, string>> friends, UUI user, bool online)
        {
            throw new NotSupportedException();
        }

        public override bool IsOnline(UUI user)
        {
            return false;
        }

        public override DestinationInfo GetHomeRegion(UUI user)
        {
            throw new NotSupportedException();
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

        public override ServerURIs GetServerURLs(UUI user)
        {
            throw new NotSupportedException();
        }

        public override string LocateUser(UUI user)
        {
            throw new NotSupportedException();
        }

        public override UUI GetUUI(UUI user, UUI targetUserID)
        {
            throw new NotSupportedException();
        }

        bool IDisplayNameAccessor.TryGetValue(UUI agent, out string displayname)
        {
            displayname = string.Empty;
            return false;
        }

        bool IDisplayNameAccessor.ContainsKey(UUI agent)
        {
            return false;
        }
    }
}

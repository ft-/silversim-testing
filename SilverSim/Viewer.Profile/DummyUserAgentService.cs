// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

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

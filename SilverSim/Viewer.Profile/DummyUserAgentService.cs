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
    internal sealed class DummyUserAgentService : UserAgentServiceInterface, IDisplayNameAccessor
    {
        public override IDisplayNameAccessor DisplayName => this;

        string IDisplayNameAccessor.this[UGUI agent]
        {
            get { return string.Empty; }

            set { throw new NotSupportedException(); }
        }

        public override void VerifyAgent(UUID sessionID, string token)
        {
            throw new NotSupportedException();
        }

        public override void VerifyClient(UUID sessionID, string token)
        {
            throw new NotSupportedException();
        }

        public override List<UUID> NotifyStatus(List<KeyValuePair<UGUI, string>> friends, UGUI user, bool online)
        {
            throw new NotSupportedException();
        }

        public override bool IsOnline(UGUI user)
        {
            return false;
        }

        public override DestinationInfo GetHomeRegion(UGUI user)
        {
            throw new NotSupportedException();
        }

        public override UserInfo GetUserInfo(UGUI user) => new UserInfo
        {
            FirstName = string.Empty,
            LastName = string.Empty,
            UserTitle = string.Empty,
            UserFlags = 0,
            UserCreated = new Date()
        };

        public override ServerURIs GetServerURLs(UGUI user)
        {
            throw new NotSupportedException();
        }

        public override string LocateUser(UGUI user)
        {
            throw new NotSupportedException();
        }

        public override UGUIWithName GetUUI(UGUI user, UGUI targetUserID)
        {
            throw new NotSupportedException();
        }

        bool IDisplayNameAccessor.TryGetValue(UGUI agent, out string displayname)
        {
            displayname = string.Empty;
            return false;
        }

        bool IDisplayNameAccessor.ContainsKey(UGUI agent)
        {
            return false;
        }
    }
}

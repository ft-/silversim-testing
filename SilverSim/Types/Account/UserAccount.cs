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

using SilverSim.Types.Agent;
using System.Collections.Generic;

namespace SilverSim.Types.Account
{
    public class UserAccount
    {
        public UGUIWithName Principal = UGUIWithName.Unknown;
        public UUID ScopeID = UUID.Zero;
        public string Email = string.Empty;
        public Date Created = new Date();
        public int UserLevel = -1;
        public UserFlags UserFlags;
        public string UserTitle = string.Empty;
        public bool IsLocalToGrid;
        public bool IsEverLoggedIn;
        public Dictionary<string, string> ServiceURLs = new Dictionary<string,string>(); /* only valid when IsLocalToGrid is set to false */
        public Date LastLogout = Date.Now;

        public UserRegionData LastRegion;
        public UserRegionData HomeRegion;

        public UserAccount()
        {
        }

        public UserAccount(UserAccount src)
        {
            Principal = new UGUIWithName(src.Principal);
            ScopeID = src.ScopeID;
            Email = src.Email;
            Created = src.Created;
            UserLevel = src.UserLevel;
            UserFlags = src.UserFlags;
            UserTitle = src.UserTitle;
            ServiceURLs = new Dictionary<string, string>(src.ServiceURLs);
            IsEverLoggedIn = src.IsEverLoggedIn;
            LastLogout = new Date(src.LastLogout);
            LastRegion = src.LastRegion?.Clone();
            HomeRegion = src.HomeRegion?.Clone();
        }
    }
}

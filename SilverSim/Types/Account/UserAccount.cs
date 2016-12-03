// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Collections.Generic;

namespace SilverSim.Types.Account
{
    public class UserAccount
    {
        public UUI Principal = UUI.Unknown;
        public UUID ScopeID = UUID.Zero;
        public string Email = string.Empty;
        public Date Created = new Date();
        public int UserLevel = -1;
        public uint UserFlags;
        public string UserTitle = string.Empty;
        public bool IsLocalToGrid;
        public bool IsEverLoggedIn;
        public Dictionary<string, string> ServiceURLs = new Dictionary<string,string>(); /* only valid when IsLocalToGrid is set to false */

        public UserAccount()
        {

        }

        public UserAccount(UserAccount src)
        {
            Principal = new UUI(src.Principal);
            ScopeID = src.ScopeID;
            Email = src.Email;
            Created = src.Created;
            UserLevel = src.UserLevel;
            UserFlags = src.UserFlags;
            UserTitle = src.UserTitle;
            ServiceURLs = new Dictionary<string, string>(src.ServiceURLs);
            IsEverLoggedIn = src.IsEverLoggedIn;
        }
    }
}

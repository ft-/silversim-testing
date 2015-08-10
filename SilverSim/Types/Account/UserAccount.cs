// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Collections.Generic;

namespace SilverSim.Types.Account
{
    public class UserAccount
    {
        public UUI Principal = UUI.Unknown;
        public UUID ScopeID = UUID.Zero;
        public string Email = "";
        public Date Created = new Date();
        public int UserLevel = -1;
        public int UserFlags = 0;
        public string UserTitle = "";
        public bool IsLocalToGrid = false;
        public Dictionary<string, string> ServiceURLs = new Dictionary<string,string>(); /* only valid when IsLocalToGrid is set to false */

        public UserAccount()
        {

        }
    }
}

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

using System.Collections.Generic;

namespace SilverSim.Types.UserSession
{
    public sealed class UserSessionInfo
    {
        public UGUI User = UGUI.Unknown;
        public UUID SessionID = UUID.Zero;
        public UUID SecureSessionID = UUID.Zero;
        public string ClientIPAddress = string.Empty;
        /* key is <protocol-association>/<variable-name> */
        public readonly Dictionary<string, string> DynamicData = new Dictionary<string, string>();
        public Date Timestamp = Date.Now;

        public UserSessionInfo()
        {
        }

        public UserSessionInfo(UserSessionInfo src)
        {
            User = new UGUI(src.User);
            SessionID = src.SessionID;
            ClientIPAddress = src.ClientIPAddress;
            SecureSessionID = src.SecureSessionID;
            foreach(KeyValuePair<string, string> kvp in src.DynamicData)
            {
                DynamicData.Add(kvp.Key, kvp.Value);
            }
        }
    }
}

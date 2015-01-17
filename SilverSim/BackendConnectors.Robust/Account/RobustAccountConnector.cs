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

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.Types.Account;
using SilverSim.Types;
using SilverSim.Main.Common;
using SilverSim.BackendConnectors.Robust.Common;
using HttpClasses;
using log4net;
using Nini.Config;

namespace SilverSim.BackendConnectors.Robust.Account
{
    #region Service Implementation
    class RobustAccountConnector : UserAccountServiceInterface, IPlugin
    {
        string m_UserAccountURI;
        public int TimeoutMs { get; set; }

        #region Constructor
        public RobustAccountConnector(string uri)
        {
            if(!uri.EndsWith("/"))
            {
                uri += "/";
            }
            uri += "accounts";
            m_UserAccountURI = uri;
            TimeoutMs = 20000;
        }

        public void Startup(ConfigurationLoader loader)
        {

        }
        #endregion

        public override UserAccount this[UUID scopeID, UUID accountID]
        {
            get
            {
                Dictionary<string, string> post = new Dictionary<string, string>();
                post["UserID"] = accountID;
                post["SCOPEID"] = scopeID;
                post["METHOD"] = "getaccount";
                Map map = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_UserAccountURI, null, post, false, TimeoutMs));
                if (!(map["result"] is Map))
                {
                    throw new UserAccountNotFoundException();
                }
                return DeserializeEntry((Map)(map["result"]));
            }
        }

        public override UserAccount this[UUID scopeID, string email]
        {
            get
            {
                Dictionary<string, string> post = new Dictionary<string, string>();
                post["Email"] = email;
                post["SCOPEID"] = scopeID;
                post["METHOD"] = "getaccount";
                Map map = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_UserAccountURI, null, post, false, TimeoutMs));
                if (!(map["result"] is Map))
                {
                    throw new UserAccountNotFoundException();
                }
                return DeserializeEntry((Map)(map["result"]));
            }
        }

        public override UserAccount this[UUID scopeID, string firstName, string lastName]
        {
            get
            {
                Dictionary<string, string> post = new Dictionary<string, string>();
                post["FirstName"] = firstName;
                post["LastName"] = lastName;
                post["SCOPEID"] = scopeID;
                post["METHOD"] = "getaccount";
                Map map = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_UserAccountURI, null, post, false, TimeoutMs));
                if (!(map["result"] is Map))
                {
                    throw new UserAccountNotFoundException();
                }
                return DeserializeEntry((Map)(map["result"]));
            }
        }

        public override List<UserAccount> GetAccounts(UUID scopeID, string query)
        {
            Dictionary<string, string> post = new Dictionary<string, string>();
            post["query"] = query;
            post["SCOPEID"] = scopeID;
            post["METHOD"] = "getaccounts";
            Map map = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_UserAccountURI, null, post, false, TimeoutMs));
            if (!(map["result"] is Map))
            {
                throw new UserAccountNotFoundException();
            }
            List<UserAccount> res = new List<UserAccount>();

            foreach(IValue i in ((Map)map["result"]).Values)
            {
                Map m = (Map)i;
                res.Add(DeserializeEntry(m));
            }
            return res;
        }

        private UserAccount DeserializeEntry(Map m)
        {
            UserAccount ua = new UserAccount();

            ua.Principal.FirstName = m["FirstName"].ToString();
            ua.Principal.LastName = m["LastName"].ToString();
            ua.Email = m["Email"].ToString();
            ua.Principal.ID = m["PrincipalID"].ToString();
            ua.ScopeID = m["ScopeID"].ToString();
            ua.Created = Date.UnixTimeToDateTime(m["Created"].AsULong);
            ua.UserLevel = int.Parse(m["UserLevel"].ToString());
            ua.UserFlags = int.Parse(m["UserFlags"].ToString());
            ua.IsLocalToGrid = true;
            string serviceURLs = "";
            if(m.ContainsKey("ServiceURLs"))
            {
                serviceURLs = m["ServiceURLs"].ToString();
            }

            foreach(string p in serviceURLs.Split(';'))
            {
                string[] pa = p.Split(new char[] {'*'}, 2, StringSplitOptions.RemoveEmptyEntries);
                if(pa.Length < 2)
                {
                    continue;
                }
                ua.ServiceURLs[pa[0]] = pa[1];
            }
            return ua;
        }
    }
    #endregion

    #region Factory
    [PluginName("UserAccounts")]
    public class RobustAccountConnectorFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("ROBUST ACCOUNT CONNECTOR");
        public RobustAccountConnectorFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            if (!ownSection.Contains("URI"))
            {
                m_Log.FatalFormat("Missing 'URI' in section {0}", ownSection.Name);
                throw new ConfigurationLoader.ConfigurationError();
            }
            return new RobustAccountConnector(ownSection.GetString("URI"));
        }
    }
    #endregion
}

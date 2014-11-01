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

using HttpClasses;
using log4net;
using Nini.Config;
using SilverSim.BackendConnectors.Robust.Common;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.BackendConnectors.Robust.AvatarName
{
    #region Service Implementation
    class RobustAccountAvatarNameConnector : AvatarNameServiceInterface, IPlugin
    {
        string m_UserAccountURI;
        string m_HomeURI;
        UUID m_ScopeID;
        public int TimeoutMs { get; set; }

        #region Constructor
        public RobustAccountAvatarNameConnector(string uri, string homeURI, UUID scopeID)
        {
            m_ScopeID = scopeID;
            if(!uri.EndsWith("/"))
            {
                uri += "/";
            }
            uri += "accounts";
            m_UserAccountURI = uri;
            TimeoutMs = 20000;
            m_HomeURI = homeURI;
        }

        public void Startup(ConfigurationLoader loader)
        {

        }
        #endregion

        public override NameData this[UUID accountID]
        {
            get
            {
                Dictionary<string, string> post = new Dictionary<string, string>();
                post["UserID"] = accountID;
                post["SCOPEID"] = m_ScopeID;
                post["METHOD"] = "getaccount";
                Map map = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_UserAccountURI, null, post, false, TimeoutMs));
                if (!(map["result"] is Map))
                {
                    throw new KeyNotFoundException();
                }

                Map m = (Map)(map["result"]);
                NameData nd = new NameData();
                nd.ID.FirstName = m["FirstName"].ToString();
                nd.ID.LastName = m["LastName"].ToString();
                nd.ID.ID = m["PrincipalID"].ToString();
                nd.ID.HomeURI = new Uri(m_HomeURI);
                nd.Authoritative = true;
                return nd;
            }
            set
            {

            }
        }
    }
    #endregion

    #region Factory
    public class RobustAccountAvatarNameConnectorFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("ROBUST ACCOUNT AVATAR NAME CONNECTOR");
        public RobustAccountAvatarNameConnectorFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            if (!ownSection.Contains("URI"))
            {
                m_Log.FatalFormat("Missing 'URI' in section {0}", ownSection.Name);
                throw new ConfigurationLoader.ConfigurationError();
            }
            if (!ownSection.Contains("HomeURI"))
            {
                m_Log.FatalFormat("Missing 'HomeURI' in section {0}", ownSection.Name);
                throw new ConfigurationLoader.ConfigurationError();
            }
            return new RobustAccountAvatarNameConnector(ownSection.GetString("URI"), ownSection.GetString("HomeURI"), ownSection.GetString("ScopeID", UUID.Zero));
        }
    }
    #endregion
}

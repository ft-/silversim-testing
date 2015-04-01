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

using log4net;
using Nini.Config;
using SilverSim.BackendConnectors.Robust.Common;
using SilverSim.HttpClient;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.Types;
using SilverSim.Types.Presence;
using System;
using System.Collections.Generic;

namespace SilverSim.BackendConnectors.Robust.Presence
{
    #region Service Implementation
    public class RobustPresenceConnector : PresenceServiceInterface, IPlugin
    {
        public int TimeoutMs { get; set; }
        string m_PresenceUri;
        string m_HomeURI;

        #region Constructor
        public RobustPresenceConnector(string uri, string homeuri)
        {
            TimeoutMs = 20000;
            if (!uri.EndsWith("/"))
            {
                uri += "/";
            }
            uri += "presence";
            m_PresenceUri = uri;
            m_HomeURI = homeuri;
        }

        public void Startup(ConfigurationLoader loader)
        {
        }
        #endregion

        public override PresenceInfo this[UUID sessionID, UUID userID]
        {
            get
            {
                Dictionary<string, string> post = new Dictionary<string, string>();
                post["SessionID"] = sessionID;
                post["METHOD"] = "getagent";

                Map map = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_PresenceUri, null, post, false, TimeoutMs));
                if(!(map["result"] is Map))
                {
                    throw new PresenceNotFoundException();
                }
                PresenceInfo p = new PresenceInfo();
                Map m = (Map)(map["result"]);
                p.RegionID = m["RegionID"].ToString();
                p.UserID.ID = m["UserID"].ToString();
                p.UserID.HomeURI = new Uri(m_HomeURI);
                p.SessionID = sessionID;
                return p;
            }
            set
            {
                if(value == null)
                {
                    Dictionary<string, string> post = new Dictionary<string, string>();
                    post["SessionID"] = sessionID;
                    post["METHOD"] = "logout";

                    Map map = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_PresenceUri, null, post, false, TimeoutMs));
                    if (!map.ContainsKey("result"))
                    {
                        throw new PresenceUpdateFailedException();
                    }
                    if (map["result"].ToString() != "Success")
                    {
                        throw new PresenceUpdateFailedException();
                    }
                }
                else
                {
                    throw new ArgumentException("setting value != null is not allowed without reportType");
                }
            }
        }

        public override PresenceInfo this[UUID sessionID, UUID userID, SetType reportType]
        {
            set
            {
                if (value == null)
                {
                    Dictionary<string, string> post = new Dictionary<string, string>();
                    post["SessionID"] = sessionID;
                    post["METHOD"] = "logout";

                    Map map = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_PresenceUri, null, post, false, TimeoutMs));
                    if (!map.ContainsKey("result"))
                    {
                        throw new PresenceUpdateFailedException();
                    }
                    if (map["result"].ToString() != "Success")
                    {
                        throw new PresenceUpdateFailedException();
                    }
                }
                else if(reportType == SetType.Login)
                {
                    Dictionary<string, string> post = new Dictionary<string, string>();
                    post["UserID"] = value.UserID;
                    post["SessionID"] = value.SessionID;
                    post["METHOD"] = "login";

                    Map map = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_PresenceUri, null, post, false, TimeoutMs));
                    if (!map.ContainsKey("result"))
                    {
                        throw new PresenceUpdateFailedException();
                    }
                    if (map["result"].ToString() != "Success")
                    {
                        throw new PresenceUpdateFailedException();
                    }
                }
                else if(reportType == SetType.Report)
                {
                    Dictionary<string, string> post = new Dictionary<string, string>();
                    post["SessionID"] = value.SessionID;
                    post["RegionID"] = value.RegionID;

                    Map map = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_PresenceUri, null, post, false, TimeoutMs));
                    if (!map.ContainsKey("result"))
                    {
                        throw new PresenceUpdateFailedException();
                    }
                    if (map["result"].ToString() != "Success")
                    {
                        throw new PresenceUpdateFailedException();
                    }
                }
                else
                {
                    throw new ArgumentException("Invalid reportType specified");
                }
            }
        }

        public override void logoutRegion(UUID regionID)
        {
            Dictionary<string, string> post = new Dictionary<string, string>();
            post["RegionID"] = regionID;
            post["METHOD"] = "logoutregion";

            Map map = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_PresenceUri, null, post, false, TimeoutMs));
            if (!map.ContainsKey("result"))
            {
                throw new PresenceLogoutRegionFailedException();
            }
            if (map["result"].ToString() != "Success")
            {
                throw new PresenceLogoutRegionFailedException();
            }
        }
    }
    #endregion

    #region Factory
    [PluginName("Presence")]
    public class RobustPresenceConnectorFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("ROBUST PRESENCE CONNECTOR");
        public RobustPresenceConnectorFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            if (!ownSection.Contains("URI") || !ownSection.Contains("HomeURI"))
            {
                if (!ownSection.Contains("URI"))
                {
                    m_Log.FatalFormat("Missing 'URI' in section {0}", ownSection.Name);
                }

                if (!ownSection.Contains("HomeURI"))
                {
                    m_Log.FatalFormat("Missing 'HomeURI' in section {0}", ownSection.Name);
                }

                throw new ConfigurationLoader.ConfigurationError();
            }

            string homeURI = ownSection.GetString("HomeURI");
            if(!Uri.IsWellFormedUriString(homeURI, UriKind.Absolute))
            {
                m_Log.FatalFormat("Invalid 'HomeURI' in section {0}", ownSection.Name);
                throw new ConfigurationLoader.ConfigurationError();
            }

            return new RobustPresenceConnector(ownSection.GetString("URI"), homeURI);
        }
    }
    #endregion
}

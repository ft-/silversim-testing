﻿/*

ArribaSim is distributed under the terms of the
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

using ArribaSim.BackendConnectors.Robust.Common;
using ArribaSim.Main.Common;
using ArribaSim.ServiceInterfaces.GridUser;
using ArribaSim.Types;
using ArribaSim.Types.GridUser;
using HttpClasses;
using log4net;
using Nini.Config;
using System.Collections.Generic;
using System.Reflection;

namespace ArribaSim.BackendConnectors.Robust.GridUser
{
    #region Service Implementation
    class RobustGridUserConnector : GridUserServiceInterface, IPlugin
    {
        public int TimeoutMs { get; set; }
        string m_GridUserURI;

        #region Constructor
        public RobustGridUserConnector(string uri)
        {
            TimeoutMs = 20000;
            if (!uri.EndsWith("/"))
            {
                uri += "/";
            }
            uri += "griduser";

            m_GridUserURI = uri;
        }

        public void Startup(ConfigurationLoader loader)
        {

        }
        #endregion

        private GridUserInfo fromResult(Map map)
        {
            GridUserInfo info = new GridUserInfo();
            info.UserID = new UUI(map["UserID"].ToString());
            info.HomeRegionID = map["HomeRegionID"].ToString();
            info.HomePosition = new Vector3(map["HomePosition"].ToString());
            info.HomeLookAt = new Vector3(map["HomeLookAt"].ToString());
            info.LastRegionID = map["LastRegionID"].ToString();
            info.LastPosition = new Vector3(map["LastPosition"].ToString());
            info.LastLookAt = new Vector3(map["LastLookAt"].ToString());
            info.Online = map["Online"].AsBoolean;
            /* TODO: */
            //info.LastLogin = //$xmlout.="<Login>".strftime("%F %T", intval($this->Login))."</Login>";
            //info.LastLogout = //$xmlout.="<Logout>".strftime("%F %T", intval($this->Logout))."</Logout>";
            return info;
        }

        private GridUserInfo GetUserInfo(string userID)
        {
            Dictionary<string, string> post = new Dictionary<string, string>();
            post["UserID"] = userID;
            post["METHOD"] = "getgriduserinfo";
            Map map = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_GridUserURI, null, post, false, TimeoutMs));
            if (!map.ContainsKey("result"))
            {
                throw new GridUserNotFoundException();
            }
            if (!(map["result"] is Map))
            {
                throw new GridUserNotFoundException();
            }
            return fromResult((Map)map["result"]);
        }

        public override GridUserInfo this[UUID userID]
        {
            get
            {
                return GetUserInfo(userID);
            }
        }
        public override GridUserInfo this[UUI userID]
        {
            get
            {
                return GetUserInfo(userID.ToString());
            }
        }

        private void checkResult(Map map)
        {
            if (!map.ContainsKey("result"))
            {
                throw new GridUserUpdateFailedException();
            }
            if (!map["result"].AsBoolean)
            {
                throw new GridUserUpdateFailedException();
            }
        }

        public override void LoggedIn(UUI userID)
        {
            Dictionary<string, string> post = new Dictionary<string, string>();
            post["UserID"] = userID;
            post["METHOD"] = "loggedin";
            checkResult(OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_GridUserURI, null, post, false, TimeoutMs)));
        }

        public override void LoggedOut(UUI userID, UUID lastRegionID, Vector3 lastPosition, Vector3 lastLookAt)
        {
            Dictionary<string, string> post = new Dictionary<string, string>();
            post["UserID"] = userID;
            post["RegionID"] = lastRegionID.ToString();
            post["Position"] = lastPosition.ToString();
            post["LookAt"] = lastLookAt.ToString();
            post["METHOD"] = "loggedout";
            checkResult(OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_GridUserURI, null, post, false, TimeoutMs)));
        }

        public override void SetHome(UUI userID, UUID homeRegionID, Vector3 homePosition, Vector3 homeLookAt)
        {
            Dictionary<string, string> post = new Dictionary<string, string>();
            post["UserID"] = userID;
            post["RegionID"] = homeRegionID.ToString();
            post["Position"] = homePosition.ToString();
            post["LookAt"] = homeLookAt.ToString();
            post["METHOD"] = "sethome";
            checkResult(OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_GridUserURI, null, post, false, TimeoutMs)));
        }

        public override void SetPosition(UUI userID, UUI lastRegionID, Vector3 lastPosition, Vector3 lastLookAt)
        {
            Dictionary<string, string> post = new Dictionary<string, string>();
            post["UserID"] = userID;
            post["RegionID"] = lastRegionID.ToString();
            post["Position"] = lastPosition.ToString();
            post["LookAt"] = lastLookAt.ToString();
            post["METHOD"] = "setposition";
            checkResult(OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_GridUserURI, null, post, false, TimeoutMs)));
        }
    }
    #endregion

    #region Factory
    public class RobustGridUserConnectorFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public RobustGridUserConnectorFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            if (!ownSection.Contains("URI"))
            {
                m_Log.FatalFormat("Missing 'URI' in section {0}", ownSection.Name);
                throw new ConfigurationLoader.ConfigurationError();
            }
            return new RobustGridUserConnector(ownSection.GetString("URI"));
        }
    }
    #endregion
}

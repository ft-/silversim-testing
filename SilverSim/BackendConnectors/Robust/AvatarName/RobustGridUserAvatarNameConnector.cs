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
using System.Collections.Generic;

namespace SilverSim.BackendConnectors.Robust.AvatarName
{
    #region Service Implementation
    class RobustGridUserAvatarNameConnector : AvatarNameServiceInterface, IPlugin
    {
        public int TimeoutMs { get; set; }
        string m_GridUserURI;

        #region Constructor
        public RobustGridUserAvatarNameConnector(string uri)
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

        private NameData fromResult(Map map)
        {
            NameData info = new NameData();
            UUI uui = new UUI(map["UserID"].ToString());
            info.Authoritative = null != uui.HomeURI;
            info.ID = uui;
            return info;
        }

        public override NameData this[UUID userID]
        {
            get
            {
                Dictionary<string, string> post = new Dictionary<string, string>();
                post["UserID"] = userID;
                post["METHOD"] = "getgriduserinfo";
                Map map = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_GridUserURI, null, post, false, TimeoutMs));
                if (!map.ContainsKey("result"))
                {
                    throw new KeyNotFoundException();
                }
                if (!(map["result"] is Map))
                {
                    throw new KeyNotFoundException();
                }
                return fromResult((Map)map["result"]);
            }
            set
            {

            }
        }
    }
    #endregion

    #region Factory
    public class RobustGridUserAvatarNameConnectorFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("ROBUST GRIDUSER AVATAR NAME CONNECTOR");
        public RobustGridUserAvatarNameConnectorFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            if (!ownSection.Contains("URI"))
            {
                m_Log.FatalFormat("Missing 'URI' in section {0}", ownSection.Name);
                throw new ConfigurationLoader.ConfigurationError();
            }
            return new RobustGridUserAvatarNameConnector(ownSection.GetString("URI"));
        }
    }
    #endregion
}

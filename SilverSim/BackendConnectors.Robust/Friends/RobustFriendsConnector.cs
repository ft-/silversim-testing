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
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpClient;
using SilverSim.ServiceInterfaces.Friends;
using SilverSim.Types;
using SilverSim.Types.Friends;
using System.Collections.Generic;

namespace SilverSim.BackendConnectors.Robust.Friends
{
    class RobustFriendsConnector : FriendsServiceInterface, IPlugin
    {
        string m_Uri;
        public int TimeoutMs = 20000;

        public RobustFriendsConnector(string uri)
        {
            if (!uri.EndsWith("/"))
            {
                uri += "/";
            }
            uri += "friends";
            m_Uri = uri;
        }

        private void checkResult(Map map)
        {
            if (!map.ContainsKey("Result"))
            {
                throw new FriendUpdateFailedException();
            }
            if (map["Result"].ToString().ToLower() != "success")
            {
                throw new FriendUpdateFailedException();
            }
        }

        public override FriendInfo this[UUI user, UUI friend]
        {
            get 
            {
                List<FriendInfo> filist = this[user];
                foreach(FriendInfo fi in filist)
                {
                    if(fi.Friend.Equals(friend))
                    {
                        return fi;
                    }
                }
                throw new KeyNotFoundException();
            }
        }

        public override List<FriendInfo> this[UUI user]
        {
            get 
            {
                List<FriendInfo> reslist = new List<FriendInfo>();
                Dictionary<string, string> post = new Dictionary<string, string>();
                post["METHOD"] = "getfriends_string";
                post["PRINCIPALID"] = user.ID;

                Map res = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_Uri, null, post, false, TimeoutMs));
                foreach(KeyValuePair<string, IValue> kvp in res)
                {
                    if(kvp.Key.StartsWith("friend"))
                    {
                        Map m = (Map)kvp.Value;
                        FriendInfo fi = new FriendInfo();
                        fi.User = user;
                        try
                        {
                            string friend = m["Friend"].ToString();
                            string[] parts = friend.Split(';');
                            if (parts.Length > 3)
                            {
                                /* fourth part is secret */
                                fi.Secret = parts[3];
                                fi.Friend.FullName = parts[0] + ";" + parts[1] + ";" + parts[2];
                            }
                            else
                            {
                                fi.Friend.FullName = friend;
                            }

                            fi.UserGivenFlags = m["MyFlags"].AsInt;
                            fi.FriendGivenFlags = m["TheirFlags"].AsInt;
                        }
                        catch
                        {

                        }
                    }
                }
                return reslist;
            }
        }

        public override void Store(FriendInfo fi)
        {
            Dictionary<string, string> post = new Dictionary<string, string>();
            post["METHOD"] = "storefriend";
            post["PrincipalID"] = fi.User.ID;
            post["Friend"] = fi.Friend.FullName;
            if(fi.Friend.HomeURI != null)
            {
                post["Friend"] += ";" + fi.Secret;
            }
            post["MyFlags"] = fi.UserGivenFlags.ToString();

            checkResult(OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_Uri, null, post, false, TimeoutMs)));
        }

        public override void Delete(FriendInfo fi)
        {
            Dictionary<string, string> post = new Dictionary<string, string>();
            post["METHOD"] = "deletefriend_string";
            post["PRINCIPALID"] = fi.User.ID;
            post["FRIEND"] = fi.Friend.FullName;
            if (fi.Friend.HomeURI != null)
            {
                post["FRIEND"] += ";" + fi.Secret;
            }

            checkResult(OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_Uri, null, post, false, TimeoutMs)));
        }

        public void Startup(ConfigurationLoader loader)
        {
        }
    }

    #region Factory
    [PluginName("Friends")]
    public class RobustFriendsConnectorFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("ROBUST FRIENDS CONNECTOR");
        public RobustFriendsConnectorFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            if (!ownSection.Contains("URI"))
            {
                m_Log.FatalFormat("Missing 'URI' in section {0}", ownSection.Name);
                throw new ConfigurationLoader.ConfigurationError();
            }
            return new RobustFriendsConnector(ownSection.GetString("URI"));
        }
    }
    #endregion

}

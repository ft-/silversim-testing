using Nwc.XmlRpc;
using SilverSim.Main.Common.Rpc;
using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.BackendConnectors.Robust.UserAgent
{
    public class RobustUserAgentConnector : UserAgentServiceInterface
    {
        public int TimeoutMs = 20000;
        string m_Uri;

        public RobustUserAgentConnector(string uri)
        {
            m_Uri = uri;
        }

        public override void VerifyAgent(UUID sessionID, string token)
        {
            Hashtable hash = new Hashtable();
            hash["sessionID"] = sessionID.ToString();
            hash["token"] = token;
            DoXmlRpcWithBoolResponse("verify_agent", hash);
        }

        public override void VerifyClient(UUID sessionID, string token)
        {
            Hashtable hash = new Hashtable();
            hash["sessionID"] = sessionID.ToString();
            hash["token"] = token;
            DoXmlRpcWithBoolResponse("verify_client", hash);
        }

        public override List<UUID> NotifyStatus(List<KeyValuePair<UUI, string>> friends, UUI user, bool online)
        {
            Hashtable hash = new Hashtable();
            hash["userID"] = user.ID.ToString();
            hash["online"] = online.ToString();
            int i = 0;
            foreach(KeyValuePair<UUI, string> s in friends)
            {
                hash["friend_" + i.ToString()] = s.Key.ToString() + ";" + s.Value;
                ++i;
            }

            Hashtable res = DoXmlRpcWithHashResponse("status_notification", hash);

            List<UUID> friendsOnline = new List<UUID>();

            foreach(object key in hash.Keys)
            {
                if(key is string && ((string)key).StartsWith("friend_") && hash[key] != null)
                {
                    UUID friend;
                    if(UUID.TryParse(hash[key].ToString(), out friend))
                    {
                        friendsOnline.Add(friend);
                    }
                }
            }

            return friendsOnline;
        }

        public override Dictionary<string, object> GetUserInfo(UUI user)
        {
            Hashtable hash = new Hashtable();
            hash["userID"] = user.ID.ToString();

            Hashtable res = DoXmlRpcWithHashResponse("get_user_info", hash);
            Dictionary<string, object> info = new Dictionary<string, object>();
            foreach(object key in hash.Keys)
            {
                if(hash[key] != null)
                {
                    info.Add(key.ToString(), hash[key]);
                }
            }

            return info;
        }

        public override Dictionary<string, string> GetServerURLs(UUI user)
        {
            Hashtable hash = new Hashtable();
            hash["userID"] = user.ID.ToString();

            Hashtable res = DoXmlRpcWithHashResponse("get_server_urls", hash);
            Dictionary<string, string> serverUrls = new Dictionary<string, string>();
            foreach(object key in hash.Keys)
            {
                if(key is string && ((string)key).StartsWith("SRV_") && hash[key] != null)
                {
                    string serverType = key.ToString().Substring(4);
                    serverUrls.Add(serverType, hash[key].ToString());
                }
            }

            return serverUrls;
        }

        public override string LocateUser(UUI user)
        {
            Hashtable hash = new Hashtable();
            hash["userID"] = user.ID.ToString();

            Hashtable res = DoXmlRpcWithHashResponse("locate_user", hash);

            if(hash.ContainsKey("URL"))
            {
                return hash["URL"].ToString();
            }

            throw new KeyNotFoundException();
        }

        public override UUI GetUUI(UUI user, UUI targetUserID)
        {
            Hashtable hash = new Hashtable();
            hash["userID"] = user.ID.ToString();
            hash["targetUserID"] = targetUserID.ID.ToString();

            Hashtable res = DoXmlRpcWithHashResponse("get_uui", hash);

            if (hash.ContainsKey("UUI"))
            {
                return new UUI(hash["UUI"].ToString());
            }

            throw new KeyNotFoundException();
        }

        void DoXmlRpcWithBoolResponse(string method, Hashtable reqparams)
        {
            IList paramList = new ArrayList();
            paramList.Add(reqparams);
            XmlRpcRequest req = new XmlRpcRequest(method, paramList);
            XmlRpcResponse res = RPC.DoXmlRpcRequest(m_Uri, req, TimeoutMs);

            Hashtable hash = (Hashtable)res.Value;
            if(hash == null)
            {
                throw new InvalidOperationException();
            }

            bool success = false;
            if (hash.ContainsKey("result"))
            {
                success = Boolean.Parse((string)hash["result"]);
                if(!success)
                {
                    throw new RequestFailedException();
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        Hashtable DoXmlRpcWithHashResponse(string method, Hashtable reqparams)
        {
            IList paramList = new ArrayList();
            paramList.Add(reqparams);
            XmlRpcRequest req = new XmlRpcRequest(method, paramList);
            XmlRpcResponse res = RPC.DoXmlRpcRequest(m_Uri, req, TimeoutMs);

            Hashtable hash = (Hashtable)res.Value;
            if (hash == null)
            {
                throw new InvalidOperationException();
            }

            return hash;
        }
    }
}

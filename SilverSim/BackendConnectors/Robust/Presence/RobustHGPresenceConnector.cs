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
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.Types;
using SilverSim.Types.Presence;
using System;
using System.Collections;
using System.Collections.Generic;
using Nwc.XmlRpc;

namespace SilverSim.BackendConnectors.Robust.Presence
{
    public class RobustHGPresenceConnector : PresenceServiceInterface
    {
        private static readonly XmlRpcDeserializer m_XmlRpcDeserializer = new XmlRpcDeserializer();

        public int TimeoutMs { get; set; }
        string m_PresenceUri;
        string m_HomeURI;
        RobustPresenceConnector m_LocalConnector;

        #region Constructor
        public RobustHGPresenceConnector(string uri, string homeuri)
        {
            TimeoutMs = 20000;
            m_LocalConnector = new RobustPresenceConnector(uri, homeuri);
            if (!uri.EndsWith("/"))
            {
                uri += "/";
            }
            uri += "presence";
            m_PresenceUri = uri;
            m_HomeURI = homeuri;
        }
        #endregion

        void HGLogout(UUID sessionID, UUID userId)
        {
            Dictionary<string, object> p = new Dictionary<string, object>();
            p["userID"] = userId.ToString();
            p["sessionID"] = sessionID.ToString();

            List<object> plist = new List<object>();
            plist.Add(p);
            XmlRpcRequest req = new XmlRpcRequest("logout_agent", plist);

            XmlRpcResponse res = (XmlRpcResponse)m_XmlRpcDeserializer.Deserialize(HttpRequestHandler.DoRequest("POST", m_HomeURI, null, "text/xml", req.ToString(), false, TimeoutMs));
            if (res.IsFault)
            {
                throw new PresenceUpdateFailedException();
            }
            if (res.Value is IDictionary)
            {
                IDictionary d = (IDictionary)res.Value;
                if (bool.Parse(d["result"].ToString()))
                {
                    return;
                }
            }
            throw new PresenceUpdateFailedException();
        }

        public override PresenceInfo this[UUID sessionID, UUID userID]
        {
            get
            {
                return m_LocalConnector[sessionID, userID];
            }
            set
            {
                if(value == null)
                {
                    try
                    {
                        m_LocalConnector[sessionID, userID] = null;
                    }
                    catch
                    {

                    }
                    HGLogout(sessionID, userID);
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
                    try
                    {
                        m_LocalConnector[sessionID, userID, reportType] = null;
                    }
                    catch
                    {

                    }
                    HGLogout(sessionID, userID);
                }
                else if(reportType == SetType.Login)
                {
                }
                else if(reportType == SetType.Report)
                {
                }
                else
                {
                    throw new ArgumentException("Invalid reportType specified");
                }
            }
        }

        public override void logoutRegion(UUID regionID)
        {
        }
    }
}

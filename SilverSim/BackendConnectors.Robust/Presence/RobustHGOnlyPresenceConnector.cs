﻿/*

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

using SilverSim.Main.Common.HttpClient;
using SilverSim.Main.Common.Rpc;
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.Types;
using SilverSim.Types.Presence;
using SilverSim.Types.StructuredData.XMLRPC;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SilverSim.BackendConnectors.Robust.Presence
{
    public class RobustHGOnlyPresenceConnector : PresenceServiceInterface
    {
        public int TimeoutMs { get; set; }
        string m_HomeURI;

        #region Constructor
        public RobustHGOnlyPresenceConnector(string homeuri)
        {
            TimeoutMs = 20000;
            m_HomeURI = homeuri;
        }
        #endregion

        void HGLogout(UUID sessionID, UUID userId)
        {
            Map p = new Map();
            p.Add("userID", userId);
            p.Add("sessionID", sessionID);

            XMLRPC.XmlRpcRequest req = new XMLRPC.XmlRpcRequest("logout_agent");
            req.Params.Add(p);
            XMLRPC.XmlRpcResponse res;
            try
            {
                res = RPC.DoXmlRpcRequest(m_HomeURI, req, TimeoutMs);
            }
            catch
            {
                throw new PresenceUpdateFailedException();
            }
            if (res.ReturnValue is Map)
            {
                Map d = (Map)res.ReturnValue;
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
                throw new NotImplementedException();
            }
            set
            {
                if(value == null)
                {
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

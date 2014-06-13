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
using ArribaSim.ServiceInterfaces.IM;
using ArribaSim.Types;
using ArribaSim.Types.IM;
using HttpClasses;
using log4net;
using Nini.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ArribaSim.BackendConnectors.Robust.IM
{
    #region Service Implementation
    public class RobustOfflineIMConnector : OfflineIMServiceInterface, IPlugin
    {
        public int TimeoutMs { get; set; }
        string m_OfflineIMURI;
        public RobustOfflineIMConnector(string uri)
        {
            TimeoutMs = 20000;
            if (!uri.EndsWith("/"))
            {
                uri += "/";
            }
            uri += "offlineim";
            m_OfflineIMURI = uri;
        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        private static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
        public override void storeOfflineIM(GridInstantMessage im)
        {
            Dictionary<string, string> post = new Dictionary<string, string>();
            post["BinaryBucket"] = BitConverter.ToString(im.BinaryBucket).Replace("-", string.Empty);
            post["Dialog"] = ((int)im.Dialog).ToString();
            post["FromAgentID"] = im.FromAgent.ID;
            post["FromAgentName"] = im.FromAgent.FullName;
            bool isFromGroup = !im.IsFromGroup.Equals(UUID.Zero);
            post["FromGroup"] = isFromGroup.ToString();
            post["Message"] = im.Message;
            post["EstateID"] = im.ParentEstateID.ToString();
            post["Position"] = im.Position;
            post["RegionID"] = im.RegionID;
            post["Timestamp"] = im.Timestamp.DateTimeToUnixTime().ToString();
            post["ToAgentID"] = im.ToAgent.ID;
            Map map = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_OfflineIMURI, null, post, false, TimeoutMs));
            if (!map.ContainsKey("RESULT"))
            {
                throw new IMOfflineStoreFailedException();
            }
            if (map["RESULT"].ToString().ToLower() == "false")
            {
                throw new IMOfflineStoreFailedException(map.ContainsKey("REASON") ? map["REASON"].ToString() : "Unknown Error");
            }
        }

        public override List<GridInstantMessage> getOfflineIMs(UUID principalID)
        {
            Dictionary<string, string> post = new Dictionary<string, string>();
            post["PrincipalID"] = principalID;
            post["METHOD"] = "GET";
            Map map = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_OfflineIMURI, null, post, false, TimeoutMs));
            if (!map.ContainsKey("RESULT"))
            {
                throw new IMOfflineStoreFailedException();
            }
            if (!(map["RESULT"] is Map) || map["RESULT"].ToString().ToLower() == "false")
            {
                throw new IMOfflineStoreFailedException(map.ContainsKey("REASON") ? map["REASON"].ToString() : "Unknown Error");
            }
            List<GridInstantMessage> ims = new List<GridInstantMessage>();
            foreach(IValue v in map.Values)
            {
                if(!(v is Map))
                {
                    continue;
                }
                Map m = (Map)v;

                GridInstantMessage im = new GridInstantMessage();
                im.BinaryBucket = StringToByteArray(m["BinaryBucket"].ToString());
                im.Dialog = (GridInstantMessageDialog) m["Dialog"].AsInt;
                im.FromAgent.ID = m["FromAgentID"].ToString();
                im.FromAgent.FullName = m["FromAgentName"].ToString();
                im.IsFromGroup = m["FromGroup"].AsBoolean;
                im.IMSessionID = m["SessionID"].ToString();
                im.Message = m["Message"].ToString();
                im.IsOffline = m["Offline"].AsBoolean;
                im.ParentEstateID = m["EstateID"].AsInt;
                im.Position = m["Position"].AsVector3;
                im.RegionID = m["RegionID"].AsString.ToString();
                im.Timestamp = Date.UnixTimeToDateTime(m["Timestamp"].AsULong);
                im.ToAgent.ID = m["ToAgentID"].ToString();
                ims.Add(im);
            }
            return ims;
        }

        public override void deleteOfflineIM(ulong offlineImID)
        {

        }

    }
    #endregion

    #region Factory
    public class RobustOfflineIMConnectorFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public RobustOfflineIMConnectorFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            if (!ownSection.Contains("URI"))
            {
                m_Log.FatalFormat("Missing 'URI' in section {0}", ownSection.Name);
                throw new ConfigurationLoader.ConfigurationError();
            }
            return new RobustOfflineIMConnector(ownSection.GetString("URI"));
        }
    }
    #endregion
}

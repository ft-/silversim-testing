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
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.ServiceInterfaces.Avatar;
using SilverSim.Types;
using SilverSim.Types.StructuredData.REST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;

namespace SilverSim.BackendHandlers.Robust.Avatar
{
    static class RobustAvatarServiceExtensionMethods
    {
        public static Dictionary<string, string> ToAvatarData(this Dictionary<string, object> reqdata)
        {
            Dictionary<string, string> resdata = new Dictionary<string, string>();

            foreach(string key in reqdata.Keys)
            {
                switch(key)
                {
                    case "UserID":
                        break;

                    case "VERSIONMAX":
                        break;

                    case "VERSIONMIN":
                        break;

                    case "METHOD":
                        break;

                    default:
                        resdata[reqdata[key].ToString().Replace("_", " ")] = reqdata.GetString(key);
                        break;
                }
            }

            return resdata;
        }
    }

    #region Service Implementation
    class RobustAvatarServerHandler : IPlugin
    {
        protected static readonly ILog m_Log = LogManager.GetLogger("ROBUST AVATAR HANDLER");
        private BaseHttpServer m_HttpServer;
        AvatarServiceInterface m_AvatarService = null;
        string m_AvatarServiceName;
        private static Encoding UTF8NoBOM = new System.Text.UTF8Encoding(false);

        public RobustAvatarServerHandler(string avatarServiceName)
        {
            m_AvatarServiceName = avatarServiceName;
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_Log.Info("Initializing handler for avatar server");
            m_HttpServer = loader.HttpServer;
            m_HttpServer.StartsWithUriHandlers.Add("/avatar", AvatarHandler);
            m_AvatarService = loader.GetService<AvatarServiceInterface>(m_AvatarServiceName);
        }

        void SuccessResult(HttpRequest req)
        {
            HttpResponse res = req.BeginResponse("text/xml");
            using (XmlTextWriter writer = new XmlTextWriter(res.GetOutputStream(), UTF8NoBOM))
            {
                writer.WriteStartElement("ServerResponse");
                writer.WriteStartElement("result");
                writer.WriteValue("Success");
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
            res.Close();
        }

        public void AvatarHandler(HttpRequest req)
        {
            if (req.ContainsHeader("X-SecondLife-Shard"))
            {
                req.ErrorResponse(HttpStatusCode.BadRequest, "Request source not allowed");
                return;
            }

            if (req.Method != "POST")
            {
                req.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method Not Allowed");
                return;
            }

            Dictionary<string, object> data;
            try
            {
                data = REST.parseREST(req.Body);
            }
            catch
            {
                req.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                return;
            }

            if(!data.ContainsKey("METHOD"))
            {
                req.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                return;
            }

            try
            {
                switch (data["METHOD"].ToString())
                {
                    case "getavatar":
                        getavatar(req, data);
                        break;

                    case "resetavatar":
                        resetavatar(req, data);
                        break;

                    case "removeitems":
                        removeitems(req, data);
                        break;

                    case "setavatar":
                        setavatar(req, data);
                        break;

                    case "setitems":
                        setitems(req, data);
                        break;

                    default:
                        req.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                        break;
                }
            }
            catch(FailureResultException)
            {
                HttpResponse res = req.BeginResponse("text/xml");
                using(XmlTextWriter writer = new XmlTextWriter(res.GetOutputStream(), UTF8NoBOM))
                {
                    writer.WriteStartElement("ServerResponse");
                    writer.WriteStartElement("result");
                    writer.WriteValue("Failure");
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }
                res.Close();
            }
        }

        public void getavatar(HttpRequest req, Dictionary<string, object> reqdata)
        {

        }

        public void resetavatar(HttpRequest req, Dictionary<string, object> reqdata)
        {

        }

        public void removeitems(HttpRequest req, Dictionary<string, object> reqdata)
        {

        }

        public void setavatar(HttpRequest req, Dictionary<string, object> reqdata)
        {
            Dictionary<string, string> avatarData = reqdata.ToAvatarData();
            UUID principalID = reqdata.GetUUID("UserID");
            try
            {
                m_AvatarService[principalID] = avatarData;
            }
            catch
            {
                throw new FailureResultException();
            }
            SuccessResult(req);
        }

        public void setitems(HttpRequest req, Dictionary<string, object> reqdata)
        {
            Dictionary<string, string> avatarData = reqdata.ToAvatarData();
            UUID principalID = reqdata.GetUUID("UserID");
            try
            {
                m_AvatarService[principalID, new List<string>(avatarData.Keys)] = new List<string>(avatarData.Values);
            }
            catch
            {
                throw new FailureResultException();
            }
            SuccessResult(req);
        }
    }
    #endregion

    #region Factory
    [PluginName("AvatarHandler")]
    public class RobustAvatarHandlerFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("ROBUST AVATAR HANDLER");
        public RobustAvatarHandlerFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new RobustAvatarServerHandler(ownSection.GetString("AvatarService", "AvatarService"));
        }
    }
    #endregion
}

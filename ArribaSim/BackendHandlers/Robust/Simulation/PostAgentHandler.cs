using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using ArribaSim.Main.Common;
using ArribaSim.Main.Common.HttpServer;
using ArribaSim.StructuredData.Agent;
using ArribaSim.Types;
using Nini.Config;
using log4net;
using System.IO.Compression;
using System.Reflection;

namespace ArribaSim.BackendHandlers.Robust.Simulation
{
    #region Service Implementation
    public class PostAgentHandler : IPlugin, IPluginShutdown
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private BaseHttpServer m_HttpServer;
        public PostAgentHandler()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {
            m_Log.Info("[ROBUST AGENT HANDLER]: Initializing agent post handler");
            m_HttpServer = loader.HttpServer;
            m_HttpServer.StartsWithUriHandlers.Add("/agent/", AgentPostHandler);
        }

        public ShutdownOrder ShutdownOrder
        {
            get
            {
                return ShutdownOrder.Any;
            }
        }

        public void Shutdown()
        {
            m_HttpServer.StartsWithUriHandlers.Remove("/agent/");
        }

        private void GetAgentParams(string uri, out UUID agentID, out UUID regionID, out string action)
        {
            agentID = UUID.Zero;
            regionID = UUID.Zero;
            action = "";

            uri = uri.Trim(new char[] { '/' });
            string[] parts = uri.Split('/');
            if(parts.Length < 2)
            {
                throw new InvalidDataException();
            }
            else
            {
                agentID = UUID.Parse(parts[1]);
                if(parts.Length > 2)
                {
                    regionID = UUID.Parse(parts[2]);
                }
                if(parts.Length > 3)
                {
                    action = parts[3];
                }
            }
        }

        public void AgentPostHandler(HttpRequest req)
        {
            UUID agentID;
            UUID regionID;
            string action;
            try
            {
                GetAgentParams(req.RawUrl, out agentID, out regionID, out action);
            }
            catch(Exception e)
            {
                m_Log.InfoFormat("[ROBUST AGENT HANDLER]: Invalid parameters for agent message {0}", req.RawUrl);
                HttpResponse res = req.BeginResponse(HttpStatusCode.NotFound, e.Message);
                res.Close();
                return;
            }
            if (req.Method == "POST")
            {
                Stream httpBody = req.Body;
                if(req.ContentType == "application/x-gzip")
                {
                    httpBody = new GZipStream(httpBody, CompressionMode.Decompress);
                }
                else if(req.ContentType == "application/json")
                {

                }
                else
                {
                    m_Log.InfoFormat("[ROBUST AGENT HANDLER]: Invalid content for agent message {0}: {1}", req.RawUrl, req.ContentType);
                    HttpResponse res = req.BeginResponse(HttpStatusCode.UnsupportedMediaType, "Invalid content for agent message");
                    res.Close();
                    return;
                }

                try
                {
                    PostData agentPost = PostData.Deserialize(httpBody);
                }
                catch(Exception e)
                {
                    m_Log.InfoFormat("[ROBUST AGENT HANDLER]: Deserialization error for agent message {0}", req.RawUrl);
                    HttpResponse res = req.BeginResponse(HttpStatusCode.UnprocessableEntity, e.Message);
                    res.Close();
                    return;
                }
            }
            else if(req.Method == "PUT")
            {
                /* this is the rather nasty HTTP variant of the UDP AgentPosition messaging */
                HttpResponse res = req.BeginResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                res.Close();
            }
            else if(req.Method == "DELETE")
            {
                HttpResponse res = req.BeginResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                res.Close();
            }
            else if(req.Method == "QUERYACCESS")
            {
                HttpResponse res = req.BeginResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                res.Close();
            }
            else
            {
                HttpResponse res = req.BeginResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                res.Close();
            }
        }
    }
    #endregion

    #region Service Factory
    public class PostAgentHandlerFactory : IPluginFactory
    {
        public PostAgentHandlerFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new PostAgentHandler();
        }
    }
    #endregion
}

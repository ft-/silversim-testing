/*

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

using ArribaSim.Main.Common;
using ArribaSim.Main.Common.HttpServer;
using ArribaSim.StructuredData.Agent;
using ArribaSim.Types;
using log4net;
using Nini.Config;
using System;
using System.IO;
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

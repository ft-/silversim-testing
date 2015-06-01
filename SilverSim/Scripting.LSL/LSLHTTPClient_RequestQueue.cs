using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpClient;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using ThreadedClasses;

namespace SilverSim.Scripting.LSL
{
    public class LSLHTTPClient_RequestQueue : IPlugin, IPluginShutdown
    {
        public class LSLHttpRequest
        {
            public UUID RequestID = UUID.Random;
            public UUID SceneID;
            public UUID PrimID;
            public UUID ItemID;
            public string Url;
            public string Method = "GET";
            public string MimeType = "text/plain;charset=utf-8";
            public bool VerifyCert = true;
            public bool VerboseThrottle = true;
            public bool SendPragmaNoCache = true;
            public int MaxBodyLength = 2048;
            public string RequestBody = "";
            public Dictionary<string, string> Headers = new Dictionary<string, string>();

            public LSLHttpRequest()
            {
                Headers.Add("User-Agent", string.Format("{0} {1}", VersionInfo.ProductName, VersionInfo.Version));
                Headers.Add("X-SecondLife-Shard", VersionInfo.Shard);
            }
        }

        RwLockedDictionary<UUID, BlockingQueue<LSLHttpRequest>> m_RequestQueues = new RwLockedDictionary<UUID, BlockingQueue<LSLHttpRequest>>();

        public LSLHTTPClient_RequestQueue()
        {
            SceneManager.Scenes.OnRegionAdd += RegionAdded;
            SceneManager.Scenes.OnRegionRemove += RegionRemoved;
        }

        void RegionRemoved(SceneInterface scene)
        {
            m_RequestQueues.Remove(scene.ID);
        }

        void RegionAdded(SceneInterface scene)
        {
            int i;
            try
            {
                m_RequestQueues.Add(scene.ID, new BlockingQueue<LSLHttpRequest>());
            }
            catch
            {

            }
            for(i = 0; i < 10; ++i)
            {
                Thread t = new Thread(ProcessThread);
                t.Name = "LSL:HTTPClient Processor for region " + scene.ID;
                t.Start(scene.ID);
            }
        }

        internal bool Enqueue(LSLHttpRequest req)
        {
            BlockingQueue<LSLHttpRequest> queue;
            if(m_RequestQueues.TryGetValue(req.SceneID, out queue))
            {
                queue.Enqueue(req);
                return true;
            }
            return false;
        }

        void ProcessThread(object o)
        {
            UUID id = (UUID)o;
            LSLHttpRequest req;
            for(;;)
            {
                BlockingQueue<LSLHttpRequest> reqqueue;

                if(!m_RequestQueues.TryGetValue(id, out reqqueue))
                {
                    /* terminate condition is deletion of the request queue */
                    break;
                }

                try
                {
                    req = reqqueue.Dequeue(1000);
                }
                catch(BlockingQueue<LSLHttpRequest>.TimeoutException)
                {
                    continue;
                }

                HttpResponseEvent ev = new HttpResponseEvent();
                ev.RequestID = req.RequestID;
                try
                {
                    ev.Body = HttpRequestHandler.DoRequest(req.Method, req.Url, null, req.MimeType, req.RequestBody, false, 30000);
                }
                catch(HttpRequestHandler.BadHttpResponseException)
                {
                    ev.Status = 499;
                }
                catch(HttpException e)
                {
                    ev.Body = e.Message;
                    ev.Status = e.GetHttpCode();
                }
                catch
                {
                    HttpResponseEvent e = new HttpResponseEvent();
                    e.Status = 499;
                }
                SceneInterface scene;
                if(!SceneManager.Scenes.TryGetValue(req.SceneID, out scene))
                {
                    continue;
                }

                ObjectPart part;
                try
                {
                    part = scene.Primitives[req.PrimID];
                }
                catch
                {
                    continue;
                }

                part.PostEvent(ev);
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
        }

        public ShutdownOrder ShutdownOrder
        {
            get
            {
                return Main.Common.ShutdownOrder.LogoutRegion;
            }
        }

        public void Shutdown()
        {
            m_RequestQueues.Clear();
        }
    }
}

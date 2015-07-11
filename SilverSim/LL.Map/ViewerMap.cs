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
using SilverSim.LL.Core;
using SilverSim.LL.Messages;
using SilverSim.LL.Messages.Map;
using SilverSim.Main.Common;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.Types;
using SilverSim.Types.Grid;
using System;
using System.Collections.Generic;
using System.Threading;
using ThreadedClasses;

namespace SilverSim.LL.Map
{
    public class ViewerMap : IPlugin, IPluginShutdown, IPacketHandlerExtender
    {
        private static readonly ILog m_Log = LogManager.GetLogger("LL MAP");

        [PacketHandler(MessageType.MapBlockRequest)]
        [PacketHandler(MessageType.MapNameRequest)]
        BlockingQueue<KeyValuePair<Circuit, Message>> MapBlocksRequestQueue = new BlockingQueue<KeyValuePair<Circuit, Message>>();

        [PacketHandler(MessageType.MapLayerRequest)]
        [PacketHandler(MessageType.MapItemRequest)]
        BlockingQueue<KeyValuePair<Circuit, Message>> MapDetailsRequestQueue = new BlockingQueue<KeyValuePair<Circuit, Message>>();

        bool m_ShutdownMap = false;

        public ViewerMap()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {
            new Thread(HandlerThread).Start(MapBlocksRequestQueue);
            new Thread(HandlerThread).Start(MapDetailsRequestQueue);
        }

        public void HandlerThread(object o)
        {
            BlockingQueue<KeyValuePair<Circuit, Message>> requestQueue = (BlockingQueue<KeyValuePair<Circuit, Message>>)o;
            if (requestQueue == MapDetailsRequestQueue)
            {
                Thread.CurrentThread.Name = "Map Details Handler Thread";
            }
            else
            {
                Thread.CurrentThread.Name = "Map Blocks Handler Thread";
            }

            while (!m_ShutdownMap)
            {
                KeyValuePair<Circuit, Message> req;
                try
                {
                    req = requestQueue.Dequeue(1000);
                }
                catch
                {
                    continue;
                }

                Message m = req.Value;
                SceneInterface scene = req.Key.Scene;
                if (scene == null)
                {
                    continue;
                }
                try
                {
                    switch (m.Number)
                    {
                        case MessageType.MapNameRequest:
                            HandleMapNameRequest(req.Key.Agent, scene, m);
                            break;

                        case MessageType.MapBlockRequest:
                            HandleMapBlockRequest(req.Key.Agent, scene, m);
                            break;

                        case MessageType.MapItemRequest:
                            HandleMapItemRequest(req.Key.Agent, scene, m);
                            break;

                        default:
                            break;
                    }
                }
                catch (Exception e)
                {
                    m_Log.Debug("Unexpected exception " + e.Message, e);
                }
            }
        }

        #region MapNameRequest and MapBlockRequest
        void HandleMapBlockRequest(LLAgent agent, SceneInterface scene, Message m)
        {
            MapBlockRequest req = (MapBlockRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            SendMapBlocks(agent, scene, req.Flags, new List<MapBlockReply.DataEntry>());
        }

        void HandleMapNameRequest(LLAgent agent, SceneInterface scene, Message m)
        {
            MapNameRequest req = (MapNameRequest)m;
            if(req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

#if DEBUG
            m_Log.InfoFormat("MapNameRequest for {0}", req.Name);
#endif
            string[] s;
            bool isHGTarget = false;
            string regionName = req.Name;
            string gatekeeperURI = string.Empty;
            List<MapBlockReply.DataEntry> results = new List<MapBlockReply.DataEntry>();

            s = req.Name.Split(new char[] { ':' }, 3);
            if(s.Length > 1)
            {
                /* could be a HG URI, check for number in second place */
                uint val;
                if(!uint.TryParse(s[1], out val))
                {
                    /* not a HG map name */
                }
                else if(val > 65535)
                {
                    /* not a HG map name */
                }
                else if(!Uri.IsWellFormedUriString("http://" + s[0] + ":" + s[1] + "/", UriKind.Absolute))
                {
                    /* not a HG map name */
                }
                else
                {
                    gatekeeperURI = "http://" + s[0] + ":" + s[1] + "/";
                    if (s.Length > 2)
                    {
                        regionName = s[2];
                    }
                    else
                    {
                        regionName = ""; /* DefaultHGRegion */
                    }
                    isHGTarget = true;
                }
            }
            s = req.Name.Split(new char[] { ' ' }, 2);
            if(s.Length > 1)
            {
                if(Uri.IsWellFormedUriString(s[0],UriKind.Absolute))
                {
                    /* this is a HG URI of form <url> <region name> */
                    gatekeeperURI = s[0];
                    regionName = s[1];
                }
                else
                {
                    /* does not look like a uri at all */
                }
            }
            else if(Uri.IsWellFormedUriString(req.Name, UriKind.Absolute))
            {
                /* this is a HG URI for the DefaultHGRegion */
                gatekeeperURI = req.Name;
                regionName = "";
            }
            else
            {
                /* local Grid URI */
            }

            if(isHGTarget)
            {
                
            }
            else if(string.IsNullOrEmpty(regionName))
            {
                agent.SendAlertMessage("Please enter a string", scene.ID);
            }
            else
            {
                GridServiceInterface service = scene.GridService;
                if(service != null)
                {
                    List<RegionInfo> ris;
                    try
                    {
                        ris = service.SearchRegionsByName(scene.RegionData.ScopeID, regionName);
                    }
                    catch
                    {
                        ris = new List<RegionInfo>();
                    }

                    foreach(RegionInfo ri in ris)
                    {
                        MapBlockReply.DataEntry d = new MapBlockReply.DataEntry();
                        d.X = ri.Location.GridX;
                        d.Y = ri.Location.GridY;

                        /* check for the mad V2/V3 viewers */
                        d.Name = ri.Name;
                        d.Access = ri.Access;
                        d.RegionFlags = ri.Flags;
                        d.WaterHeight = 21;
                        d.Agents = 0;
                        d.MapImageID = ri.RegionMapTexture;
                        results.Add(d);
                    }
                }
            }

            SendMapBlocks(agent, scene, req.Flags, results);
        }

        void SendMapBlocks(LLAgent agent, SceneInterface scene, MapAgentFlags mapflags, List<MapBlockReply.DataEntry> mapBlocks)
        {
            MapBlockReply.DataEntry end = new MapBlockReply.DataEntry();
            end.Agents = 0;
            end.Access = RegionAccess.NonExistent;
            end.MapImageID = UUID.Zero;
            end.Name = "";
            end.RegionFlags = RegionFlags.None;
            end.WaterHeight = 0;
            end.X = 0;
            end.Y = 0;
            mapBlocks.Add(end);

            MapBlockReply replymsg = null;
            int mapBlockReplySize = 20;

            foreach(MapBlockReply.DataEntry d in mapBlocks)
            {
                int mapBlockDataSize = 27 + d.Name.Length;
                if (mapBlockReplySize + mapBlockDataSize > 1400 && null != replymsg)
                {
                    agent.SendMessageAlways(replymsg, scene.ID);
                    replymsg = null;
                }

                if (null == replymsg)
                {
                    replymsg = new MapBlockReply();
                    replymsg.AgentID = agent.ID;
                    replymsg.Flags = mapflags;
                    mapBlockReplySize = 20;
                }

                mapBlockReplySize += mapBlockDataSize;
                replymsg.Data.Add(d);
            }

            if(null != replymsg)
            {
                agent.SendMessageAlways(replymsg, scene.ID);
            }
        }
        #endregion

        #region MapItemRequest
        void HandleMapItemRequest(LLAgent agent, SceneInterface scene, Message m)
        {
            MapItemRequest req = (MapItemRequest)m;
            if(req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            MapItemReply reply = new MapItemReply();
            reply.AgentID = agent.ID;
            reply.Flags = req.Flags;
            reply.ItemType = req.ItemType;

            agent.SendMessageAlways(reply, scene.ID);
        }
        #endregion

        public ShutdownOrder ShutdownOrder
        {
            get 
            {
                return ShutdownOrder.LogoutRegion;
            }
        }

        public void Shutdown()
        {
            m_ShutdownMap = true;
        }
    }

    [PluginName("ViewerMap")]
    public class Factory : IPluginFactory
    {
        public Factory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new ViewerMap();
        }
    }
}

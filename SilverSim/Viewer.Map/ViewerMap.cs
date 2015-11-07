// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using Nini.Config;
using SilverSim.Viewer.Core;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Map;
using SilverSim.Main.Common;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.Types;
using SilverSim.Types.Grid;
using SilverSim.Types.Parcel;
using System;
using System.Collections.Generic;
using System.Threading;
using ThreadedClasses;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Viewer.Map
{
    public class ViewerMap : IPlugin, IPluginShutdown, IPacketHandlerExtender
    {
        private static readonly ILog m_Log = LogManager.GetLogger("LL MAP");

        [PacketHandler(MessageType.MapBlockRequest)]
        [PacketHandler(MessageType.MapNameRequest)]
        readonly BlockingQueue<KeyValuePair<AgentCircuit, Message>> MapBlocksRequestQueue = new BlockingQueue<KeyValuePair<AgentCircuit, Message>>();

        [PacketHandler(MessageType.MapLayerRequest)]
        [PacketHandler(MessageType.MapItemRequest)]
        readonly BlockingQueue<KeyValuePair<AgentCircuit, Message>> MapDetailsRequestQueue = new BlockingQueue<KeyValuePair<AgentCircuit, Message>>();

        List<IForeignGridConnectorPlugin> m_ForeignGridConnectorPlugins;

        bool m_ShutdownMap;

        public ViewerMap()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {
            m_ForeignGridConnectorPlugins = loader.GetServicesByValue<IForeignGridConnectorPlugin>();

            new Thread(HandlerThread).Start(MapBlocksRequestQueue);
            new Thread(HandlerThread).Start(MapDetailsRequestQueue);
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public void HandlerThread(object o)
        {
            BlockingQueue<KeyValuePair<AgentCircuit, Message>> requestQueue = (BlockingQueue<KeyValuePair<AgentCircuit, Message>>)o;
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
                KeyValuePair<AgentCircuit, Message> req;
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
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        void HandleMapBlockRequest(ViewerAgent agent, SceneInterface scene, Message m)
        {
            List<MapBlockReply.DataEntry> results = new List<MapBlockReply.DataEntry>();
            MapBlockRequest req = (MapBlockRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

#if DEBUG
            m_Log.InfoFormat("MapBlockRequest for {0},{1} {2},{3}", req.Min.GridX, req.Min.GridY, req.Max.GridX, req.Max.GridY);
#endif
            List<RegionInfo> ris;
            try
            {
                ris = scene.GridService.GetRegionsByRange(scene.RegionData.ScopeID, req.Min, req.Max);
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

                d.Name = ri.Name;
                d.Access = ri.Access;
                d.RegionFlags = ri.Flags;
                d.WaterHeight = 21;
                d.Agents = 0;
                d.MapImageID = ri.RegionMapTexture;
                results.Add(d);
            }

            SendMapBlocks(agent, scene, req.Flags, results);
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        void HandleMapNameRequest(ViewerAgent agent, SceneInterface scene, Message m)
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
            bool isForeignGridTarget = false;
            string regionName = req.Name;
            string gatekeeperURI = string.Empty;
            List<MapBlockReply.DataEntry> results = new List<MapBlockReply.DataEntry>();

            s = req.Name.Split(new char[] { ':' }, 3);
            if(s.Length > 1)
            {
                /* could be a foreign grid URI, check for number in second place */
                uint val;
                if(!uint.TryParse(s[1], out val))
                {
                    /* not a foreign grid map name */
                }
                else if(val > 65535)
                {
                    /* not a foreign grid map name */
                }
                else if(!Uri.IsWellFormedUriString("http://" + s[0] + ":" + s[1] + "/", UriKind.Absolute))
                {
                    /* not a foreign grid map name */
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
                        regionName = string.Empty; /* Default Region */
                    }
                    isForeignGridTarget = true;
                }
            }
            s = req.Name.Split(new char[] { ' ' }, 2);
            if(s.Length > 1)
            {
                if(Uri.IsWellFormedUriString(s[0],UriKind.Absolute))
                {
                    /* this is a foreign grid URI of form <url> <region name> */
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
                /* this is a foreign Grid URI for the Default Region */
                gatekeeperURI = req.Name;
                regionName = string.Empty;
            }
            else
            {
                /* local Grid URI */
            }

            if(isForeignGridTarget)
            {
                RegionInfo ri = null;
                bool foundRegionButWrongProtocol = false;
                string foundProtocolName = string.Empty;
                foreach(IForeignGridConnectorPlugin foreignGrid in m_ForeignGridConnectorPlugins)
                {
                    if(foreignGrid.IsProtocolSupported(gatekeeperURI))
                    {
                        try
                        {
                            ri = foreignGrid.Instantiate(gatekeeperURI)[regionName];
                        }
                        catch
                        {
                            continue;
                        }
                        if(!foreignGrid.IsAgentSupported(agent.SupportedGridTypes))
                        {
                            foundRegionButWrongProtocol = true;
                            foundProtocolName = agent.DisplayName;
                            ri = null;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                if(ri == null && foundRegionButWrongProtocol)
                {
                    agent.SendAlertMessage(string.Format("Your home grid does not support the selected target grid (running {0}).", foundProtocolName), scene.ID);
                }
                else if(ri != null)
                {
                    MapBlockReply.DataEntry d = new MapBlockReply.DataEntry();
                    d.X = 0;
                    d.Y = ri.Location.GridY;
                    /* we map foreign grid locations in specific agent only */
                    if(ri.Location.GridX > d.Y)
                    {
                        d.Y = ri.Location.GridX;
                    }

                    d.Name = ri.Name;
                    d.Access = ri.Access;
                    d.RegionFlags = ri.Flags;
                    d.WaterHeight = 21;
                    d.Agents = 0;
                    d.MapImageID = ri.RegionMapTexture;
                    results.Add(d);
                }
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

        void SendMapBlocks(ViewerAgent agent, SceneInterface scene, MapAgentFlags mapflags, List<MapBlockReply.DataEntry> mapBlocks)
        {
            MapBlockReply.DataEntry end = new MapBlockReply.DataEntry();
            end.Agents = 0;
            end.Access = RegionAccess.NonExistent;
            end.MapImageID = UUID.Zero;
            end.Name = string.Empty;
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
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        void HandleMapItemRequest(ViewerAgent agent, SceneInterface scene, Message m)
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

            SceneInterface accessScene = null;
            if(req.Location.RegionHandle == 0)
            {
                accessScene = scene;
            }
            else if(req.Location.Equals(scene.RegionData.Location))
            {
                accessScene = scene;
            }
            else 
            {
                try
                {
                    accessScene = SceneManager.Scenes[req.Location];
                }
                catch
                {
                    accessScene = null; /* remote */
                }
            }

            switch(req.ItemType)
            {
                case MapItemType.AgentLocations:
                    if(null != accessScene)
                    {
                        /* local */
                        foreach(IAgent sceneagent in accessScene.Agents)
                        {
                            if(sceneagent.IsInScene(accessScene) && !sceneagent.Owner.Equals(agent.Owner) && sceneagent is ViewerAgent)
                            {
                                MapItemReply.DataEntry d = new MapItemReply.DataEntry();
                                d.X = (ushort)sceneagent.GlobalPosition.X;
                                d.Y = (ushort)sceneagent.GlobalPosition.Y;
                                d.ID = UUID.Zero;
                                d.Name = sceneagent.Owner.FullName;
                                d.Extra = 1;
                                d.Extra2 = 0;
                                reply.Data.Add(d);
                            }
                        }
                    }
                    else
                    {
                        /* remote */
                    }
                    break;

                case MapItemType.LandForSale:
                    if(null != accessScene)
                    {
                        /* local */
                        foreach(ParcelInfo parcel in accessScene.Parcels)
                        {
                            if((parcel.Flags & ParcelFlags.ForSale) != 0)
                            {
                                MapItemReply.DataEntry d = new MapItemReply.DataEntry();
                                double x = (parcel.AABBMin.X + parcel.AABBMax.X) / 2;
                                double y = (parcel.AABBMin.Y + parcel.AABBMax.Y) / 2;
                                d.X = (ushort)x;
                                d.Y = (ushort)y;
                                d.ID = parcel.ID;
                                d.Name = parcel.Name;
                                d.Extra = parcel.Area;
                                d.Extra2 = parcel.SalePrice;
                                reply.Data.Add(d);
                            }
                        }
                    }
                    else
                    {
                        /* remote */
                    }
                    break;

                case MapItemType.Telehub:
                    if(null != accessScene)
                    {
                        /* local */
                    }
                    else
                    {
                        /* remote */
                    }
                    break;

                default:
                    break;
            }
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

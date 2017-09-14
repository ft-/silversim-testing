// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

#pragma warning disable IDE0018
#pragma warning disable RCS1029

using log4net;
using SilverSim.Main.Common;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Estate;
using SilverSim.Types.Grid;
using SilverSim.Types.Parcel;
using SilverSim.Viewer.Core;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Map;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace SilverSim.Viewer.Map
{
    [Description("Viewer Map Handler")]
    [PluginName("ViewerMap")]
    public class ViewerMap : IPlugin, IPluginShutdown, IPacketHandlerExtender
    {
        private static readonly ILog m_Log = LogManager.GetLogger("LL MAP");

        [PacketHandler(MessageType.MapBlockRequest)]
        [PacketHandler(MessageType.MapNameRequest)]
        private readonly BlockingQueue<KeyValuePair<AgentCircuit, Message>> MapBlocksRequestQueue = new BlockingQueue<KeyValuePair<AgentCircuit, Message>>();

        [PacketHandler(MessageType.MapLayerRequest)]
        [PacketHandler(MessageType.MapItemRequest)]
        private readonly BlockingQueue<KeyValuePair<AgentCircuit, Message>> MapDetailsRequestQueue = new BlockingQueue<KeyValuePair<AgentCircuit, Message>>();

        private List<IForeignGridConnectorPlugin> m_ForeignGridConnectorPlugins;
        private SceneList m_Scenes;
        private bool m_ShutdownMap;

        public void Startup(ConfigurationLoader loader)
        {
            m_Scenes = loader.Scenes;
            m_ForeignGridConnectorPlugins = loader.GetServicesByValue<IForeignGridConnectorPlugin>();

            ThreadManager.CreateThread(HandlerThread).Start(MapBlocksRequestQueue);
            ThreadManager.CreateThread(HandlerThread).Start(MapDetailsRequestQueue);
        }

        public void HandlerThread(object o)
        {
            var requestQueue = (BlockingQueue<KeyValuePair<AgentCircuit, Message>>)o;
            Thread.CurrentThread.Name = (requestQueue == MapDetailsRequestQueue) ?
                "Map Details Handler Thread" :
                "Map Blocks Handler Thread";

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
                var circuit = req.Key;
                if(circuit == null)
                {
                    continue;
                }
                var scene = circuit.Scene;
                if (scene == null)
                {
                    continue;
                }
                try
                {
                    switch (m.Number)
                    {
                        case MessageType.MapNameRequest:
                            HandleMapNameRequest(circuit.Agent, scene, m);
                            break;

                        case MessageType.MapBlockRequest:
                            HandleMapBlockRequest(circuit.Agent, scene, m);
                            break;

                        case MessageType.MapItemRequest:
                            HandleMapItemRequest(circuit.Agent, scene, m);
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
        private void HandleMapBlockRequest(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var results = new List<MapBlockReply.DataEntry>();
            var req = (MapBlockRequest)m;
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
                ris = scene.GridService.GetRegionsByRange(scene.ScopeID, req.Min, req.Max);
            }
            catch
            {
                ris = new List<RegionInfo>();
            }

            foreach(var ri in ris)
            {
                results.Add(new MapBlockReply.DataEntry()
                {
                    X = ri.Location.GridX,
                    Y = ri.Location.GridY,

                    Name = ri.Name,
                    Access = ri.Access,
                    RegionFlags = RegionOptionFlags.None, /* this is same RegionOptionFlags as seen in a sim */
                    WaterHeight = 21,
                    Agents = 0,
                    MapImageID = ri.RegionMapTexture
                });
            }

            SendMapBlocks(agent, scene, req.Flags, results);
        }

        private void HandleMapNameRequest(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (MapNameRequest)m;
            if(req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

#if DEBUG
            m_Log.InfoFormat("MapNameRequest for {0}", req.Name);
#endif
            string[] s;
            var isForeignGridTarget = false;
            var regionName = req.Name;
            var gatekeeperURI = string.Empty;
            var results = new List<MapBlockReply.DataEntry>();

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
                    regionName = (s.Length > 2) ?
                        s[2] :
                        string.Empty; /* Default Region */
                    isForeignGridTarget = true;
                }
            }
            if (isForeignGridTarget)
            {
                /* already identified one form */
            }
            else
            {
                s = req.Name.Split(new char[] { ' ' }, 2);
                if (s.Length > 1)
                {
                    if (Uri.IsWellFormedUriString(s[0], UriKind.Absolute))
                    {
                        /* this is a foreign grid URI of form <url> <region name> */
                        gatekeeperURI = s[0];
                        regionName = s[1];
                        isForeignGridTarget = true;
                    }
                    else
                    {
                        /* does not look like a uri at all */
                    }
                }
                else if (Uri.IsWellFormedUriString(req.Name, UriKind.Absolute))
                {
                    /* this is a foreign Grid URI for the Default Region */
                    gatekeeperURI = req.Name;
                    regionName = string.Empty;
                    isForeignGridTarget = true;
                }
                else
                {
                    /* local Grid URI */
                }
            }

            if(isForeignGridTarget)
            {
#if DEBUG
                m_Log.InfoFormat("MapNameRequest for foreign at {0} region={1}", gatekeeperURI, regionName);
#endif

                RegionInfo ri = null;
                var foundRegionButWrongProtocol = false;
                var foundProtocolName = string.Empty;
                string message;
                foreach(var foreignGrid in m_ForeignGridConnectorPlugins)
                {
#if DEBUG
                    m_Log.DebugFormat("Testing foreign grid protocol \"{0}\"", foreignGrid.DisplayName);
#endif
                    if(foreignGrid.IsProtocolSupported(gatekeeperURI))
                    {
                        try
                        {
                            if(!foreignGrid.Instantiate(gatekeeperURI).TryGetValue(regionName, out ri, out message))
                            {
                                continue;
                            }
                        }
                        catch(Exception e)
                        {
                            m_Log.Error("Failed to connect to grid " + gatekeeperURI, e);
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
#if DEBUG
                            m_Log.DebugFormat("Selected protocol {0}", foreignGrid.Name);
#endif
                            break;
                        }
                    }
                    else
                    {
#if DEBUG
                        m_Log.DebugFormat("Foreign grid protocol \"{0}\" not supported for \"{1}\"", foreignGrid.DisplayName, gatekeeperURI);
#endif
                    }
                }

                if(ri == null && foundRegionButWrongProtocol)
                {
                    agent.SendAlertMessage(string.Format(this.GetLanguageString(agent.CurrentCulture, "YourHomeGridDoesNotSupportSelectedTargetGrid0", "Your home grid does not support the selected target grid (running {0})."), foundProtocolName), scene.ID);
                }
                else if(ri != null)
                {
                    var hgLoc = agent.CacheInterGridDestination(ri);
                    results.Add(new MapBlockReply.DataEntry()
                    {
                        /* we map foreign grid locations in specific agent only */
                        X = hgLoc.GridX,
                        Y = hgLoc.GridY,

                        Name = ri.Name,
                        Access = ri.Access,
                        RegionFlags = RegionOptionFlags.None, /* this is same region flags as seen on a sim */
                        WaterHeight = 21,
                        Agents = 0,
                        MapImageID = ri.RegionMapTexture
                    });
                }
            }
            else if(string.IsNullOrEmpty(regionName))
            {
                agent.SendAlertMessage(this.GetLanguageString(agent.CurrentCulture, "PleaseEnterAString", "Please enter a string"), scene.ID);
            }
            else
            {
#if DEBUG
                m_Log.InfoFormat("MapNameRequest for {0} at local grid", regionName);
#endif

                var service = scene.GridService;
                if(service != null)
                {
                    List<RegionInfo> ris;
                    try
                    {
                        ris = service.SearchRegionsByName(scene.ScopeID, regionName);
                    }
                    catch
                    {
                        ris = new List<RegionInfo>();
                    }

                    foreach(var ri in ris)
                    {
                        results.Add(new MapBlockReply.DataEntry()
                        {
                            X = ri.Location.GridX,
                            Y = ri.Location.GridY,

                            Name = ri.Name,
                            Access = ri.Access,
                            RegionFlags = RegionOptionFlags.None, /* this is same region flags as seen on a sim */
                            WaterHeight = 21,
                            Agents = 0,
                            MapImageID = ri.RegionMapTexture
                        });
                    }
                }
            }

            SendMapBlocks(agent, scene, req.Flags, results);
        }

        private void SendMapBlocks(ViewerAgent agent, SceneInterface scene, MapAgentFlags mapflags, List<MapBlockReply.DataEntry> mapBlocks)
        {
            mapBlocks.Add(new MapBlockReply.DataEntry()
            {
                Agents = 0,
                Access = RegionAccess.NonExistent,
                MapImageID = UUID.Zero,
                Name = string.Empty,
                RegionFlags = RegionOptionFlags.None, /* this is same region flags as seen on a sim */
                WaterHeight = 0,
                X = 0,
                Y = 0
            });

            MapBlockReply replymsg = null;
            int mapBlockReplySize = 20;

            foreach(var d in mapBlocks)
            {
                int mapBlockDataSize = 27 + d.Name.Length;
                if (mapBlockReplySize + mapBlockDataSize > 1400 && replymsg != null)
                {
                    agent.SendMessageAlways(replymsg, scene.ID);
                    replymsg = null;
                }

                if (replymsg == null)
                {
                    replymsg = new MapBlockReply()
                    {
                        AgentID = agent.ID,
                        Flags = mapflags
                    };
                    mapBlockReplySize = 20;
                }

                mapBlockReplySize += mapBlockDataSize;
                replymsg.Data.Add(d);
            }

            if(replymsg != null)
            {
                agent.SendMessageAlways(replymsg, scene.ID);
            }
        }
        #endregion

        #region MapItemRequest
        private void HandleMapItemRequest(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (MapItemRequest)m;
            if(req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            var reply = new MapItemReply()
            {
                AgentID = agent.ID,
                Flags = req.Flags,
                ItemType = req.ItemType
            };
            SceneInterface accessScene = null;
            if(req.Location.RegionHandle == 0 ||
                req.Location.Equals(scene.GridPosition))
            {
                accessScene = scene;
            }
            else
            {
                try
                {
                    accessScene = m_Scenes[req.Location];
                }
                catch
                {
                    accessScene = null; /* remote */
                }
            }

            switch(req.ItemType)
            {
                case MapItemType.AgentLocations:
                    if(accessScene != null)
                    {
                        /* local */
                        foreach(var sceneagent in accessScene.Agents)
                        {
                            if(sceneagent.IsInScene(accessScene) && !sceneagent.Owner.Equals(agent.Owner) && sceneagent is ViewerAgent)
                            {
                                var d = new MapItemReply.DataEntry()
                                {
                                    X = (ushort)sceneagent.GlobalPosition.X,
                                    Y = (ushort)sceneagent.GlobalPosition.Y,
                                    ID = UUID.Zero,
                                    Name = sceneagent.Owner.FullName,
                                    Extra = 1,
                                    Extra2 = 0
                                };
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
                    if(accessScene != null)
                    {
                        /* local */
                        foreach(var parcel in accessScene.Parcels)
                        {
                            if((parcel.Flags & ParcelFlags.ForSale) != 0)
                            {
                                var d = new MapItemReply.DataEntry();
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
                    if(accessScene != null)
                    {
                        /* local */
                    }
                    else
                    {
                        /* remote */
                    }
                    break;
            }
            agent.SendMessageAlways(reply, scene.ID);
        }
        #endregion

        public ShutdownOrder ShutdownOrder => ShutdownOrder.LogoutRegion;

        public void Shutdown()
        {
            m_ShutdownMap = true;
        }
    }
}

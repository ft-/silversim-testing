// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Http.Client;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types.StructuredData.Llsd;
using SilverSim.Types;
using SilverSim.Types.Grid;
using SilverSim.Viewer.Messages.Chat;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Diagnostics.CodeAnalysis;
using SilverSim.Threading;
using SilverSim.ServiceInterfaces.ServerParam;

namespace SilverSim.Scene.Types.Scene
{
    [ServerParam("ChatPassInEnable", ParameterType = typeof(bool))]
    [ServerParam("ChatPassOutEnable", ParameterType = typeof(bool))]
    public partial class SceneInterface
    {
        public class NeighborEntry
        {
            /* <summary>RemoteOffset = RemoteGlobalPosition - LocalGlobalPosition</summary> */
            [Description("RemoteOffset = RemoteGlobalPosition - LocalGlobalPosition")]
            public Vector3 RemoteOffset;
            public ICircuit RemoteCircuit;
            public RegionInfo RemoteRegionData;
        }

        public readonly Dictionary<UUID, NeighborEntry> Neighbors = new Dictionary<UUID, NeighborEntry>();

        [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
        public delegate bool TryGetSceneDelegate(UUID id, out SceneInterface scene);
        public TryGetSceneDelegate TryGetScene;

        readonly RwLockedList<UUID> m_ChatPassInEnableLocal = new RwLockedList<UUID>();
        readonly RwLockedList<UUID> m_ChatPassInEnableGlobal = new RwLockedList<UUID>();
        bool m_ChatPassInEnableSetToLocal = false;

        readonly RwLockedList<UUID> m_ChatPassOutEnableLocal = new RwLockedList<UUID>();
        readonly RwLockedList<UUID> m_ChatPassOutEnableGlobal = new RwLockedList<UUID>();
        bool m_ChatPassOutEnableSetToLocal = false;

        void ChatPassEnableUpdated(
           RwLockedList<UUID> locallist,
           RwLockedList<UUID> globallist,
           ref bool settolocal,
           UUID regionId,
           string varname,
           string value)
        {
            if(string.IsNullOrEmpty(value))
            {
                if(regionId != UUID.Zero)
                {
                    settolocal = false;
                    locallist.Clear();
                }
                else
                {
                    globallist.Clear();
                }
            }
            else
            {
                string[] parts = value.Split(',');
                List<UUID> new_ids = new List<UUID>();
                foreach(string part in parts)
                {
                    UUID id;
                    if(!UUID.TryParse(part, out id))
                    {
                        m_Log.WarnFormat("Invalid UUID '{1}' found in {0}/{2} variable", regionId.ToString(), part, varname);
                    }
                    else if(!new_ids.Contains(id))
                    {
                        new_ids.Add(id);
                    }
                }

                RwLockedList<UUID> activelist = regionId != UUID.Zero ? locallist : globallist;

                foreach (UUID id in new List<UUID>(activelist))
                {
                    if(!new_ids.Contains(id))
                    {
                        activelist.Remove(id);
                    }
                }

                foreach(UUID id in new_ids)
                {
                    if(!activelist.Contains(id))
                    {
                        activelist.Add(id);
                    }
                }

                if(regionId != UUID.Zero)
                {
                    settolocal = true;
                }
            }
        }

        [ServerParam("ChatPassInEnable", ParameterType = typeof(bool))]
        public void ChatPassInEnableUpdated(UUID regionId, string value)
        {
            ChatPassEnableUpdated(
                m_ChatPassInEnableLocal,
                m_ChatPassInEnableGlobal,
                ref m_ChatPassInEnableSetToLocal,
                regionId,
                "ChatPassInEnable",
                value);
        }

        [ServerParam("ChatPassOutEnable", ParameterType = typeof(bool))]
        public void ChatPassOutEnableUpdated(UUID regionId, string value)
        {
            ChatPassEnableUpdated(
                m_ChatPassOutEnableLocal,
                m_ChatPassOutEnableGlobal,
                ref m_ChatPassOutEnableSetToLocal,
                regionId,
                "ChatPassOutEnable",
                value);
        }

        public void ChatPassInbound(UUID fromRegionID, ListenEvent ev)
        {
            RwLockedList<UUID> activelist = m_ChatPassInEnableSetToLocal ? m_ChatPassInEnableLocal : m_ChatPassInEnableGlobal;
            if(activelist.Count == 0 || activelist.Contains(UUID.Zero) || activelist.Contains(ev.ID))
            {
                SendChatPass(ev);
            }
        }

        protected void ChatPassLocalNeighbors(ListenEvent le)
        {
            RwLockedList<UUID> activelist = m_ChatPassInEnableSetToLocal ? m_ChatPassInEnableLocal : m_ChatPassInEnableGlobal;
            bool chatPassDefault = true;
            foreach (KeyValuePair<UUID, NeighborEntry> kvp in Neighbors)
            {
                SceneInterface remoteScene;
                TryGetSceneDelegate m_TryGetScene = TryGetScene;
                if(!(activelist.Count == 0 || activelist.Contains(le.ID) || activelist.Contains(UUID.Zero)))
                {
                    continue;
                }

                if(null != kvp.Value.RemoteCircuit)
                {
                    Vector3 newPosition = le.GlobalPosition + kvp.Value.RemoteOffset;
                    if (newPosition.X >= -le.Distance && 
                        newPosition.Y >= -le.Distance &&
                        newPosition.X <= kvp.Value.RemoteRegionData.Size.X + le.Distance &&
                        newPosition.Y <= kvp.Value.RemoteRegionData.Size.Y + le.Distance)
                    {
                        ChatPass cp = new ChatPass();
                        cp.ChatType = (ChatType)(byte)le.Type;
                        cp.Name = le.Name;
                        cp.Message = le.Message;
                        cp.Position = newPosition;
                        cp.ID = le.ID;
                        cp.SourceType = (ChatSourceType)(byte)le.SourceType;
                        cp.OwnerID = le.OwnerID;
                        cp.Channel = le.Channel;
                        kvp.Value.RemoteCircuit.SendMessage(cp);
                    }
                }
                else if (null != m_TryGetScene && m_TryGetScene(kvp.Key, out remoteScene))
                {
                    Vector3 newPosition = le.GlobalPosition + kvp.Value.RemoteOffset;
                    if (newPosition.X >= -le.Distance &&
                        newPosition.Y >= -le.Distance &&
                        newPosition.X <= kvp.Value.RemoteRegionData.Size.X + le.Distance &&
                        newPosition.Y <= kvp.Value.RemoteRegionData.Size.Y + le.Distance)
                    {
                        ListenEvent routedle = new ListenEvent(le);
                        routedle.OriginSceneID = ID;
                        routedle.GlobalPosition = newPosition;

                        remoteScene.SendChatPass(routedle);
                    }
                }
            }
        }

        protected virtual void SendChatPass(ListenEvent le)
        {

        }

        public virtual void NotifyNeighborOnline(RegionInfo rinfo)
        {
            VerifyNeighbor(rinfo);
        }

        public virtual void NotifyNeighborOffline(RegionInfo rinfo)
        {
            Neighbors.Remove(rinfo.ID);
        }

        void CheckAgentsForNeighbors()
        {
            foreach(IAgent ag in RootAgents)
            {
                if(ag.ActiveTeleportService == null)
                {
                }
            }
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        void VerifyNeighbor(RegionInfo rinfo)
        {
            if(rinfo.ServerURI == ServerURI)
            {
                if(!Neighbors.ContainsKey(rinfo.ID))
                {
                    NeighborEntry lne = new NeighborEntry();
                    lne.RemoteOffset = rinfo.Location - GridPosition;
                    lne.RemoteRegionData = rinfo;
                    Neighbors[rinfo.ID] = lne;
                    CheckAgentsForNeighbors();
                }
                return;
            }

            Dictionary<string, string> headers = new Dictionary<string,string>();
            try
            {
                using (Stream responseStream = HttpRequestHandler.DoStreamRequest("HEAD", rinfo.ServerURI + "helo", null, string.Empty, string.Empty, false, 20000, headers))
                {
                    using (StreamReader reader = new StreamReader(responseStream))
                    {
                        reader.ReadToEnd();
                    }
                }
            }
            catch
            {
                headers.Clear();
            }

            if(headers.ContainsKey("X-UDP-InterSim"))
            {
                /* neighbor supports UDP Inter-Sim connects */
                UUID randomID = UUID.Random;
                uint circuitID;
                NeighborEntry lne = new NeighborEntry();
                lne.RemoteOffset = rinfo.Location - GridPosition;
                lne.RemoteRegionData = rinfo;
                Neighbors[rinfo.ID] = lne;
                EnableSimCircuit(rinfo, out randomID, out circuitID);
            }
            else
            {
                /* we have to keep the original protocol which may be slow */
            }
        }

        void EnableSimCircuit(RegionInfo destinationInfo, out UUID sessionID, out uint circuitCode)
        {
            Map reqmap = new Map();
            reqmap["to_region_id"] = destinationInfo.ID;
            reqmap["from_region_id"] = ID;
            reqmap["scope_id"] = ScopeID;
            byte[] reqdata;
            using(MemoryStream ms = new MemoryStream())
            {
                LlsdXml.Serialize(reqmap, ms);
                reqdata = ms.ToArray();
            }

            /* try DNS lookup before triggering add circuit code */
            IPAddress[] addresses = Dns.GetHostAddresses(destinationInfo.ServerIP);
            IPEndPoint ep = new IPEndPoint(addresses[0], (int)destinationInfo.ServerPort);

            Map resmap;
            using(Stream responseStream = 
                HttpRequestHandler.DoStreamRequest(
                    "POST", 
                    destinationInfo.ServerURI + "circuit",
                    null,
                    "application/llsd+xml",
                    reqdata.Length,
                    delegate(Stream s)
                    {
                        s.Write(reqdata, 0, reqdata.Length);
                    },
                    false,
                    10000,
                    null))
            {
                resmap = (Map)LlsdXml.Deserialize(responseStream);
            }

            circuitCode = resmap["circuit_code"].AsUInt;
            sessionID = resmap["session_id"].AsUUID;
            ICircuit simCircuit = UDPServer.UseSimCircuit(
                ep, 
                sessionID, 
                this, 
                destinationInfo.ID, 
                circuitCode, 
                destinationInfo.Location, 
                destinationInfo.Location - GridPosition);
            Neighbors[destinationInfo.ID].RemoteCircuit = simCircuit;
            CheckAgentsForNeighbors();
        }
    }
}

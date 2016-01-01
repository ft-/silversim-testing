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

namespace SilverSim.Scene.Types.Scene
{
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

        public void ChatPassInbound(UUID fromRegionID, ListenEvent ev)
        {
            bool chatPassInboundDefault = true;
            if (null != ServerParamService)
            {
                chatPassInboundDefault = ServerParamService.GetBoolean(ID, "ChatPassInEnable", true);
                chatPassInboundDefault = ServerParamService.GetBoolean(fromRegionID, "ChatPassInEnable", chatPassInboundDefault);
                chatPassInboundDefault = ServerParamService.GetBoolean(ID, "ChatPassInEnable_" + ev.ID.ToString(), chatPassInboundDefault);
                chatPassInboundDefault = ServerParamService.GetBoolean(fromRegionID, "ChatPassInEnable_" + ev.ID.ToString(), chatPassInboundDefault);
            }
            if(chatPassInboundDefault)
            {
                SendChatPass(ev);
            }
        }

        protected void ChatPassLocalNeighbors(ListenEvent le)
        {
            bool chatPassDefault = true;
            if (null != ServerParamService)
            {
                chatPassDefault = ServerParamService.GetBoolean(ID, "ChatPassOutEnable", true);
            }
            foreach (KeyValuePair<UUID, NeighborEntry> kvp in Neighbors)
            {
                SceneInterface remoteScene;
                TryGetSceneDelegate m_TryGetScene = TryGetScene;
                if(null != ServerParamService)
                {
                    bool chatPass;
                    chatPass = ServerParamService.GetBoolean(kvp.Key, "ChatPassOutEnable", chatPassDefault);
                    chatPass = ServerParamService.GetBoolean(ID, "ChatPassOutEnable_" + le.ID.ToString(), chatPass);
                    chatPass = ServerParamService.GetBoolean(kvp.Key, "ChatPassOutEnable_" + le.ID.ToString(), chatPass);

                    if (!chatPass)
                    {
                        continue;
                    }
                }

                if(null != kvp.Value.RemoteCircuit)
                {
                    Vector3 newPosition = le.GlobalPosition + kvp.Value.RemoteOffset;;
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
                    Vector3 newPosition = le.GlobalPosition + kvp.Value.RemoteOffset;;
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
            if(rinfo.ServerURI == RegionData.ServerURI)
            {
                if(!Neighbors.ContainsKey(rinfo.ID))
                {
                    NeighborEntry lne = new NeighborEntry();
                    lne.RemoteOffset = rinfo.Location - RegionData.Location;
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
                lne.RemoteOffset = rinfo.Location - RegionData.Location;
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
            reqmap["scope_id"] = RegionData.ScopeID;
            byte[] reqdata;
            using(MemoryStream ms = new MemoryStream())
            {
                LlsdXml.Serialize(reqmap, ms);
                reqdata = ms.GetBuffer();
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
                destinationInfo.Location - RegionData.Location);
            Neighbors[destinationInfo.ID].RemoteCircuit = simCircuit;
            CheckAgentsForNeighbors();
        }
    }
}

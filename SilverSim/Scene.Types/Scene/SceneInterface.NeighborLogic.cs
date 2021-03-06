﻿// SilverSim is distributed under the terms of the
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

using SilverSim.Http.Client;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Grid;
using SilverSim.Types.StructuredData.Llsd;
using SilverSim.Viewer.Messages.Chat;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;

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

        public readonly RwLockedDictionary<UUID, NeighborEntry> Neighbors = new RwLockedDictionary<UUID, NeighborEntry>();

        public delegate bool TryGetSceneDelegate(UUID id, out SceneInterface scene);
        public TryGetSceneDelegate TryGetScene;

        private readonly RwLockedList<UUID> m_ChatPassInEnableLocal = new RwLockedList<UUID>();
        private readonly RwLockedList<UUID> m_ChatPassInEnableGlobal = new RwLockedList<UUID>();
        private bool m_ChatPassInEnableSetToLocal;

        private readonly RwLockedList<UUID> m_ChatPassOutEnableLocal = new RwLockedList<UUID>();
        private readonly RwLockedList<UUID> m_ChatPassOutEnableGlobal = new RwLockedList<UUID>();
        private bool m_ChatPassOutEnableSetToLocal;

        private void ChatPassEnableUpdated(
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
                var new_ids = new List<UUID>();
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

                var activelist = regionId != UUID.Zero ? locallist : globallist;

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
            if (ev.Type != ListenEvent.ChatType.Say &&
                ev.Type != ListenEvent.ChatType.Shout &&
                ev.Type != ListenEvent.ChatType.Whisper)
            {
                /* only pass those above */
                return;
            }
            var activelist = m_ChatPassInEnableSetToLocal ? m_ChatPassInEnableLocal : m_ChatPassInEnableGlobal;
            if(activelist.Count == 0 || activelist.Contains(UUID.Zero) || activelist.Contains(ev.ID))
            {
                SendChatPass(ev);
            }
        }

        protected void ChatPassLocalNeighbors(ListenEvent le)
        {
            if (le.Type != ListenEvent.ChatType.Say &&
                le.Type != ListenEvent.ChatType.Shout &&
                le.Type != ListenEvent.ChatType.Whisper)
            {
                /* only pass those above */
                return;
            }
            var activelist = m_ChatPassInEnableSetToLocal ? m_ChatPassInEnableLocal : m_ChatPassInEnableGlobal;
            foreach (var kvp in Neighbors)
            {
                SceneInterface remoteScene;
                var m_TryGetScene = TryGetScene;
                if(!(activelist.Count == 0 || activelist.Contains(le.ID) || activelist.Contains(UUID.Zero)))
                {
                    continue;
                }

                if(kvp.Value.RemoteCircuit != null)
                {
                    Vector3 newPosition = le.GlobalPosition + kvp.Value.RemoteOffset;
                    if (newPosition.X >= -le.Distance &&
                        newPosition.Y >= -le.Distance &&
                        newPosition.X <= kvp.Value.RemoteRegionData.Size.X + le.Distance &&
                        newPosition.Y <= kvp.Value.RemoteRegionData.Size.Y + le.Distance)
                    {
                        kvp.Value.RemoteCircuit.SendMessage(new ChatPass
                        {
                            ChatType = (ChatType)(byte)le.Type,
                            Name = le.Name,
                            Message = le.Message,
                            Position = newPosition,
                            ID = le.ID,
                            SourceType = (ChatSourceType)(byte)le.SourceType,
                            OwnerID = le.OwnerID,
                            Channel = le.Channel
                        });
                    }
                }
                else if (m_TryGetScene != null && m_TryGetScene(kvp.Key, out remoteScene))
                {
                    Vector3 newPosition = le.GlobalPosition + kvp.Value.RemoteOffset;
                    if (newPosition.X >= -le.Distance &&
                        newPosition.Y >= -le.Distance &&
                        newPosition.X <= kvp.Value.RemoteRegionData.Size.X + le.Distance &&
                        newPosition.Y <= kvp.Value.RemoteRegionData.Size.Y + le.Distance)
                    {
                        remoteScene.SendChatPass(new ListenEvent(le)
                        {
                            OriginSceneID = ID,
                            GlobalPosition = newPosition
                        });
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

        private void CheckAgentsForNeighbors()
        {
            foreach(IAgent ag in RootAgents)
            {
                if(ag.ActiveTeleportService == null)
                {
                }
            }
        }

        private void VerifyNeighbor(RegionInfo rinfo)
        {
            if(rinfo.ServerURI == ServerURI)
            {
                if(!Neighbors.ContainsKey(rinfo.ID))
                {
                    Neighbors[rinfo.ID] = new NeighborEntry
                    {
                        RemoteOffset = rinfo.Location - GridPosition,
                        RemoteRegionData = rinfo
                    };
                    CheckAgentsForNeighbors();
                }
                return;
            }

            var headers = new Dictionary<string,string>();
            try
            {
                new HttpClient.Head(rinfo.ServerURI + "helo")
                {
                    Headers = headers
                }.ExecuteRequest();
            }
            catch
            {
                headers.Clear();
            }

            if(headers.ContainsKey("X-UDP-InterSim"))
            {
                /* neighbor supports UDP Inter-Sim connects */
                var randomID = UUID.Random;
                uint circuitID;
                Neighbors[rinfo.ID] = new NeighborEntry
                {
                    RemoteOffset = rinfo.Location - GridPosition,
                    RemoteRegionData = rinfo
                };
                EnableSimCircuit(rinfo, out randomID, out circuitID);
            }
            else
            {
                /* we have to keep the original protocol which may be slow */
            }
        }

        private void EnableSimCircuit(RegionInfo destinationInfo, out UUID sessionID, out uint circuitCode)
        {
            var reqmap = new Map
            {
                ["to_region_id"] = destinationInfo.ID,
                ["from_region_id"] = ID
            };
            byte[] reqdata;
            using(var ms = new MemoryStream())
            {
                LlsdXml.Serialize(reqmap, ms);
                reqdata = ms.ToArray();
            }

            /* try DNS lookup before triggering add circuit code */
            IPAddress[] addresses = Dns.GetHostAddresses(destinationInfo.ServerIP);
            var ep = new IPEndPoint(addresses[0], (int)destinationInfo.ServerPort);

            Map resmap;
            using(var responseStream = new HttpClient.Post(
                destinationInfo.ServerURI + "circuit",
                "application/llsd+xml",
                reqdata.Length,
                (Stream s) => s.Write(reqdata, 0, reqdata.Length)).ExecuteStreamRequest())
            {
                resmap = (Map)LlsdXml.Deserialize(responseStream);
            }

            circuitCode = resmap["circuit_code"].AsUInt;
            sessionID = resmap["session_id"].AsUUID;
            Neighbors[destinationInfo.ID].RemoteCircuit = UDPServer.UseSimCircuit(
                ep,
                sessionID,
                this,
                destinationInfo.ID,
                circuitCode,
                destinationInfo.Location,
                destinationInfo.Location - GridPosition);
            CheckAgentsForNeighbors();
        }
    }
}

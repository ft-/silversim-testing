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
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Types.Scene;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Viewer.Core;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Search;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Threading;
using System.Xml;

namespace SilverSim.Viewer.Search
{
    [Description("Viewer Search Handler")]
    [PluginName("ViewerSearch")]
    public class ViewerSearch : IPlugin, IPacketHandlerExtender, ICapabilityExtender, IPluginShutdown
    {
        private static readonly ILog m_Log = LogManager.GetLogger("LL SEARCH");

        [PacketHandler(MessageType.AvatarPickerRequest)]
        [PacketHandler(MessageType.PlacesQuery)]
        [PacketHandler(MessageType.DirPlacesQuery)]
        [PacketHandler(MessageType.DirLandQuery)]
        [PacketHandler(MessageType.DirPopularQuery)]
        [PacketHandler(MessageType.DirFindQuery)]
        private readonly BlockingQueue<KeyValuePair<AgentCircuit, Message>> RequestQueue = new BlockingQueue<KeyValuePair<AgentCircuit, Message>>();

        private bool m_ShutdownSearch;

        public void Startup(ConfigurationLoader loader)
        {
            ThreadManager.CreateThread(HandlerThread).Start();
        }

        public void HandlerThread()
        {
            Thread.CurrentThread.Name = "Search Handler Thread";

            while(!m_ShutdownSearch)
            {
                KeyValuePair<AgentCircuit, Message> req;
                try
                {
                    req = RequestQueue.Dequeue(1000);
                }
                catch
                {
                    continue;
                }

                Message m = req.Value;

                try
                {
                    switch (m.Number)
                    {
                        case MessageType.DirFindQuery:
                            ProcessDirFindQuery(req.Key.Agent, req.Key, m);
                            break;

                        case MessageType.AvatarPickerRequest:
                            ProcessAvatarPickerRequest(req.Key.Agent, req.Key, m);
                            break;
                    }
                }
                catch(Exception e)
                {
                    m_Log.Debug("Unexpected exception " + e.Message, e);
                }
            }
        }

        [AgentCircuit.IgnoreMethod]
        private void ProcessDirFindQuery(ViewerAgent agent, AgentCircuit circuit, Message m)
        {
            var req = (DirFindQuery)m;
            SceneInterface scene = circuit.Scene;
            if(scene == null)
            {
                return;
            }

            if(req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }

            if((req.QueryFlags & SearchFlags.People) != 0)
            {
                ProcessDirFindQuery_People(agent, scene, req);
            }

            if((req.QueryFlags & SearchFlags.Groups) != 0)
            {
                ProcessDirFindQuery_Groups(agent, scene, req);
            }

            if ((req.QueryFlags & SearchFlags.Events) != 0)
            {
                ProcessDirFindQuery_Events(agent, scene, req);
            }
        }

        private void ProcessDirFindQuery_People(ViewerAgent agent, SceneInterface scene, DirFindQuery req)
        {
            DirPeopleReply res = null;
            var t = new UDPPacket();

            foreach(UUI uui in scene.AvatarNameService.Search(req.QueryText.Split(new char[] {' '}, 2)))
            {
                if(res == null)
                {
                    res = new DirPeopleReply()
                    {
                        AgentID = req.AgentID,
                        QueryID = req.QueryID
                    };
                }

                var d = new DirPeopleReply.QueryReplyData()
                {
                    AgentID = uui.ID
                };
                string[] parts = uui.FullName.Split(' ');
                d.FirstName = parts[0];
                if (parts.Length > 1)
                {
                    d.LastName = parts[1];
                }
                //d.Group
                //d.Online
                d.Reputation = 0;
                res.QueryReplies.Add(d);

                t.Reset();
                res.Serialize(t);
                if(t.DataLength >= 1400)
                {
                    agent.SendMessageAlways(res, scene.ID);
                    res = null;
                }
            }
            if(res != null)
            {
                agent.SendMessageAlways(res, scene.ID);
            }
        }

        private void ProcessDirFindQuery_Groups(ViewerAgent agent, SceneInterface scene, DirFindQuery req)
        {
            DirGroupsReply res = null;

            var groupsService = scene.GroupsService;
            if(groupsService == null)
            {
                res = new DirGroupsReply()
                {
                    AgentID = req.AgentID,
                    QueryID = req.QueryID
                };
                agent.SendMessageAlways(res, scene.ID);
                return;
            }

            var gis = groupsService.Groups.GetGroupsByName(agent.Owner, req.QueryText);
            if(gis.Count == 0)
            {
                res = new DirGroupsReply()
                {
                    AgentID = req.AgentID,
                    QueryID = req.QueryID
                };
                agent.SendMessageAlways(res, scene.ID);
                return;
            }
            var t = new UDPPacket();
            foreach (var gi in gis)
            {
                if (res == null)
                {
                    res = new DirGroupsReply()
                    {
                        AgentID = req.AgentID,
                        QueryID = req.QueryID
                    };
                }

                var d = new DirGroupsReply.QueryReplyData()
                {
                    GroupID = gi.ID.ID,
                    GroupName = gi.ID.GroupName,
                    Members = gi.MemberCount,
                    SearchOrder = gi.SearchOrder
                };
                res.QueryReplies.Add(d);

                t.Reset();
                res.Serialize(t);
                if (t.DataLength >= 1400)
                {
                    agent.SendMessageAlways(res, scene.ID);
                    res = null;
                }
            }
            if (res != null)
            {
                agent.SendMessageAlways(res, scene.ID);
            }
        }

        private void ProcessDirFindQuery_Events(ViewerAgent agent, SceneInterface scene, DirFindQuery req)
        {

        }

        [AgentCircuit.IgnoreMethod]
        private void ProcessAvatarPickerRequest(ViewerAgent agent, AgentCircuit circuit, Message m)
        {
            var req = (AvatarPickerRequest)m;
            var res = new AvatarPickerReply();
            var scene = circuit.Scene;
            if(scene == null)
            {
                return;
            }

            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }

            res.AgentID = req.AgentID;
            res.QueryID = req.QueryID;
            if (string.IsNullOrEmpty(req.Name) || req.Name.Length < 3)
            {
                agent.SendMessageAlways(res, scene.ID);
                return;
            }

            string[] names = req.Name.Split(' ');

            if(names.Length < 1 || names.Length > 2)
            {
                agent.SendMessageAlways(res, scene.ID);
                return;
            }

            var results = scene.AvatarNameService.Search(names);
            for(int offset = 0; offset < results.Count && offset < 100; ++offset)
            {
                string[] sp = results[offset].FullName.Split(new char[] {' '}, 2);
                var d = new AvatarPickerReply.DataEntry()
                {
                    AvatarID = results[offset].ID,
                    FirstName = sp[0],
                    LastName = (sp.Length > 1) ?
                    sp[1] :
                    string.Empty
                };
                res.Data.Add(d);
            }
            agent.SendMessageAlways(res, scene.ID);
        }

        [CapabilityHandler("AvatarPickerSearch")]
        public void HandleAvatarPickerSearchCapability(ViewerAgent agent, AgentCircuit circuit, HttpRequest req)
        {
            if (req.CallerIP != circuit.RemoteIP)
            {
                req.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }
            var parts = req.RawUrl.Substring(1).Split('/');
            if (req.Method != "GET")
            {
                req.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }
            if (parts[3].Substring(0, 1) != "?")
            {
                req.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            var query = parts[3].Substring(1);
            var queryreqs = query.Split('&');

            var names = string.Empty;
            var psize = string.Empty;
            var pnumber = string.Empty;

            foreach (string reqentry in queryreqs)
            {
                if (reqentry.StartsWith("names="))
                {
                    names = reqentry.Substring(6);
                }
                else if(reqentry.StartsWith("page_size="))
                {
                    psize = reqentry.Substring(10);
                }
                else if(reqentry.StartsWith("page="))
                {
                    pnumber = reqentry.Substring(5);
                }
            }

            if (string.IsNullOrEmpty(names) || names.Length < 3)
            {
                req.ErrorResponse(HttpStatusCode.NotFound, string.Empty);
                return;
            }

            int page_size = string.IsNullOrEmpty(psize) ? 500 : int.Parse(psize);
            int page_number = string.IsNullOrEmpty(pnumber) ? 1 : int.Parse(pnumber);
            SceneInterface scene = circuit.Scene;
            if(scene == null)
            {
                req.ErrorResponse(HttpStatusCode.NotFound, string.Empty);
                return;
            }

            var nameparts = names.Split(' ');
            if(nameparts.Length > 2 || nameparts.Length < 1)
            {
                req.ErrorResponse(HttpStatusCode.NotFound, string.Empty);
                return;
            }

            List<UUI> results = scene.AvatarNameService.Search(nameparts);
            using (HttpResponse res = req.BeginResponse("application/llsd+xml"))
            {
                using (XmlTextWriter writer = res.GetOutputStream().UTF8XmlTextWriter())
                {
                    writer.WriteStartElement("llsd");
                    writer.WriteStartElement("map");
                    {
                        writer.WriteNamedValue("key", "next_page_url");
                        writer.WriteNamedValue("string", req.RawUrl);
                        writer.WriteNamedValue("key", "agents");
                        writer.WriteStartElement("array");
                        for (int offset = page_number * page_size; offset < (page_number + 1) * page_size && offset < results.Count; ++page_size)
                        {
                            writer.WriteStartElement("map");
                            {
                                UUI uui = results[offset];
                                writer.WriteNamedValue("key", "username");
                                writer.WriteNamedValue("string", uui.FullName);
                                writer.WriteNamedValue("key", "display_name");
                                writer.WriteNamedValue("string", uui.FullName);
                                writer.WriteNamedValue("key", "legacy_first_name");
                                writer.WriteNamedValue("string", uui.FirstName);
                                writer.WriteNamedValue("key", "legacy_last_name");
                                writer.WriteNamedValue("string", uui.LastName);
                                writer.WriteNamedValue("key", "id");
                                writer.WriteNamedValue("uuid", uui.ID);
                                writer.WriteNamedValue("key", "is_display_name_default");
                                writer.WriteNamedValue("boolean", false);
                            }
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }
            }
        }

        public ShutdownOrder ShutdownOrder => ShutdownOrder.LogoutRegion;

        public void Shutdown()
        {
            m_ShutdownSearch = true;
        }
    }
}

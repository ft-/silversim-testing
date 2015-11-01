// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using Nini.Config;
using SilverSim.Viewer.Core;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Search;
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using SilverSim.Types.Groups;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Xml;
using ThreadedClasses;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Viewer.Search
{
    public class ViewerSearch : IPlugin, IPacketHandlerExtender, ICapabilityExtender, IPluginShutdown
    {
        private static readonly ILog m_Log = LogManager.GetLogger("LL SEARCH");

        [PacketHandler(MessageType.AvatarPickerRequest)]
        [PacketHandler(MessageType.PlacesQuery)]
        [PacketHandler(MessageType.DirPlacesQuery)]
        [PacketHandler(MessageType.DirLandQuery)]
        [PacketHandler(MessageType.DirPopularQuery)]
        [PacketHandler(MessageType.DirFindQuery)]
        BlockingQueue<KeyValuePair<AgentCircuit, Message>> RequestQueue = new BlockingQueue<KeyValuePair<AgentCircuit, Message>>();

        bool m_ShutdownSearch;

        public ViewerSearch()
        {
        }

        public void Startup(ConfigurationLoader loader)
        {
            new Thread(HandlerThread).Start();
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
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
        void ProcessDirFindQuery(ViewerAgent agent, AgentCircuit circuit, Message m)
        {
            DirFindQuery req = (DirFindQuery)m;
            SceneInterface scene = circuit.Scene;
            if(null == scene)
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

        void ProcessDirFindQuery_People(ViewerAgent agent, SceneInterface scene, DirFindQuery req)
        {
            DirPeopleReply res = null;
            UDPPacket t = new UDPPacket();

            List<UUI> uuis = scene.AvatarNameService.Search(req.QueryText.Split(new char[] {' '}, 2));
            foreach(UUI uui in uuis)
            {
                if(null == res)
                {
                    res = new DirPeopleReply();
                    res.AgentID = req.AgentID;
                    res.QueryID = req.QueryID;
                }

                DirPeopleReply.QueryReplyData d = new DirPeopleReply.QueryReplyData();
                d.AgentID = uui.ID;
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

        void ProcessDirFindQuery_Groups(ViewerAgent agent, SceneInterface scene, DirFindQuery req)
        {
            DirGroupsReply res = null;

            GroupsServiceInterface groupsService = scene.GroupsService;
            if(null == groupsService)
            {
                res = new DirGroupsReply();
                res.AgentID = req.AgentID;
                res.QueryID = req.QueryID;
                agent.SendMessageAlways(res, scene.ID);
                return;
            }

            List<DirGroupInfo> gis = groupsService.Groups.GetGroupsByName(agent.Owner, req.QueryText);
            if(gis.Count == 0)
            {
                res = new DirGroupsReply();
                res.AgentID = req.AgentID;
                res.QueryID = req.QueryID;
                agent.SendMessageAlways(res, scene.ID);
                return;
            }
            UDPPacket t = new UDPPacket();
            foreach (DirGroupInfo gi in gis)
            {
                if (null == res)
                {
                    res = new DirGroupsReply();
                    res.AgentID = req.AgentID;
                    res.QueryID = req.QueryID;
                }

                DirGroupsReply.QueryReplyData d = new DirGroupsReply.QueryReplyData();
                d.GroupID = gi.ID.ID;
                d.GroupName = gi.ID.GroupName;
                d.Members = gi.MemberCount;
                d.SearchOrder = gi.SearchOrder;

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

        void ProcessDirFindQuery_Events(ViewerAgent agent, SceneInterface scene, DirFindQuery req)
        {

        }

        [AgentCircuit.IgnoreMethod]
        void ProcessAvatarPickerRequest(ViewerAgent agent, AgentCircuit circuit, Message m)
        {
            AvatarPickerRequest req = (AvatarPickerRequest)m;
            AvatarPickerReply res = new AvatarPickerReply();
            SceneInterface scene = circuit.Scene;
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

            List<UUI> results = scene.AvatarNameService.Search(names);
            int offset = 0;
            for(offset = 0; offset < results.Count && offset < 100; ++offset)
            {
                AvatarPickerReply.DataEntry d = new AvatarPickerReply.DataEntry();
                d.AvatarID = results[offset].ID;
                string[] sp = results[offset].FullName.Split(new char[] {' '}, 2);
                d.FirstName = sp[0];
                if (sp.Length > 1)
                {
                    d.LastName = sp[1];
                }
                else
                {
                    d.LastName = string.Empty;
                }
                res.Data.Add(d);
            }
            agent.SendMessageAlways(res, scene.ID);
        }

        [CapabilityHandler("AvatarPickerSearch")]
        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public void HandleAvatarPickerSearchCapability(ViewerAgent agent, AgentCircuit circuit, HttpRequest req)
        {
            string[] parts = req.RawUrl.Substring(1).Split('/');
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

            string query = parts[3].Substring(1);
            string[] queryreqs = query.Split('&');

            string names = string.Empty;
            string psize = string.Empty;
            string pnumber = string.Empty;

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

            int page_size = (string.IsNullOrEmpty(psize) ? 500 : int.Parse(psize));
            int page_number = (string.IsNullOrEmpty(pnumber) ? 1 : int.Parse(pnumber));
            SceneInterface scene = circuit.Scene;
            if(scene == null)
            {
                req.ErrorResponse(HttpStatusCode.NotFound, string.Empty);
                return;
            }

            string[] nameparts = names.Split(' ');
            if(nameparts.Length > 2 || nameparts.Length < 1)
            {
                req.ErrorResponse(HttpStatusCode.NotFound, string.Empty);
                return;
            }

            List<UUI> results = scene.AvatarNameService.Search(nameparts);
            using (HttpResponse res = req.BeginResponse("application/llsd+xml"))
            {
                using (XmlTextWriter writer = new XmlTextWriter(res.GetOutputStream(), UTF8NoBOM))
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

        static UTF8Encoding UTF8NoBOM = new UTF8Encoding(false);

        public ShutdownOrder ShutdownOrder
        {
            get 
            {
                return ShutdownOrder.LogoutRegion;
            }
        }

        public void Shutdown()
        {
            m_ShutdownSearch = true;
        }
    }

    [PluginName("ViewerSearch")]
    public class Factory : IPluginFactory
    {
        public Factory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new ViewerSearch();
        }
    }
}

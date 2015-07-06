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

using Nini.Config;
using SilverSim.LL.Core;
using SilverSim.LL.Messages;
using SilverSim.LL.Messages.Search;
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Xml;
using ThreadedClasses;

namespace SilverSim.LL.Search
{
    public class ViewerSearch : IPlugin, IPacketHandlerExtender, ICapabilityExtender
    {
        BlockingQueue<Message> RequestQueue = new BlockingQueue<Message>();
        public ViewerSearch()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {
        }

        [PacketHandler(MessageType.PlacesQuery)]
        [PacketHandler(MessageType.DirPlacesQuery)]
        [PacketHandler(MessageType.DirLandQuery)]
        [PacketHandler(MessageType.DirPopularQuery)]
        public void HandleMessage(Message m)
        {

        }

        [PacketHandler(MessageType.DirFindQuery)]
        void ProcessDirFindQuery(LLAgent agent, Circuit circuit, Message m)
        {

        }

        [PacketHandler(MessageType.AvatarPickerRequest)]
        void ProcessAvatarPickerRequest(LLAgent agent, Circuit circuit, Message m)
        {
#warning async this service here
            AvatarPickerRequest req = (AvatarPickerRequest)m;
            AvatarPickerReply res = new AvatarPickerReply();
            SceneInterface scene = circuit.Scene;
            if(scene == null)
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
                    d.LastName = "";
                }
                res.Data.Add(d);
            }
            agent.SendMessageAlways(res, scene.ID);
        }

        [CapabilityHandler("AvatarPickerSearch")]
        public void HandleAvatarPickerSearchCapability(LLAgent agent, Circuit circuit, HttpRequest req)
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
                req.ErrorResponse(HttpStatusCode.NotFound, "");
                return;
            }

            int page_size = (string.IsNullOrEmpty(psize) ? 500 : int.Parse(psize));
            int page_number = (string.IsNullOrEmpty(pnumber) ? 1 : int.Parse(pnumber));
            SceneInterface scene = circuit.Scene;
            if(scene == null)
            {
                req.ErrorResponse(HttpStatusCode.NotFound, "");
                return;
            }

            string[] nameparts = names.Split(' ');
            if(nameparts.Length > 2 || nameparts.Length < 1)
            {
                req.ErrorResponse(HttpStatusCode.NotFound, "");
                return;
            }

            List<UUI> results = scene.AvatarNameService.Search(nameparts);
            HttpResponse res = req.BeginResponse("application/llsd+xml");
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

        static UTF8Encoding UTF8NoBOM = new UTF8Encoding(false);
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

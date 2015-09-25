// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common.HttpServer;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Net;
using System.Xml;

namespace SilverSim.LL.Core
{
    public partial class AgentCircuit
    {
        void WriteAvatarNameData(XmlTextWriter writer, UUI nd)
        {
            writer.WriteStartElement("map");
#warning verify expected username handling with viewer source code (GetDisplayNames capability)
            string utcstring = DateTime.Now.AddDays(1).ToUniversalTime().ToString("yyyy\\-MM\\-dd\\THH\\-mm\\-ss\\Z");
            writer.WriteKeyValuePair("username", nd.FullName.Replace(' ', '.'));
            writer.WriteKeyValuePair("display_name", nd.FullName);
            writer.WriteKeyValuePair("display_name_next_update", utcstring);
            writer.WriteKeyValuePair("legacy_first_name", nd.FirstName);
            writer.WriteKeyValuePair("legacy_last_name", nd.LastName);
            writer.WriteKeyValuePair("id", nd.ID);
            writer.WriteKeyValuePair("is_display_name_default", false);
            writer.WriteEndElement();
        }

        void Cap_GetDisplayNames(HttpRequest httpreq)
        {
            if (httpreq.Method != "GET")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            string[] parts = httpreq.RawUrl.Split('?');
            if(parts.Length < 2)
            {
                m_Log.WarnFormat("Invalid GetDisplayNames request: {0}", httpreq.RawUrl);
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                return;
            }

            string[] reqs = parts[1].Split('&');
            List<UUID> uuids = new List<UUID>();
            List<string> names = new List<string>();
            List<string> baduuids = new List<string>();
            List<string> badnames = new List<string>();
            foreach(string req in reqs)
            {
                string[] p = req.Split('=');
                if(p.Length == 2)
                {
                    if(p[0] == "ids")
                    {
                        try
                        {
                            UUID uuid = p[1];
                            if (!uuids.Contains(uuid))
                            {
                                uuids.Add(uuid);
                            }
                        }
                        catch
                        {
                            baduuids.Add(p[1]);
                        }
                    }
                    else if(p[0] == "username")
                    {
                        names.Add(p[1]);
                    }
                }
            }

            HttpResponse res = httpreq.BeginResponse();
            XmlTextWriter text = new XmlTextWriter(res.GetOutputStream(), UTF8NoBOM);
            text.WriteStartElement("llsd");
            text.WriteStartElement("map");

            bool haveAgents = false;

            foreach(UUID id in uuids)
            {
                UUI nd;
                try
                {
                    nd = Scene.AvatarNameService[id];
                }
                catch
                {
                    baduuids.Add((string)id);
                    continue;
                }
                if (!haveAgents)
                {
                    text.WriteNamedValue("key", "agents");
                    text.WriteStartElement("array");
                    haveAgents = true;
                }

                WriteAvatarNameData(text, nd);
            }

            foreach(string name in names)
            {
                UUI nd;
                string[] nameparts = name.Split('.');
                if(nameparts.Length < 2)
                {
                    nameparts = new string[] { nameparts[0], "" };
                }
                try
                {
                    nd = Scene.AvatarNameService[nameparts[0], nameparts[1]];
                }
                catch
                {
                    badnames.Add(name);
                    continue;
                }
                if (!haveAgents)
                {
                    text.WriteNamedValue("key", "haveAgents");
                    text.WriteStartElement("array");
                    haveAgents = true;
                }

                WriteAvatarNameData(text, nd);
            }

            if(haveAgents)
            {
                text.WriteEndElement();
            }

            if(baduuids.Count != 0)
            {
                text.WriteKeyValuePair("key", "bad_ids");
                text.WriteStartElement("array");
                foreach(UUID id in baduuids)
                {
                    text.WriteKeyValuePair("uuid", id);
                }
                text.WriteEndElement();
            }

            if (badnames.Count != 0)
            {
                text.WriteKeyValuePair("key", "bad_names");
                text.WriteStartElement("array");
                foreach (string name in badnames)
                {
                    text.WriteKeyValuePair("string", name);
                }
                text.WriteEndElement();
            }
            text.WriteEndElement();
            text.WriteEndElement();
            text.Flush();

            res.Close();
        }
    }
}

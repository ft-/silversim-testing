﻿/*

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

using SilverSim.Main.Common.HttpServer;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Net;
using System.Xml;

namespace SilverSim.LL.Core
{
    public partial class Circuit
    {
        void WriteAvatarNameData(XmlTextWriter writer, AvatarNameServiceInterface.NameData nd)
        {
            writer.WriteStartElement("map");
            WriteKeyValuePair(writer, "key", "username");
            string username;
            if(string.IsNullOrEmpty(nd.ID.LastName))
            {
                username = nd.ID.FirstName;
            }
            else
            {
                username = nd.ID.FirstName + "." + nd.ID.LastName;
            }
            string utcstring = DateTime.Now.AddDays(1).ToUniversalTime().ToString("yyyy\\-MM\\-dd\\THH\\-mm\\-ss\\Z");
            WriteKeyValuePair(writer, "string", username);
            WriteKeyValuePair(writer, "key", "display_name");
            WriteKeyValuePair(writer, "string", username);
            WriteKeyValuePair(writer, "key", "display_name_next_update");
            WriteKeyValuePair(writer, "string", utcstring);
            WriteKeyValuePair(writer, "key", "legacy_first_name");
            WriteKeyValuePair(writer, "string", nd.ID.FirstName);
            WriteKeyValuePair(writer, "key", "legacy_last_name");
            WriteKeyValuePair(writer, "string", nd.ID.LastName);
            WriteKeyValuePair(writer, "key", "id");
            WriteKeyValuePair(writer, "uuid", nd.ID.ID);
            WriteKeyValuePair(writer, "key", "is_display_name_default");
            WriteKeyValuePair(writer, "boolean", false);
            writer.WriteEndElement();
        }

        void Cap_GetDisplayNames(HttpRequest httpreq)
        {
            if (httpreq.Method != "GET")
            {
                httpreq.BeginResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed").Close();
                return;
            }

            string[] parts = httpreq.RawUrl.Split('?');
            if(parts.Length < 2)
            {
                m_Log.WarnFormat("Invalid GetDisplayNames request: {0}", httpreq.RawUrl);
                httpreq.BeginResponse(HttpStatusCode.BadRequest, "Bad Request").Close();
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
                AvatarNameServiceInterface.NameData nd;
                try
                {
                    nd = Scene.AvatarNameService[id];
                }
                catch
                {
                    baduuids.Add(id);
                    continue;
                }
                if (!haveAgents)
                {
                    WriteKeyValuePair(text, "key", "haveAgents");
                    text.WriteStartElement("array");
                    haveAgents = true;
                }

                WriteAvatarNameData(text, nd);
            }

            foreach(string name in names)
            {
                AvatarNameServiceInterface.NameData nd;
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
                    WriteKeyValuePair(text, "key", "haveAgents");
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
                WriteKeyValuePair(text, "key", "bad_ids");
                text.WriteStartElement("array");
                foreach(UUID id in baduuids)
                {
                    WriteKeyValuePair(text, "uuid", id);
                }
                text.WriteEndElement();
            }

            if (badnames.Count != 0)
            {
                WriteKeyValuePair(text, "key", "bad_names");
                text.WriteStartElement("array");
                foreach (string name in badnames)
                {
                    WriteKeyValuePair(text, "string", name);
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

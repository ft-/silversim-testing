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

using SilverSim.Main.Common.HttpServer;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Net;
using System.Xml;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
    {
        private void WriteAvatarNameData(XmlTextWriter writer, UUI nd)
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

        private void Cap_GetDisplayNames(HttpRequest httpreq)
        {
            if (httpreq.CallerIP != RemoteIP)
            {
                httpreq.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }
            if (httpreq.Method != "GET")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            var parts = httpreq.RawUrl.Split('?');
            if(parts.Length < 2)
            {
                m_Log.WarnFormat("Invalid GetDisplayNames request: {0}", httpreq.RawUrl);
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                return;
            }

            var reqs = parts[1].Split('&');
            var uuids = new List<UUID>();
            var names = new List<string>();
            var baduuids = new List<UUID>();
            var badnames = new List<string>();
            foreach(var req in reqs)
            {
                var p = req.Split('=');
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

            using (var res = httpreq.BeginResponse("application/llsd+xml"))
            {
                using (var text = res.GetOutputStream().UTF8XmlTextWriter())
                {
                    text.WriteStartElement("llsd");
                    text.WriteStartElement("map");

                    var haveAgents = false;

                    foreach (var id in uuids)
                    {
                        UUI nd;
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
                            text.WriteNamedValue("key", "agents");
                            text.WriteStartElement("array");
                            haveAgents = true;
                        }

                        WriteAvatarNameData(text, nd);
                    }

                    foreach (var name in names)
                    {
                        UUI nd;
                        var nameparts = name.Split('.');
                        if (nameparts.Length < 2)
                        {
                            nameparts = new string[] { nameparts[0], string.Empty };
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

                    if (haveAgents)
                    {
                        text.WriteEndElement();
                    }

                    if (baduuids.Count != 0)
                    {
                        text.WriteKeyValuePair("key", "bad_ids");
                        text.WriteStartElement("array");
                        foreach (var id in baduuids)
                        {
                            text.WriteKeyValuePair("uuid", id);
                        }
                        text.WriteEndElement();
                    }

                    if (badnames.Count != 0)
                    {
                        text.WriteKeyValuePair("key", "bad_names");
                        text.WriteStartElement("array");
                        foreach (var name in badnames)
                        {
                            text.WriteKeyValuePair("string", name);
                        }
                        text.WriteEndElement();
                    }
                    text.WriteEndElement();
                    text.WriteEndElement();
                }
            }
        }
    }
}

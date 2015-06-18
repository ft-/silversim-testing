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

using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Types.WindLight;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;

namespace SilverSim.LL.Core
{
    public partial class Circuit
    {
        public void Cap_EnvironmentSettings(HttpRequest httpreq)
        {
            switch(httpreq.Method)
            {
                case "GET":
                    Cap_EnvironmentSettings_GET(httpreq);
                    break;

                case "POST":
                    Cap_EnvironmentSettings_POST(httpreq);
                    break;

                default:
                    httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method Not Allowed");
                    break;
            }
        }

        public void Cap_EnvironmentSettings_GET(HttpRequest httpreq)
        {
            HttpResponse httpres = httpreq.BeginResponse("application/llsd+xml");
            EnvironmentSettings settings = Scene.EnvironmentSettings;
            if(settings != null)
            {
                settings.Serialize(httpres.GetOutputStream(), Scene.ID);
                httpres.Close();
            }
            else
            {
                using(XmlTextWriter writer = new XmlTextWriter(httpres.GetOutputStream(), UTF8NoBOM))
                {
                    writer.WriteStartElement("llsd");
                    {
                        writer.WriteStartElement("array");
                        {
                            writer.WriteStartElement("map");
                            {
                                writer.WriteNamedValue("key", "messageID");
                                writer.WriteNamedValue("uuid", UUID.Zero);
                                writer.WriteNamedValue("key", "regionID");
                                writer.WriteNamedValue("uuid", Scene.ID);
                            }
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }
                httpres.Close();
            }
        }

        public void EnvironmentPostResponse(HttpRequest httpreq, UUID messageID, bool success, string failreason)
        {
            HttpResponse httpres = httpreq.BeginResponse("application/llsd+xml");
            using(XmlTextWriter writer = new XmlTextWriter(httpres.GetOutputStream(), UTF8NoBOM))
            {
                writer.WriteStartElement("llsd");
                {
                    writer.WriteStartElement("map");
                    {
                        writer.WriteNamedValue("key", "regionID");
                        writer.WriteNamedValue("uuid", Scene.ID);
                        writer.WriteNamedValue("key", "messageID");
                        writer.WriteNamedValue("uuid", messageID);
                        writer.WriteNamedValue("key", "success");
                        writer.WriteNamedValue("boolean", success);
                        writer.WriteNamedValue("key", "fail_reason");
                        writer.WriteNamedValue("string", failreason);
                    }
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            httpres.Close();
        }

        public void Cap_EnvironmentSettings_POST(HttpRequest httpreq)
        {
            EnvironmentSettings envsettings;
            try
            {
                envsettings = EnvironmentSettings.Deserialize(httpreq.Body);
            }
            catch
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                return;
            }

            EnvironmentPostResponse(httpreq, UUID.Zero, false, "Setting not yet supported");
        }
    }
}

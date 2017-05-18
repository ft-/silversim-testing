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
using SilverSim.Scene.Types.WindLight;
using SilverSim.Types;
using System;
using System.Net;
using System.Xml;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
    {
        public void Cap_EnvironmentSettings(HttpRequest httpreq)
        {
            if (httpreq.CallerIP != RemoteIP)
            {
                httpreq.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }
            switch (httpreq.Method)
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
            using (var httpres = httpreq.BeginResponse("application/llsd+xml"))
            {
                var settings = Scene.EnvironmentSettings;
                if (settings != null)
                {
                    settings.Serialize(httpres.GetOutputStream(), Scene.ID);
                    httpres.Close();
                }
                else
                {
                    using (XmlTextWriter writer = httpres.GetOutputStream().UTF8XmlTextWriter())
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
                }
            }
        }

        public void EnvironmentPostResponse(HttpRequest httpreq, UUID messageID, bool success, string failreason)
        {
            using (var httpres = httpreq.BeginResponse("application/llsd+xml"))
            {
                using (var writer = httpres.GetOutputStream().UTF8XmlTextWriter())
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
            }
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

            if (Scene.IsEstateManager(Agent.Owner) || (Agent.IsActiveGod && Agent.IsInScene(Scene)))
            {
                try
                {
                    Scene.EnvironmentSettings = envsettings;
                    /* reading back EnvironmentSettings happens by RegionInfo UDP message */
                    /* Viewer triggers that by updating RegionInfo through UDP message */
                }
                catch (Exception e)
                {
                    m_Log.ErrorFormat("Exception when storing Environment settings: {0}: {1}\n{2}",
                        e.GetType().FullName,
                        e.Message,
                        e.StackTrace);
                    EnvironmentPostResponse(httpreq, UUID.Zero, false, this.GetLanguageString(Agent.CurrentCulture, "InternalError", "Internal Error"));
                    return;
                }
                try
                {
                    Scene.TriggerStoreOfEnvironmentSettings();
                }
                catch(Exception e)
                {
                    /* ensure no exceptions from line before is passed on */
                    m_Log.ErrorFormat("Exception when triggering actual store of Environment settings: {0}: {1}\n{2}",
                        e.GetType().FullName,
                        e.Message,
                        e.StackTrace);
                }
                EnvironmentPostResponse(httpreq, UUID.Zero, true, string.Empty);
            }
            else
            {
                EnvironmentPostResponse(httpreq, UUID.Zero, false, this.GetLanguageString(Agent.CurrentCulture, "InsufficientPermissionsEnvironmentSettingsHaveNotBeenSaved", "Insufficient estate permissions, settings have not been saved."));
            }
        }
    }
}

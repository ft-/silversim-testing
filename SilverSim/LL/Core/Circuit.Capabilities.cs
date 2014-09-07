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
using SilverSim.StructuredData.LLSD;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Xml;

namespace SilverSim.LL.Core
{
    public partial class Circuit
    {
        #region Capabilities registration
        public void AddCapability(string type, UUID id, Action<HttpRequest> del)
        {
            m_RegisteredCapabilities.Add(type, id);
            try
            {
                m_CapsRedirector.Caps[type].Add(id, del);
            }
            catch
            {
                m_RegisteredCapabilities.Remove(type);
                throw;
            }
        }

        public bool RemoveCapability(string type)
        {
            UUID id;
            if (m_RegisteredCapabilities.Remove(type, out id))
            {
                return m_CapsRedirector.Caps[type].Remove(id);
            }
            return false;
        }

        void WriteKeyValuePair(XmlTextWriter writer, string key, string value)
        {
            writer.WriteStartElement("key");
            writer.WriteValue(key);
            writer.WriteEndElement();
            writer.WriteStartElement("string");
            writer.WriteValue(value);
            writer.WriteEndElement();
        }

        void WriteKeyValuePair(XmlTextWriter writer, string key, uint value)
        {
            writer.WriteStartElement("key");
            writer.WriteValue(key);
            writer.WriteEndElement();
            writer.WriteStartElement("integer");
            writer.WriteValue(value);
            writer.WriteEndElement();
        }

        void WriteKeyValuePair(XmlTextWriter writer, string key, int value)
        {
            writer.WriteStartElement("key");
            writer.WriteValue(key);
            writer.WriteEndElement();
            writer.WriteStartElement("integer");
            writer.WriteValue(value);
            writer.WriteEndElement();
        }

        void WriteKeyValuePair(XmlTextWriter writer, string key, float value)
        {
            writer.WriteStartElement("key");
            writer.WriteValue(key);
            writer.WriteEndElement();
            writer.WriteStartElement("real");
            writer.WriteValue(value);
            writer.WriteEndElement();
        }

        void WriteKeyValuePair(XmlTextWriter writer, string key, bool value)
        {
            writer.WriteStartElement("key");
            writer.WriteValue(key);
            writer.WriteEndElement();
            writer.WriteStartElement("boolean");
            writer.WriteValue(value ? "1" : "0");
            writer.WriteEndElement();
        }

        void WriteKeyValuePair(XmlTextWriter writer, string key, double value)
        {
            writer.WriteStartElement("key");
            writer.WriteValue(key);
            writer.WriteEndElement();
            writer.WriteStartElement("real");
            writer.WriteValue((float)value);
            writer.WriteEndElement();
        }

        void WriteKeyValuePair(XmlTextWriter writer, string key, UUID value)
        {
            writer.WriteStartElement("key");
            writer.WriteValue(key);
            writer.WriteEndElement();
            writer.WriteStartElement("uuid");
            writer.WriteValue(value);
            writer.WriteEndElement();
        }

        public void RegionSeedHandler(HttpRequest httpreq)
        {
            IValue o;
            if (httpreq.Method != "POST")
            {
                httpreq.BeginResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed").Close();
                return;
            }

            try
            {
                o = LLSD_XML.Deserialize(httpreq.Body);
            }
            catch (Exception e)
            {
                m_Log.WarnFormat("Invalid LLSD_XML: {0} {1}", e.Message, e.StackTrace.ToString());
                httpreq.BeginResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML").Close();
                return;
            }
            if (!(o is AnArray))
            {
                httpreq.BeginResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML").Close();
                return;
            }

            Dictionary<string, string> capsUri = new Dictionary<string, string>();
            foreach (IValue v in (AnArray)o)
            {
                UUID capsID;
                string capsUriStr = string.Empty;
                if (v.ToString() == "SEED")
                {

                }
                else if(Scene.CapabilitiesConfig.TryGetValue(v.ToString(), out capsUriStr) && capsUriStr != "localhost")
                {
                    if(capsUriStr=="")
                    {

                    }
                    else
                    {
                        char l = (char)0;
                        string uri = string.Empty;
                        foreach(char c in capsUriStr)
                        {
                            if(l == '%')
                            {
                                l = (char)0;
                                switch(c)
                                {
                                    case '%':
                                        uri += '%';
                                        break;

                                    case 'h':
                                        uri += System.Uri.EscapeUriString(Agent.HomeURI.ToString());
                                        break;

                                    case 'i':
                                        uri += System.Uri.EscapeUriString(Agent.ServiceURLs["InventoryServerURI"]);
                                        break;

                                    case 'r':
                                        uri += System.Uri.EscapeUriString(Scene.ID);
                                        break;

                                    case 's':
                                        uri += System.Uri.EscapeUriString(SessionID);
                                        break;

                                    case 'u':
                                        uri += System.Uri.EscapeUriString(AgentID);
                                        break;
                                }
                            }
                            else if(c=='%')
                            {
                                l = '%';
                            }
                            else
                            {
                                uri += c;
                            }
                        }
                        capsUri[v.ToString()] = uri;
                    }
                }
                else if (m_RegisteredCapabilities.TryGetValue(v.ToString(), out capsID))
                {
                    capsUri[v.ToString()] = string.Format("http://{0}:{1}/CAPS/{2}/{3}", m_CapsRedirector.ExternalHostName, m_CapsRedirector.Port,
                        v.ToString(), capsID);
                }
            }

            HttpResponse res = httpreq.BeginResponse();
            res.ContentType = "application/llsd+xml";
            Stream tw = res.GetOutputStream();
            XmlTextWriter text = new XmlTextWriter(tw, UTF8NoBOM);
            text.WriteStartElement("llsd");
            text.WriteStartElement("map");
            foreach (KeyValuePair<string, string> kvp in capsUri)
            {
                WriteKeyValuePair(text, kvp.Key, kvp.Value);
            }
            text.WriteEndElement();
            text.WriteEndElement();
            text.Flush();

            res.Close();
        }
        #endregion

        #region Default Capabilities
        private bool IsLocalHost(Dictionary<string, string> capConfig, string key)
        {
            string val;
            if(!capConfig.TryGetValue(key, out val))
            {
                return true;
            }
            return val == "localhost";
        }

        public void AddDefCapability(string capabilityType, UUID seedID, Action<HttpRequest> del, Dictionary<string, string> capConfig)
        {
            if (IsLocalHost(capConfig, capabilityType))
            {
                AddCapability(capabilityType, seedID, del);
            }
        }

        public void SetupDefaultCapabilities(UUID regionSeedID, Dictionary<string, string> capConfig)
        {
            AddDefCapability("FetchInventory2", regionSeedID, Cap_FetchInventory2, capConfig);
            AddDefCapability("FetchLib2", regionSeedID, Cap_FetchInventory2, capConfig);
            AddDefCapability("FetchInventoryDescendents2", regionSeedID, Cap_FetchInventoryDescendents2, capConfig);
            AddDefCapability("FetchLibDescendents2", regionSeedID, Cap_FetchInventoryDescendents2, capConfig);
            AddDefCapability("GetTexture", regionSeedID, Cap_GetTexture, capConfig);
            AddDefCapability("GetMesh", regionSeedID, Cap_GetMesh, capConfig);
        }
        #endregion
    }
}

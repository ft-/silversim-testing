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

using SilverSim.LL.Core.Capabilities;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Common;
using SilverSim.StructuredData.LLSD;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
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

        Dictionary<string, string> m_ServiceURLCapabilities = new Dictionary<string, string>();


        bool GetCustomCapsUri(string capType, out string uri)
        {
            string capsUriStr;
            uri = string.Empty;
            if(capType == "EventQueueGet")
            {
            }
            else if (m_ServiceURLCapabilities.TryGetValue(capType, out uri))
            {
                return true;
            }
            else if (Scene.CapabilitiesConfig.TryGetValue(capType, out capsUriStr) && uri != "localhost")
            {
                if (uri == "")
                {
                    return true;
                }
                else
                {
                    char l = (char)0;
                    foreach(char c in capsUriStr)
                    {
                        if(l == '$')
                        {
                            l = (char)0;
                            switch(c)
                            {
                                case '$':
                                    uri += '$';
                                    break;

                                case 'h':
                                    uri += System.Uri.EscapeUriString(Agent.HomeURI.ToString());
                                    break;

                                case 'i':
                                    uri += System.Uri.EscapeUriString(Agent.ServiceURLs["InventoryServerURI"]);
                                    break;

                                case 'a':
                                    uri += System.Uri.EscapeUriString(Agent.ServiceURLs["AssetServerURI"]);
                                    break;

                                case 'r':
                                    uri += System.Uri.EscapeUriString((string)Scene.ID);
                                    break;

                                case 's':
                                    uri += System.Uri.EscapeUriString((string)SessionID);
                                    break;

                                case 'u':
                                    uri += System.Uri.EscapeUriString((string)AgentID);
                                    break;
                            }
                        }
                        else if (c == '$')
                        {
                            l = '$';
                        }
                        else
                        {
                            uri += c;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        public void RegionSeedHandler(HttpRequest httpreq)
        {
            IValue o;
            if (httpreq.Method != "POST")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            try
            {
                o = LLSD_XML.Deserialize(httpreq.Body);
            }
            catch (Exception e)
            {
                m_Log.WarnFormat("Invalid LLSD_XML: {0} {1}", e.Message, e.StackTrace.ToString());
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML");
                return;
            }
            if (!(o is AnArray))
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML");
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
                else if(GetCustomCapsUri(v.ToString(), out capsUriStr))
                {
                    capsUri[v.ToString()] = capsUriStr;
                }
                else if (m_RegisteredCapabilities.TryGetValue(v.ToString(), out capsID))
                {
                    capsUri[v.ToString()] = string.Format("{0}://{1}:{2}/CAPS/{3}/{4}",
                        m_CapsRedirector.Scheme, m_CapsRedirector.ExternalHostName, m_CapsRedirector.Port,
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
                text.WriteKeyValuePair(kvp.Key, kvp.Value);
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

        public delegate ICapabilityInterface DefCapabilityInstantiate(LLAgent agent);
        public void AddDefCapabilityFactory(string capabilityType, UUID seedID, DefCapabilityInstantiate del, Dictionary<string, string> capConfig)
        {
            if(IsLocalHost(capConfig, capabilityType))
            {
                AddCapability(capabilityType, seedID, del(Agent).HttpRequestHandler);
            }
        }

        public void AddExtenderCapability(string capabilityType, UUID seedID, CapabilityHandler.CapabilityDelegate del, Dictionary<string, string> capConfig)
        {
            if (IsLocalHost(capConfig, capabilityType))
            {
                AddCapability(capabilityType, seedID, new ExtenderCapabilityCaller(Agent, this, del).HttpRequestHandler);
            }
        }

        class ExtenderCapabilityCaller
        {
            /* Weak reference kills the hard referencing */
            WeakReference m_Agent;
            WeakReference m_Circuit;
            CapabilityHandler.CapabilityDelegate m_Delegate;

            public ExtenderCapabilityCaller(LLAgent agent, Circuit circuit, CapabilityHandler.CapabilityDelegate del)
            {
                m_Agent = new WeakReference(agent, false);
                m_Circuit = new WeakReference(circuit, false);
                m_Delegate = del;
            }

            public void HttpRequestHandler(HttpRequest req)
            {
                LLAgent agent = m_Agent.Target as LLAgent;
                Circuit circuit = m_Circuit.Target as Circuit;
                if (agent != null && circuit != null)
                {
                    m_Delegate(agent, circuit, req);
                }
            }
        }

        private List<UploadAssetAbstractCapability> m_UploadCapabilities = new List<UploadAssetAbstractCapability>();

        public void SetupDefaultCapabilities(
            UUID regionSeedID, 
            Dictionary<string, string> capConfig,
            Dictionary<string, string> serviceURLs)
        {
            /* grid may override the caps through Cap_ ServiceURLs */
            foreach(KeyValuePair<string, string> kvp in serviceURLs)
            {
                if(kvp.Key.StartsWith("Cap_"))
                {
                    m_ServiceURLCapabilities.Add(kvp.Key.Substring(4), kvp.Value);
                }
            }

            try
            {
                /* The LSLCompiler is the only one that has this method */
                /* there has to be LSLSyntaxId which contains the hash of the file as UUID */
                IScriptCompiler compiler = CompilerRegistry.ScriptCompilers["lsl"];
                MethodInfo mi = compiler.GetType().GetMethod("GetLSLSyntaxId", new Type[0]);
                if(compiler.GetType().GetMethod("WriteLSLSyntaxFile", new Type[] { typeof(Stream)}) != null && mi != null)
                {
                    AddDefCapability("LSLSyntax", regionSeedID, Cap_LSLSyntax, capConfig);
                    m_ServiceURLCapabilities["LSLSyntaxId"] = (string)mi.Invoke(compiler, new object[0]);
                }
            }
            catch
            {

            }

            AddDefCapability("EnvironmentSettings", regionSeedID, Cap_EnvironmentSettings, capConfig);
            AddDefCapability("RenderMaterials", regionSeedID, Cap_RenderMaterials, capConfig);
            AddDefCapability("SimConsoleAsync", regionSeedID, Cap_SimConsoleAsync, capConfig);
            AddDefCapability("FetchInventory2", regionSeedID, Cap_FetchInventory2, capConfig);
            AddDefCapability("FetchLib2", regionSeedID, Cap_FetchInventory2, capConfig);
            AddDefCapability("FetchInventoryDescendents2", regionSeedID, Cap_FetchInventoryDescendents2, capConfig);
            AddDefCapability("FetchLibDescendents2", regionSeedID, Cap_FetchInventoryDescendents2, capConfig);
            AddDefCapability("GetTexture", regionSeedID, Cap_GetTexture, capConfig);
            AddDefCapability("GetMesh", regionSeedID, Cap_GetMesh, capConfig);
            AddDefCapability("GetMesh2", regionSeedID, Cap_GetMesh, capConfig);
            AddDefCapability("CreateInventoryCategory", regionSeedID, Cap_CreateInventoryCategory, capConfig);
            AddDefCapability("GetDisplayNames", regionSeedID, Cap_GetDisplayNames, capConfig);
            AddDefCapability("MeshUploadFlag", regionSeedID, Cap_MeshUploadFlag, capConfig);
            string localHostName = string.Format("{0}://{1}:{2}", m_CapsRedirector.Scheme, m_CapsRedirector.ExternalHostName, m_CapsRedirector.Port);
            AddDefCapabilityFactory("ObjectAdd", regionSeedID, delegate(LLAgent agent)
            {
                return new Capabilities.ObjectAdd(Scene, agent.Owner);
            }, capConfig);
            AddDefCapabilityFactory("UploadBakedTexture", regionSeedID, delegate(LLAgent agent) 
            {
                UploadAssetAbstractCapability capability = new Capabilities.UploadBakedTexture(agent.Owner, m_Server.Scene.AssetService, localHostName);
                m_UploadCapabilities.Add(capability);
                return capability;
            }, capConfig);
            AddDefCapabilityFactory("NewFileAgentInventory", regionSeedID, delegate(LLAgent agent) 
            {
                UploadAssetAbstractCapability capability = new Capabilities.NewFileAgentInventory(agent.Owner, agent.InventoryService, agent.AssetService, localHostName);
                m_UploadCapabilities.Add(capability);
                return capability;
            }, capConfig);
            AddDefCapabilityFactory("NewFileAgentInventoryVariablePrice", regionSeedID, delegate(LLAgent agent) 
            {
                UploadAssetAbstractCapability capability = new Capabilities.NewFileAgentInventoryVariablePrice(agent.Owner, agent.InventoryService, agent.AssetService, localHostName);
                m_UploadCapabilities.Add(capability);
                return capability;
            }, capConfig);
            AddDefCapabilityFactory("UpdateGestureAgentInventory", regionSeedID, delegate(LLAgent agent) 
            {
                UploadAssetAbstractCapability capability = new Capabilities.UpdateGestureAgentInventory(agent, agent.InventoryService, agent.AssetService, localHostName);
                m_UploadCapabilities.Add(capability);
                return capability;
            }, capConfig);
            AddDefCapabilityFactory("UpdateNotecardAgentInventory", regionSeedID, delegate(LLAgent agent) 
            {
                UploadAssetAbstractCapability capability = new Capabilities.UpdateNotecardAgentInventory(agent, agent.InventoryService, agent.AssetService, localHostName);
                m_UploadCapabilities.Add(capability);
                return capability;
            }, capConfig);
            AddDefCapabilityFactory("UpdateScriptAgent", regionSeedID, delegate(LLAgent agent) 
            {
                UploadAssetAbstractCapability capability = new Capabilities.UpdateScriptAgent(agent, agent.InventoryService, agent.AssetService, localHostName);
                m_UploadCapabilities.Add(capability);
                return capability;
            }, capConfig);
            AddDefCapabilityFactory("UpdateScriptTask", regionSeedID, delegate(LLAgent agent) 
            {
                UploadAssetAbstractCapability capability = new Capabilities.UpdateScriptTask(agent, Scene, localHostName);
                m_UploadCapabilities.Add(capability);
                return capability;
            }, capConfig);
            AddDefCapabilityFactory("UpdateGestureTaskInventory", regionSeedID, delegate(LLAgent agent) 
            {
                UploadAssetAbstractCapability capability = new Capabilities.UpdateGestureTaskInventory(agent, Scene, localHostName);
                m_UploadCapabilities.Add(capability);
                return capability;
            }, capConfig);
            AddDefCapabilityFactory("UpdateNotecardTaskInventory", regionSeedID, delegate(LLAgent agent) 
            {
                UploadAssetAbstractCapability capability = new Capabilities.UpdateNotecardTaskInventory(agent, Scene, localHostName);
                m_UploadCapabilities.Add(capability);
                return capability;
            }, capConfig);
            //AddDefCapabilityFactory("ParcelNavigateMedia", regionSeedID, delegate(LLAgent agent) { return new Capabilities.ParcelNavigateMedia(agent.Owner, Scene); }, capConfig);
            //AddDefCapabilityFactory("ObjectMediaNavigate", regionSeedID, delegate(LLAgent agent) { return new Capabilities.ObjectMediaNavigate(agent.Owner, Scene); }, capConfig);
        }
        #endregion
    }
}

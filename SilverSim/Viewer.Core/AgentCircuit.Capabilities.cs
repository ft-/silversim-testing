// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Viewer.Core.Capabilities;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Common;
using SilverSim.Types.StructuredData.Llsd;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Xml;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
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

        readonly Dictionary<string, string> m_ServiceURLCapabilities = new Dictionary<string, string>();


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
                if (0 == uri.Length)
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
                                    uri += "$";
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

                                default:
                                    break;
                            }
                        }
                        else if (c == '$')
                        {
                            l = '$';
                        }
                        else
                        {
                            uri += c.ToString();
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
                o = LlsdXml.Deserialize(httpreq.Body);
            }
            catch (Exception e)
            {
                m_Log.WarnFormat("Invalid LLSD_XML: {0} {1}", e.Message, e.StackTrace);
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML");
                return;
            }
            AnArray oarray = o as AnArray;
            if (null == oarray)
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML");
                return;
            }

            Dictionary<string, string> capsUri = new Dictionary<string, string>();

            foreach (IValue v in oarray)
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

            using (HttpResponse res = httpreq.BeginResponse())
            {
                res.ContentType = "application/llsd+xml";
                using (Stream tw = res.GetOutputStream())
                {
                    using (XmlTextWriter text = new XmlTextWriter(tw, UTF8NoBOM))
                    {
                        text.WriteStartElement("llsd");
                        text.WriteStartElement("map");
                        foreach (KeyValuePair<string, string> kvp in capsUri)
                        {
                            text.WriteKeyValuePair(kvp.Key, kvp.Value);
                        }
                        text.WriteEndElement();
                        text.WriteEndElement();
                        text.Flush();
                    }
                }
            }
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

        public void AddDefCapabilityFactory(string capabilityType, UUID seedID, Func<ViewerAgent, ICapabilityInterface> del, Dictionary<string, string> capConfig)
        {
            if(IsLocalHost(capConfig, capabilityType))
            {
                AddCapability(capabilityType, seedID, del(Agent).HttpRequestHandler);
            }
        }

        public void AddExtenderCapability(string capabilityType, UUID seedID, Action<ViewerAgent, AgentCircuit, HttpRequest> del, Dictionary<string, string> capConfig)
        {
            if (IsLocalHost(capConfig, capabilityType))
            {
                AddCapability(capabilityType, seedID, new ExtenderCapabilityCaller(Agent, this, del).HttpRequestHandler);
            }
        }

        sealed class ExtenderCapabilityCaller
        {
            /* Weak reference kills the hard referencing */
            readonly WeakReference m_Agent;
            readonly WeakReference m_Circuit;
            readonly Action<ViewerAgent, AgentCircuit, HttpRequest> m_Delegate;

            public ExtenderCapabilityCaller(ViewerAgent agent, AgentCircuit circuit, Action<ViewerAgent, AgentCircuit, HttpRequest> del)
            {
                m_Agent = new WeakReference(agent, false);
                m_Circuit = new WeakReference(circuit, false);
                m_Delegate = del;
            }

            public void HttpRequestHandler(HttpRequest req)
            {
                ViewerAgent agent = m_Agent.Target as ViewerAgent;
                AgentCircuit circuit = m_Circuit.Target as AgentCircuit;
                if (agent != null && circuit != null)
                {
                    m_Delegate(agent, circuit, req);
                }
            }
        }

        readonly List<UploadAssetAbstractCapability> m_UploadCapabilities = new List<UploadAssetAbstractCapability>();

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
                MethodInfo mi = compiler.GetType().GetMethod("GetLSLSyntaxId", Type.EmptyTypes);
                if(compiler.GetType().GetMethod("WriteLSLSyntaxFile", new Type[] { typeof(Stream)}) != null && mi != null)
                {
                    AddDefCapability("LSLSyntax", regionSeedID, Cap_LSLSyntax, capConfig);
                    m_ServiceURLCapabilities["LSLSyntaxId"] = (string)mi.Invoke(compiler, new object[0]);
                }
            }
            catch
            {

            }

            AddDefCapability("UpdateAgentLanguage", regionSeedID, Cap_UpdateAgentLanguage, capConfig);
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
            AddDefCapabilityFactory("ObjectAdd", regionSeedID, delegate(ViewerAgent agent)
            {
                return new Capabilities.ObjectAdd(Scene, agent.Owner);
            }, capConfig);
            AddDefCapabilityFactory("UploadBakedTexture", regionSeedID, delegate(ViewerAgent agent) 
            {
                UploadAssetAbstractCapability capability = new Capabilities.UploadBakedTexture(agent.Owner, Server.Scene.AssetService, localHostName);
                m_UploadCapabilities.Add(capability);
                return capability;
            }, capConfig);
            AddDefCapabilityFactory("NewFileAgentInventory", regionSeedID, delegate(ViewerAgent agent) 
            {
                UploadAssetAbstractCapability capability = new Capabilities.NewFileAgentInventory(agent.Owner, agent.InventoryService, agent.AssetService, localHostName);
                m_UploadCapabilities.Add(capability);
                return capability;
            }, capConfig);
            AddDefCapabilityFactory("NewFileAgentInventoryVariablePrice", regionSeedID, delegate(ViewerAgent agent) 
            {
                UploadAssetAbstractCapability capability = new Capabilities.NewFileAgentInventoryVariablePrice(agent.Owner, agent.InventoryService, agent.AssetService, localHostName);
                m_UploadCapabilities.Add(capability);
                return capability;
            }, capConfig);
            AddDefCapabilityFactory("UpdateGestureAgentInventory", regionSeedID, delegate(ViewerAgent agent) 
            {
                UploadAssetAbstractCapability capability = new Capabilities.UpdateGestureAgentInventory(agent, agent.InventoryService, agent.AssetService, localHostName);
                m_UploadCapabilities.Add(capability);
                return capability;
            }, capConfig);
            AddDefCapabilityFactory("UpdateNotecardAgentInventory", regionSeedID, delegate(ViewerAgent agent) 
            {
                UploadAssetAbstractCapability capability = new Capabilities.UpdateNotecardAgentInventory(agent, agent.InventoryService, agent.AssetService, localHostName);
                m_UploadCapabilities.Add(capability);
                return capability;
            }, capConfig);
            AddDefCapabilityFactory("UpdateScriptAgent", regionSeedID, delegate(ViewerAgent agent) 
            {
                UploadAssetAbstractCapability capability = new Capabilities.UpdateScriptAgent(agent, agent.InventoryService, agent.AssetService, localHostName);
                m_UploadCapabilities.Add(capability);
                return capability;
            }, capConfig);
            AddDefCapabilityFactory("UpdateScriptTask", regionSeedID, delegate(ViewerAgent agent) 
            {
                UploadAssetAbstractCapability capability = new Capabilities.UpdateScriptTask(agent, Scene, localHostName);
                m_UploadCapabilities.Add(capability);
                return capability;
            }, capConfig);
            AddDefCapabilityFactory("UpdateGestureTaskInventory", regionSeedID, delegate(ViewerAgent agent) 
            {
                UploadAssetAbstractCapability capability = new Capabilities.UpdateGestureTaskInventory(agent, Scene, localHostName);
                m_UploadCapabilities.Add(capability);
                return capability;
            }, capConfig);
            AddDefCapabilityFactory("UpdateNotecardTaskInventory", regionSeedID, delegate(ViewerAgent agent) 
            {
                UploadAssetAbstractCapability capability = new Capabilities.UpdateNotecardTaskInventory(agent, Scene, localHostName);
                m_UploadCapabilities.Add(capability);
                return capability;
            }, capConfig);
            AddDefCapabilityFactory("ParcelNavigateMedia", regionSeedID, delegate(ViewerAgent agent) { return new Capabilities.ParcelNavigateMedia(agent.Owner, Scene); }, capConfig);
            AddDefCapabilityFactory("ObjectMedia", regionSeedID, delegate(ViewerAgent agent) { return new Capabilities.ObjectMedia(agent.Owner, Scene); }, capConfig);
            AddDefCapabilityFactory("ObjectMediaNavigate", regionSeedID, delegate(ViewerAgent agent) { return new Capabilities.ObjectMediaNavigate(agent.Owner, Scene); }, capConfig);
        }
        #endregion
    }
}

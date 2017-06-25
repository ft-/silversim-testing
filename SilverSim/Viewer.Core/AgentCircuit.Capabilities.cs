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

#pragma warning disable IDE0018

using SilverSim.Main.Common.HttpServer;
using SilverSim.Scripting.Common;
using SilverSim.Types;
using SilverSim.Types.StructuredData.Llsd;
using SilverSim.Viewer.Core.Capabilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
    {
        private ChatSessionRequest ChatSessionRequestCapability { get; set; }

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

        private readonly Dictionary<string, string> m_ServiceURLCapabilities = new Dictionary<string, string>();

        private bool GetCustomCapsUri(string capType, out string uri)
        {
            string capsUriStr;
            if(capType == "EventQueueGet")
            {
                uri = string.Empty;
            }
            else if (m_ServiceURLCapabilities.TryGetValue(capType, out uri))
            {
                return true;
            }
            else if (Scene.CapabilitiesConfig.TryGetValue(capType, out capsUriStr) && capsUriStr != "localhost")
            {
                if (0 == capsUriStr.Length)
                {
                    return true;
                }
                else
                {
                    var buildUri = new StringBuilder();
                    var l = (char)0;
                    foreach(char c in capsUriStr)
                    {
                        if(l == '$')
                        {
                            l = (char)0;
                            switch(c)
                            {
                                case '$':
                                    buildUri.Append("$");
                                    break;

                                case 'h':
                                    buildUri.Append(Uri.EscapeUriString(Agent.HomeURI.ToString()));
                                    break;

                                case 'i':
                                    buildUri.Append(Uri.EscapeUriString(Agent.ServiceURLs["InventoryServerURI"]));
                                    break;

                                case 'a':
                                    buildUri.Append(Uri.EscapeUriString(Agent.ServiceURLs["AssetServerURI"]));
                                    break;

                                case 'r':
                                    buildUri.Append(Uri.EscapeUriString((string)Scene.ID));
                                    break;

                                case 's':
                                    buildUri.Append(Uri.EscapeUriString((string)SessionID));
                                    break;

                                case 'u':
                                    buildUri.Append(Uri.EscapeUriString((string)AgentID));
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
                            buildUri.Append(c);
                        }
                    }
                    uri = buildUri.ToString();
                    return true;
                }
            }
            return false;
        }

        public void RegionSeedHandler(HttpRequest httpreq)
        {
            IValue o;
            if (httpreq.CallerIP != RemoteIP)
            {
                httpreq.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }
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
            var oarray = o as AnArray;
            if (oarray == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML");
                return;
            }

            var capsUri = new Dictionary<string, string>();

            foreach (IValue v in oarray)
            {
                UUID capsID;
                string capsUriStr = string.Empty;
                if (v.ToString() == "SEED")
                {
                    /* SEED capability has no additional handling */
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

            using (var res = httpreq.BeginResponse())
            {
                res.ContentType = "application/llsd+xml";
                using (var tw = res.GetOutputStream())
                {
                    using (var text = tw.UTF8XmlTextWriter())
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

        private sealed class ExtenderCapabilityCaller
        {
            /* Weak reference kills the hard referencing */
            private readonly WeakReference m_Agent;
            private readonly WeakReference m_Circuit;
            private readonly Action<ViewerAgent, AgentCircuit, HttpRequest> m_Delegate;

            public ExtenderCapabilityCaller(ViewerAgent agent, AgentCircuit circuit, Action<ViewerAgent, AgentCircuit, HttpRequest> del)
            {
                m_Agent = new WeakReference(agent, false);
                m_Circuit = new WeakReference(circuit, false);
                m_Delegate = del;
            }

            public void HttpRequestHandler(HttpRequest req)
            {
                var agent = m_Agent.Target as ViewerAgent;
                var circuit = m_Circuit.Target as AgentCircuit;
                if (agent != null && circuit != null)
                {
                    m_Delegate(agent, circuit, req);
                }
            }
        }

        private readonly List<UploadAssetAbstractCapability> m_UploadCapabilities = new List<UploadAssetAbstractCapability>();

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
                    string capName = kvp.Key.Substring(4);
                    if (capName != "ChatSessionRequest")
                    {
                        m_ServiceURLCapabilities.Add(capName, kvp.Value);
                    }
                }
            }

            ChatSessionRequestCapability = new ChatSessionRequest(Agent, this, RemoteIP);
            try
            {
                /* The LSLCompiler is the only one that has this method */
                /* there has to be LSLSyntaxId which contains the hash of the file as UUID */
                var compiler = CompilerRegistry.ScriptCompilers["lsl"];
                var mi = compiler.GetType().GetMethod("GetLSLSyntaxId", Type.EmptyTypes);
                if(compiler.GetType().GetMethod("WriteLSLSyntaxFile", new Type[] { typeof(Stream)}) != null && mi != null)
                {
                    AddDefCapability("LSLSyntax", regionSeedID, Cap_LSLSyntax, capConfig);
                    m_ServiceURLCapabilities["LSLSyntaxId"] = (string)mi.Invoke(compiler, new object[0]);
                }
            }
            catch
            {
                /* no action needed here */
            }

            AddDefCapability("UpdateAgentLanguage", regionSeedID, Cap_UpdateAgentLanguage, capConfig);
            AddDefCapability("EnvironmentSettings", regionSeedID, Cap_EnvironmentSettings, capConfig);
            AddDefCapability("RenderMaterials", regionSeedID, Cap_RenderMaterials, capConfig);
            AddDefCapability("SimConsoleAsync", regionSeedID, Cap_SimConsoleAsyncCap, capConfig);
            AddDefCapability("FetchInventory2", regionSeedID, Cap_FetchInventory2, capConfig);
            AddDefCapability("FetchLib2", regionSeedID, Cap_FetchInventory2, capConfig);
            AddDefCapability("FetchInventoryDescendents2", regionSeedID, Cap_FetchInventoryDescendents2, capConfig);
            AddDefCapability("FetchLibDescendents2", regionSeedID, Cap_FetchInventoryDescendents2, capConfig);
            AddDefCapability("ViewerAsset", regionSeedID, Cap_ViewerAsset, capConfig);
            AddDefCapability("GetTexture", regionSeedID, Cap_GetTexture, capConfig);
            AddDefCapability("GetMesh", regionSeedID, Cap_GetMesh, capConfig);
            AddDefCapability("GetMesh2", regionSeedID, Cap_GetMesh, capConfig);
            AddDefCapability("CreateInventoryCategory", regionSeedID, Cap_CreateInventoryCategory, capConfig);
            AddDefCapability("GetDisplayNames", regionSeedID, Cap_GetDisplayNames, capConfig);
            AddDefCapability("MeshUploadFlag", regionSeedID, Cap_MeshUploadFlag, capConfig);
            AddDefCapability("GetPhysicsObjectData", regionSeedID, Cap_GetObjectsPhysicsData, capConfig);
            AddDefCapability("ChatSessionRequest", regionSeedID, ChatSessionRequestCapability.HttpRequestHandler, capConfig);
            string localHostName = string.Format("{0}://{1}:{2}", m_CapsRedirector.Scheme, m_CapsRedirector.ExternalHostName, m_CapsRedirector.Port);
            AddDefCapabilityFactory("DispatchRegionInfo", regionSeedID, (ViewerAgent agent) => new DispatchRegionInfo(agent, Server.Scene, RemoteIP), capConfig);
            AddDefCapabilityFactory("CopyInventoryFromNotecard", regionSeedID, (ViewerAgent agent) => new CopyInventoryFromNotecard(agent, Server.Scene, RemoteIP), capConfig);
            AddDefCapabilityFactory("ParcelPropertiesUpdate", regionSeedID, (ViewerAgent agent) => new ParcelPropertiesUpdate(agent, Server.Scene, RemoteIP), capConfig);
            AddDefCapabilityFactory("AgentPreferences", regionSeedID, (ViewerAgent agent) => new AgentPreferences(agent, RemoteIP), capConfig);
            AddDefCapabilityFactory("ObjectAdd", regionSeedID, (ViewerAgent agent) => new ObjectAdd(Server.Scene, agent.Owner, RemoteIP), capConfig);
            AddDefCapabilityFactory("UploadBakedTexture", regionSeedID, (ViewerAgent agent) =>
            {
                UploadAssetAbstractCapability capability = new UploadBakedTexture(
                    agent.Owner,
                    Server.Scene.AssetService,
                    localHostName,
                    RemoteIP);
                m_UploadCapabilities.Add(capability);
                return capability;
            }, capConfig);
            AddDefCapabilityFactory("NewFileAgentInventory", regionSeedID, (ViewerAgent agent) =>
            {
                UploadAssetAbstractCapability capability = new NewFileAgentInventory(
                    agent,
                    localHostName,
                    RemoteIP);
                m_UploadCapabilities.Add(capability);
                return capability;
            }, capConfig);
            AddDefCapabilityFactory("NewFileAgentInventoryVariablePrice", regionSeedID, (ViewerAgent agent) =>
            {
                UploadAssetAbstractCapability capability = new NewFileAgentInventoryVariablePrice(
                    agent,
                    localHostName,
                    RemoteIP);
                m_UploadCapabilities.Add(capability);
                return capability;
            }, capConfig);
            AddDefCapabilityFactory("UpdateGestureAgentInventory", regionSeedID, (ViewerAgent agent) =>
            {
                UploadAssetAbstractCapability capability = new UpdateGestureAgentInventory(
                    agent,
                    agent.InventoryService,
                    agent.AssetService,
                    localHostName,
                    RemoteIP);
                m_UploadCapabilities.Add(capability);
                return capability;
            }, capConfig);
            AddDefCapabilityFactory("UpdateNotecardAgentInventory", regionSeedID, (ViewerAgent agent) =>
            {
                UploadAssetAbstractCapability capability = new UpdateNotecardAgentInventory(
                    agent,
                    agent.InventoryService,
                    agent.AssetService,
                    localHostName,
                    RemoteIP);
                m_UploadCapabilities.Add(capability);
                return capability;
            }, capConfig);
            AddDefCapabilityFactory("UpdateScriptAgent", regionSeedID, (ViewerAgent agent) =>
            {
                UploadAssetAbstractCapability capability = new UpdateScriptAgent(
                    agent,
                    agent.InventoryService,
                    agent.AssetService,
                    localHostName,
                    RemoteIP);
                m_UploadCapabilities.Add(capability);
                return capability;
            }, capConfig);
            AddDefCapabilityFactory("UpdateScriptTask", regionSeedID, (ViewerAgent agent) =>
            {
                UploadAssetAbstractCapability capability = new UpdateScriptTask(
                    agent,
                    Server.Scene,
                    localHostName,
                    RemoteIP);
                m_UploadCapabilities.Add(capability);
                return capability;
            }, capConfig);
            AddDefCapabilityFactory("UpdateGestureTaskInventory", regionSeedID, (ViewerAgent agent) =>
            {
                UploadAssetAbstractCapability capability = new UpdateGestureTaskInventory(
                    agent,
                    Server.Scene,
                    localHostName,
                    RemoteIP);
                m_UploadCapabilities.Add(capability);
                return capability;
            }, capConfig);
            AddDefCapabilityFactory("UpdateNotecardTaskInventory", regionSeedID, (ViewerAgent agent) =>
            {
                UploadAssetAbstractCapability capability = new UpdateNotecardTaskInventory(
                    agent,
                    Server.Scene,
                    localHostName,
                    RemoteIP);
                m_UploadCapabilities.Add(capability);
                return capability;
            }, capConfig);
            AddDefCapabilityFactory("ParcelNavigateMedia", regionSeedID, (ViewerAgent agent) => new ParcelNavigateMedia(agent.Owner, Server.Scene, RemoteIP), capConfig);
            AddDefCapabilityFactory("ObjectMedia", regionSeedID, (ViewerAgent agent) => new ObjectMedia(agent.Owner, Server.Scene, RemoteIP), capConfig);
            AddDefCapabilityFactory("ObjectMediaNavigate", regionSeedID, (ViewerAgent agent) => new ObjectMediaNavigate(agent.Owner, Server.Scene, RemoteIP), capConfig);
            AddDefCapabilityFactory("UpdateAvatarAppearance", regionSeedID, (ViewerAgent agent) => new UpdateAvatarAppearance(agent, Server.Scene, RemoteIP), capConfig);
        }
        #endregion
    }
}

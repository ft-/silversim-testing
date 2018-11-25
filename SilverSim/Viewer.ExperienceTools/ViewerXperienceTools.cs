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

using log4net;
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Estate;
using SilverSim.ServiceInterfaces.Experience;
using SilverSim.Types;
using SilverSim.Types.Experience;
using SilverSim.Types.Grid;
using SilverSim.Types.StructuredData.Llsd;
using SilverSim.Types.StructuredData.REST;
using SilverSim.Viewer.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;

namespace SilverSim.Viewer.ExperienceTools
{
    [Description("Viewer Experience Tools Handler")]
    [PluginName("ViewerExperienceTools")]
    public sealed class ViewerXperienceTools : IPlugin, ICapabilityExtender
    {
        private static readonly ILog m_Log = LogManager.GetLogger("LL EXPERIENCE");

        public void Startup(ConfigurationLoader loader)
        {
        }

        [CapabilityHandler("GetMetadata")]
        [RequiresExperienceSupport]
        public void HandleGetMetadataCapability(ViewerAgent agent, AgentCircuit circuit, HttpRequest req)
        {
            if (req.CallerIP != circuit.RemoteIP)
            {
                req.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }
            if (req.Method != "POST")
            {
                req.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            var scene = circuit.Scene;
            if (scene == null)
            {
                req.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }
            IValue iv;
            UUID objectid;
            UUID itemid;
            try
            {
                iv = LlsdXml.Deserialize(req.Body);
            }
            catch (Exception e)
            {
                m_Log.WarnFormat("Invalid LLSD-XML received at GetMetadata", e);
                req.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                return;
            }
            if (!(iv is Map))
            {
                m_Log.WarnFormat("Invalid LLSD-XML received at GetMetadata");
                req.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                return;
            }

            Map m = iv as Map;
            objectid = m["object-id"].AsUUID;
            itemid = m["item-id"].AsUUID;

            Map res = new Map();
            ObjectPart part;
            ObjectPartInventoryItem item;
            if (scene.Primitives.TryGetValue(objectid, out part) &&
                part.Inventory.TryGetValue(itemid, out item))
            {
                UEI id = item.ExperienceID;
                if (id != UEI.Unknown)
                {
                    res.Add("experience", item.ExperienceID.ID);
                }
            }

            using (var httpres = req.BeginResponse("application/llsd+xml"))
            {
                LlsdXml.Serialize(res, httpres.GetOutputStream());
            }
        }

        private void GetExperiencesResponse(ViewerAgent agent, AgentCircuit circuit, HttpRequest req)
        {
            Dictionary<UEI, bool> result = circuit.Scene.ExperienceService.Permissions[agent.Owner];
            var resdata = new Map();
            var allowed = new AnArray();
            var blocked = new AnArray();
            resdata.Add("experiences", allowed);
            resdata.Add("blocked", blocked);

            foreach (KeyValuePair<UEI, bool> kvp in result)
            {
                if (kvp.Value)
                {
                    allowed.Add(kvp.Key.ID);
                }
                else
                {
                    blocked.Add(kvp.Key.ID);
                }
            }

            using (var res = req.BeginResponse("application/llsd+xml"))
            {
                using (Stream s = res.GetOutputStream())
                {
                    LlsdXml.Serialize(resdata, s);
                }
            }
        }

        /* GetExperiences - GET
         * <llsd>
         *   <map>
         *     <key>experiences</key>
         *     <array><uuid>xxx</uuid></array>
         *     <key>blocked</key>
         *     <array><uuid>xxx</uuid></array>
         *   </map>
         * </llsd>
         */
        [CapabilityHandler("GetExperiences")]
        [RequiresExperienceSupport]
        public void HandleGetExperiencesCapability(ViewerAgent agent, AgentCircuit circuit, HttpRequest httpreq)
        {
            if (httpreq.CallerIP != circuit.RemoteIP)
            {
                httpreq.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }
            if (httpreq.Method != "GET")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            GetExperiencesResponse(agent, circuit, httpreq);
        }

        /* AgentExperiences 
         * GET:
         * <llsd>
         *   <map>
         *     <key>experience_ids</key>
         *     <array><uuid>xxx</uuid></array>
         *   </map>
         * </llsd>
         * POST:
         * with empty <llsd> - response identical
         */
        [CapabilityHandler("AgentExperiences")]
        [RequiresExperienceSupport]
        public void HandleAgentExperiencesCapability(ViewerAgent agent, AgentCircuit circuit, HttpRequest httpreq)
        {
            if (httpreq.CallerIP != circuit.RemoteIP)
            {
                httpreq.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }

            if (httpreq.Method != "GET" && httpreq.Method != "POST")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            SceneInterface scene = circuit.Scene;
            if (scene == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            ExperienceServiceInterface experienceService = scene.ExperienceService;
            if (experienceService == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            if (httpreq.Method == "POST")
            {
                ExperienceInfo info = new ExperienceInfo
                {
                    /* ID is setup by ExperienceService */
                    Name = "New Experience",
                    Owner = agent.Owner,
                    Creator = agent.Owner,
                };
                experienceService.Add(info);
            }

            List<UEI> experienceids = experienceService.GetOwnerExperiences(agent.Owner);
            var ids = new AnArray();
            foreach (UEI id in experienceids)
            {
                ids.Add(id.ID);
            }
            var resdata = new Map
            {
                ["experience_ids"] = ids,
                ["purchase"] = new ABoolean(true)
            };
            using (HttpResponse res = httpreq.BeginResponse("application/llsd+xml"))
            {
                using (Stream o = res.GetOutputStream())
                {
                    LlsdXml.Serialize(resdata, o);
                }
            }
        }

        /* FindExperienceByName
         * GET ?page=" << mCurrentPage << "&page_size=30&query=" << LLURI::escape(text)
         * 
         * <llsd>
         *   <map>
         *     <key>experience_keys</key>
         *     <array>
         *       <map>
         *         ExperienceInfo
         *       </map>
         *     </array>
         *   </map>
         * </llsd>
         */
        [CapabilityHandler("FindExperienceByName")]
        [RequiresExperienceSupport]
        public void HandleFindExperienceByNameCapability(ViewerAgent agent, AgentCircuit circuit, HttpRequest httpreq)
        {
            if (httpreq.CallerIP != circuit.RemoteIP)
            {
                httpreq.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }
            if (httpreq.Method != "GET")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            Dictionary<string, object> reqdata = REST.ParseRESTFromRawUrl(httpreq.RawUrl);
            int currentpage;
            int pagesize;

            object o;
            if(!reqdata.TryGetValue("page", out o) || int.TryParse(o.ToString(), out currentpage))
            {
                currentpage = 1;
            }
            if (!reqdata.TryGetValue("page_size", out o) || int.TryParse(o.ToString(), out pagesize))
            {
                pagesize = 30;
            }
            if(!reqdata.TryGetValue("query", out o))
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Bad request");
                return;
            }

            string query = o.ToString();

            SceneInterface scene = circuit.Scene;
            if (scene == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            ExperienceServiceInterface experienceService = scene.ExperienceService;
            if (experienceService == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            List<ExperienceInfo> experienceinfos = experienceService.FindExperienceInfoByName(query);

            Map resdata = new Map();
            AnArray result = new AnArray();
            resdata.Add("experience_keys", result);
            foreach(ExperienceInfo info in experienceinfos)
            {
                result.Add(info.ToMap());
            }

            using (var res = httpreq.BeginResponse("application/llsd+xml"))
            {
                using (Stream s = res.GetOutputStream())
                {
                    LlsdXml.Serialize(resdata, s);
                }
            }
        }

        /* GetExperienceInfo 
         * GET url  id?public_id=<id>&public_id=<id>
         * 
         * <llsd>
         *   <map>
         *     <key>experience_keys</key>
         *     <array>
         *        <map>
         *          ExperienceInfo
         *        </map>
         *     </array>
         *     <key>error_ids</key>
         *     <array>
         *       <uuid>xxx</uuid>
         *     </array>
         *   </map>
         * </llsd>
         */
        [CapabilityHandler("GetExperienceInfo")]
        [RequiresExperienceSupport]
        public void HandleGetExperienceInfoCapability(ViewerAgent agent, AgentCircuit circuit, HttpRequest httpreq)
        {
            if (httpreq.CallerIP != circuit.RemoteIP)
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
            if (parts.Length < 2)
            {
                m_Log.WarnFormat("Invalid GetExperienceInfo request: {0}", httpreq.RawUrl);
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                return;
            }

            var reqs = parts[1].Split('&');
            var uuids = new List<UUID>();

            Map resdata = new Map();
            AnArray infos = new AnArray();
            var baduuids = new AnArray();
            resdata.Add("error_ids", baduuids);
            resdata.Add("experience_keys", infos);

            foreach (var req in reqs)
            {
                var p = req.Split('=');
                if (p.Length == 2)
                {
                    if (p[0] == "public_id")
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
                }
            }

            SceneInterface scene = circuit.Scene;
            if (scene == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            ExperienceServiceInterface experienceService = scene.ExperienceService;
            if(experienceService == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            foreach (UUID id in uuids)
            {
                ExperienceInfo info;
                try
                {
                    info = experienceService[id];
                }
                catch
                {
                    baduuids.Add(id);
                    continue;
                }
                infos.Add(info.ToMap());
            }

            using (var res = httpreq.BeginResponse("application/llsd+xml"))
            {
                using (Stream s = res.GetOutputStream())
                {
                    LlsdXml.Serialize(resdata, s);
                }
            }
        }

        /* GetAdminExperiences 
         * GET:
         * <llsd>
         *   <map>
         *     <key>experience_ids</key>
         *     <array><uuid>xxx</uuid></array>
         *   </map>
         * </llsd>
         */

        [CapabilityHandler("GetAdminExperiences")]
        [RequiresExperienceSupport]
        public void HandleGetAdminExperiencesCapability(ViewerAgent agent, AgentCircuit circuit, HttpRequest httpreq)
        {
            if (httpreq.CallerIP != circuit.RemoteIP)
            {
                httpreq.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }
            if (httpreq.Method != "GET")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            SceneInterface scene = circuit.Scene;
            if (scene == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            ExperienceServiceInterface experienceService = scene.ExperienceService;
            if (experienceService == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            List<UEI> experienceids = experienceService.Admins[agent.Owner];
            var ids = new AnArray();
            foreach (UEI id in experienceids)
            {
                ids.Add(id.ID);
            }
            var resdata = new Map
            {
                ["experience_ids"] = ids
            };
            using (HttpResponse res = httpreq.BeginResponse("application/llsd+xml"))
            {
                using (Stream o = res.GetOutputStream())
                {
                    LlsdXml.Serialize(resdata, o);
                }
            }
        }

        /* GetCreatorExperiences 
         * GET:
         * <llsd>
         *   <map>
         *     <key>experience_ids</key>
         *     <array><uuid>xxx</uuid></array>
         *   </map>
         * </llsd>
         */
        [CapabilityHandler("GetCreatorExperiences")]
        [RequiresExperienceSupport]
        public void HandleGetCreatorExperiencesCapability(ViewerAgent agent, AgentCircuit circuit, HttpRequest httpreq)
        {
            if (httpreq.CallerIP != circuit.RemoteIP)
            {
                httpreq.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }
            if (httpreq.Method != "GET")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            SceneInterface scene = circuit.Scene;
            if (scene == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            ExperienceServiceInterface experienceService = scene.ExperienceService;
            if (experienceService == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            List<UEI> experienceids = experienceService.GetCreatorExperiences(agent.Owner);
            var ids = new AnArray();
            foreach (UEI id in experienceids)
            {
                ids.Add(id.ID);
            }
            var resdata = new Map
            {
                ["experience_ids"] = ids
            };
            using (HttpResponse res = httpreq.BeginResponse("application/llsd+xml"))
            {
                using (Stream o = res.GetOutputStream())
                {
                    LlsdXml.Serialize(resdata, o);
                }
            }
        }

        /* ExperiencePreferences 
         * PUT:
         * <llsd>
         *   <map>
         *     <key>__uuid__</key>
         *     <map>
         *       <key>permission</key>
         *       <string>Allow</string>
         *     </map>
         *   </map>
         * </llsd>
         * 
         * <llsd>
         *   <map>
         *     <key>__uuid__</key>
         *     <map>
         *       <key>permission</key>
         *       <string>Block</string>
         *     </map>
         *   </map>
         * </llsd>
         * 
         * response:
         * <llsd>
         *   <map>
         *     <key>experiences</key>
         *     <array><uuid>xxx</uuid></array>
         *     <key>blocked</key>
         *     <array><uuid>xxx</uuid></array>
         *   </map>
         * </llsd>
         * 
         * DELETE ?<experience_id>
         * 
         * response:
         * <llsd>
         *   <map>
         *     <key>experiences</key>
         *     <array><uuid>xxx</uuid></array>
         *     <key>blocked</key>
         *     <array><uuid>xxx</uuid></array>
         *   </map>
         * </llsd>
         * 
         */

        [CapabilityHandler("ExperiencePreferences")]
        [RequiresExperienceSupport]
        public void HandleExperiencePerferencesCapability(ViewerAgent agent, AgentCircuit circuit, HttpRequest httpreq)
        {
            if (httpreq.CallerIP != circuit.RemoteIP)
            {
                httpreq.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }

            switch(httpreq.Method)
            {
                case "GET":
                    break;

                case "PUT":
                    HandleExperiencePreferencesPut(agent, circuit, httpreq);
                    break;

                case "DELETE":
                    HandleExperiencePreferencesDelete(agent, circuit, httpreq);
                    break;

                default:
                    httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                    return;
            }

            GetExperiencesResponse(agent, circuit, httpreq);
        }

        private void HandleExperiencePreferencesPut(ViewerAgent agent, AgentCircuit circuit, HttpRequest httpreq)
        {
            Map reqdata;

            using (Stream input = httpreq.Body)
            {
                reqdata = LlsdXml.Deserialize(input) as Map;
            }

            if(reqdata == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Bad request");
                return;
            }

            SceneInterface scene = circuit.Scene;
            if (scene == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            ExperienceServiceInterface experienceService = scene.ExperienceService;
            if (experienceService == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            foreach(KeyValuePair<string, IValue> kvp in reqdata)
            {
                Map entry = kvp.Value as Map;
                IValue iv;
                UUID experienceid;
                UEI uei;
                if(!UUID.TryParse(kvp.Key, out experienceid) || entry == null || !entry.TryGetValue("permission", out iv) || !experienceService.TryGetValue(experienceid, out uei))
                {
                    continue;
                }

                switch(iv.ToString())
                {
                    case "Allow":
                        experienceService.Permissions[uei, agent.Owner] = true;
                        break;

                    case "Block":
                        experienceService.Permissions[uei, agent.Owner] = false;
                        break;
                }
            }
        }

        private void HandleExperiencePreferencesDelete(ViewerAgent agent, AgentCircuit circuit, HttpRequest httpreq)
        {
            string[] parts = httpreq.RawUrl.Split('?');
            UUID id;
            if(parts.Length < 2 || !UUID.TryParse(parts[1], out id))
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Bad request");
                return;
            }

            SceneInterface scene = circuit.Scene;
            if (scene == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            ExperienceServiceInterface experienceService = scene.ExperienceService;
            if (experienceService == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            UEI uei;
            if(!experienceService.TryGetValue(id, out uei))
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            experienceService.Permissions.Remove(uei, agent.Owner);
        }

        /* GroupExperiences 
         * GET:
         * <llsd>
         *   <map>
         *     <key>experience_ids</key>
         *     <array><uuid>xxx</uuid></array>
         *   </map>
         * </llsd>
         */
        [CapabilityHandler("GroupExperiences")]
        [RequiresExperienceSupport]
        public void HandleGroupExperiencesCapability(ViewerAgent agent, AgentCircuit circuit, HttpRequest httpreq)
        {
            if (httpreq.CallerIP != circuit.RemoteIP)
            {
                httpreq.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }
            if (httpreq.Method != "GET")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            SceneInterface scene = circuit.Scene;
            if (scene == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            ExperienceServiceInterface experienceService = scene.ExperienceService;
            if (experienceService == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            List<UEI> experienceids = experienceService.GetGroupExperiences(agent.Group);
            var ids = new AnArray();
            foreach (UEI id in experienceids)
            {
                ids.Add(id.ID);
            }
            var resdata = new Map
            {
                ["experience_ids"] = ids
            };
            using (HttpResponse res = httpreq.BeginResponse("application/llsd+xml"))
            {
                using (Stream o = res.GetOutputStream())
                {
                    LlsdXml.Serialize(resdata, o);
                }
            }
        }

        /* UpdateExperience
         * POST
         * <llsd>
         *   <map>
         *     <key>public_id</key><uuid></uuid>
         *     <key>group_id</key><uuid></uuid>
         *     <key>name</key><string></string>
         *     <key>properties</key><xx/>
         *     <key>description</key><string></string>
         *     <key>maturity</key><integer></integer>
         *     <key>extended_metadata</key><xx/>
         *     <key>slurl</key><url></url>
         *   </map>
         * </llsd>
         * 
         * response on error:
         * <llsd>
         *   <map>
         *     <key>removed</key>
         *     <array>
         *       <map>
         *         <key>error-tag</key><string></string>
         *       </map>
         *     </array>
         *   </map>
         * </llsd>
         * 
         * response on success:
         * <llsd>
         *   <map>
         *     <key>experience_keys</key>
         *     <array>
         *          ExperienceInfo
         *     </array>
         *   </map>
         * </llsd>
         */
        [CapabilityHandler("UpdateExperience")]
        [RequiresExperienceSupport]
        public void HandleUpdateExperienceCapability(ViewerAgent agent, AgentCircuit circuit, HttpRequest httpreq)
        {
            if (httpreq.CallerIP != circuit.RemoteIP)
            {
                httpreq.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }
            if (httpreq.Method != "POST")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            Map reqmap;

            using (Stream s = httpreq.Body)
            {
                reqmap = LlsdXml.Deserialize(s) as Map;
            }

            if (reqmap == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Bad request");
                return;
            }

            SceneInterface scene = circuit.Scene;
            if (scene == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            ExperienceServiceInterface experienceService = scene.ExperienceService;
            if (experienceService == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            UUID experienceid = reqmap["public_id"].AsUUID;

            ExperienceInfo info;
            Map resmap = new Map();
            if (experienceService.TryGetValue(experienceid, out info))
            {
                IValue iv;
                if(reqmap.TryGetValue("group_id", out iv))
                {
                    UUID id = iv.AsUUID;
                    UGI ugi;
                    if (id == UUID.Zero)
                    {
                        info.Group = UGI.Unknown;
                    }
                    else if(scene.GroupsNameService != null && scene.GroupsNameService.TryGetValue(id, out ugi))
                    {
                        info.Group = ugi;
                    }
                }

                if(reqmap.TryGetValue("name", out iv))
                {
                    info.Name = iv.ToString();
                }

                if(reqmap.TryGetValue("description", out iv))
                {
                    info.Description = iv.ToString();
                }

                if(reqmap.TryGetValue("properties", out iv))
                {
                    info.Properties = (ExperiencePropertyFlags)iv.AsInt;
                }

                if(reqmap.TryGetValue("maturity", out iv))
                {
                    info.Maturity = (RegionAccess)iv.AsInt;
                }

                if(reqmap.TryGetValue("extended_metadata", out iv))
                {
                    info.ExtendedMetadata = iv.ToString();
                }

                if(reqmap.TryGetValue("slurl", out iv))
                {
                    info.SlUrl = iv.ToString();
                }

                try
                {
                    experienceService.Update(agent.Owner, info);
                }
                catch(Exception e)
                {
#if DEBUG
                    m_Log.Debug("UpdateExperience capability", e);
#endif
                    resmap.Add("removed", new AnArray
                    {
                        new Map
                        {
                            { "error-tag", "Failed to update" }
                        }
                    });
                }

                if(!resmap.ContainsKey("removed"))
                {
                    var ids = new AnArray();
                    ids.Add(info.ToMap());
                    resmap.Add("experience_keys", ids);
                }
            }
            else
            {
                resmap.Add("removed", new AnArray
                {
                    new Map
                    {
                        { "error-tag", "Not found" }
                    }
                });
            }

            using (HttpResponse res = httpreq.BeginResponse("application/llsd+xml"))
            {
                using (Stream o = res.GetOutputStream())
                {
                    LlsdXml.Serialize(resmap, o);
                }
            }
        }

        /* IsExperienceAdmin 
         * GET ?experience_id=<experienceid>
         * 
         * <llsd>
         *   <map>
         *     <key>status</key>
         *     <boolean></boolean>
         *   </map>
         * </llsd>
         */
        [CapabilityHandler("IsExperienceAdmin")]
        [RequiresExperienceSupport]
        public void HandleIsExperienceAdminCapability(ViewerAgent agent, AgentCircuit circuit, HttpRequest httpreq)
        {
            if (httpreq.CallerIP != circuit.RemoteIP)
            {
                httpreq.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }
            if (httpreq.Method != "GET")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            Dictionary<string, object> reqdata = REST.ParseRESTFromRawUrl(httpreq.RawUrl);
            UUID experienceid = UUID.Parse((string)reqdata["experience_id"]);

            SceneInterface scene = circuit.Scene;
            if (scene == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            ExperienceServiceInterface experienceService = scene.ExperienceService;
            if (experienceService == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            ExperienceInfo info;
            bool isadmin = false;
            if(experienceService.TryGetValue(experienceid, out info))
            {
                isadmin = info.Owner.EqualsGrid(agent.Owner) || experienceService.Admins[info.ID, agent.Owner];
            }

            Map resdata = new Map
            {
                { "status", isadmin }
            };
            using (HttpResponse res = httpreq.BeginResponse("application/llsd+xml"))
            {
                using (Stream o = res.GetOutputStream())
                {
                    LlsdXml.Serialize(resdata, o);
                }
            }
        }

        /* IsExperienceContributor */
        [CapabilityHandler("IsExperienceContributor")]
        [RequiresExperienceSupport]
        public void HandleIsExperienceContributorCapability(ViewerAgent agent, AgentCircuit circuit, HttpRequest httpreq)
        {
            if (httpreq.CallerIP != circuit.RemoteIP)
            {
                httpreq.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }
            if (httpreq.Method != "GET")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            Dictionary<string, object> reqdata = REST.ParseRESTFromRawUrl(httpreq.RawUrl);
            UUID experienceid = UUID.Parse((string)reqdata["experience_id"]);

            SceneInterface scene = circuit.Scene;
            if (scene == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            ExperienceServiceInterface experienceService = scene.ExperienceService;
            if (experienceService == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            ExperienceInfo info;
            bool iscontributor = false;
            if (experienceService.TryGetValue(experienceid, out info))
            {
                iscontributor = info.Owner.EqualsGrid(agent.Owner);
            }

            Map resdata = new Map();
            resdata.Add("status", iscontributor);
            using (HttpResponse res = httpreq.BeginResponse("application/llsd+xml"))
            {
                using (Stream o = res.GetOutputStream())
                {
                    LlsdXml.Serialize(resdata, o);
                }
            }
        }

        /* RegionExperiences
         * GET:
         * <llsd>
         *   <map>
         *     <key>allowed</key>
         *     <array><uuid>xxx</uuid></array>
         *     <key>blocked</key>
         *     <array><uuid>xxx</uuid></array>
         *     <key>trusted</key>
         *     <array><uuid>xxx</uuid></array>
         *   </map>
         * </llsd>
         * 
         * POST:
         * <llsd>
         *   <map>
         *     <key>allowed</key>
         *     <array><uuid>xxx</uuid></array>
         *     <key>blocked</key>
         *     <array><uuid>xxx</uuid></array>
         *     <key>trusted</key>
         *     <array><uuid>xxx</uuid></array>
         *   </map>
         * </llsd>
         * 
         * response is identical to GET
         */
        [CapabilityHandler("RegionExperiences")]
        [RequiresExperienceSupport]
        public void HandleRegionExperiencesCapability(ViewerAgent agent, AgentCircuit circuit, HttpRequest httpreq)
        {
            if (httpreq.CallerIP != circuit.RemoteIP)
            {
                httpreq.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }
            switch(httpreq.Method)
            {
                case "GET":
                    HandleRegionExperiencesGet(agent, circuit, httpreq);
                    break;

                case "POST":
                    HandleRegionExperiencesPost(agent, circuit, httpreq);
                    break;

                default:
                    httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                    return;
            }
        }

        private void HandleRegionExperiencesGet(ViewerAgent agent, AgentCircuit circuit, HttpRequest httpreq)
        {
            List<UUID> allowed = new List<UUID>();
            List<UUID> blocked = new List<UUID>();

            SceneInterface scene = circuit.Scene;
            if(scene == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.InternalServerError, "Internal server error");
                return;
            }
            EstateServiceInterface estateService = scene.EstateService;

            foreach(EstateExperienceInfo info in estateService.Experiences[scene.ParentEstateID])
            {
                if(info.IsAllowed)
                {
                    allowed.Add(info.ExperienceID);
                }
                else
                {
                    blocked.Add(info.ExperienceID);
                }
            }
            List<UUID> trusted = estateService.TrustedExperiences[scene.ParentEstateID];

            Map resdata = new Map();
            AnArray array = new AnArray();
            foreach(UUID id in allowed)
            {
                array.Add(id);
            }
            resdata.Add("allowed", array);
            array = new AnArray();
            foreach (UUID id in blocked)
            {
                array.Add(id);
            }
            resdata.Add("blocked", array);
            array = new AnArray();
            foreach (UUID id in trusted)
            {
                array.Add(id);
            }
            resdata.Add("trusted", array);

            using (HttpResponse res = httpreq.BeginResponse("application/llsd+xml"))
            {
                using (Stream o = res.GetOutputStream())
                {
                    LlsdXml.Serialize(resdata, o);
                }
            }
        }

        private void HandleRegionExperiencesPost(ViewerAgent agent, AgentCircuit circuit, HttpRequest httpreq)
        {
            Map reqmap;

            using (Stream s = httpreq.Body)
            {
                reqmap = LlsdXml.Deserialize(s) as Map;
            }

            if(reqmap == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Bad request");
                return;
            }

            /* process map */

            HandleRegionExperiencesGet(agent, circuit, httpreq);
        }
    }
}

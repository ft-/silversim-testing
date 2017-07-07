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
using SilverSim.Types;
using SilverSim.Types.StructuredData.Llsd;
using SilverSim.Viewer.Core;
using System;
using System.ComponentModel;
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
            if(scene.Primitives.TryGetValue(objectid, out part) &&
                part.Inventory.TryGetValue(itemid, out item))
            {
                UUID id = item.ExperienceID;
                if (id != UUID.Zero)
                {
                    res.Add("experience", item.ExperienceID);
                }
            }

            using (var httpres = req.BeginResponse("application/llsd+xml"))
            {
                LlsdXml.Serialize(res, httpres.GetOutputStream());
            }
        }

        /* GetExperiences */
        /* AgentExperiences */
        /* FindExperienceByName */
        /* GetExperienceInfo */
        /* GetAdminExperiences */
        /* GetCreatorExperiences */
        /* ExperiencePreferences */
        /* GroupExperiences */
        /* UpdateExperience */
        /* IsExperienceAdmin */
        /* IsExperienceContributor */
        /* RegionExperiences */
    }
}

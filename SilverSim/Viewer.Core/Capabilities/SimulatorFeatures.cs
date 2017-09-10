﻿// SilverSim is distributed under the terms of the
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
using SilverSim.Types.StructuredData.Llsd;
using System.IO;
using System.Net;

namespace SilverSim.Viewer.Core.Capabilities
{
    public class SimulatorFeatures : ICapabilityInterface
    {
        public readonly Map Features = new Map();

        public string CapabilityName => "SimulatorFeatures";

        public SimulatorFeatures(string searchUrl, string gridName, string gridURL, bool exportSupported)
        {
            Features.Add("MeshRezEnabled", true);
            Features.Add("MeshUploadEnabled", true);
            Features.Add("MeshXferEnabled", true);
            Features.Add("PhysicsMaterialsEnabled", true);
            Features.Add("RenderMaterialsCapability", 1.0);
            Features.Add("MaxMaterialsPerTransaction", 50);
            Features.Add("DynamicPathfindingEnabled", false);
            Features.Add("AvatarHoverHeightEnabled", true);
            var typesMap = new Map
            {
                { "convex", true },
                { "none", true },
                { "prim", true }
            };
            Features.Add("PhysicsShapeTypes", typesMap);
            var extrasMap = new Map();
            if (!string.IsNullOrEmpty(gridURL))
            {
                extrasMap.Add("GridURL", gridURL);
            }
            if (!string.IsNullOrEmpty(gridName))
            {
                extrasMap.Add("GridName", gridName);
            }

            extrasMap.Add("SimulatorFPS", 30.0);
            extrasMap.Add("SimulatorFPSWarnPercent", 66.6);
            extrasMap.Add("SimulatorFPSCritPercent", 33.3);
            extrasMap.Add("SimulatorFPSFactor", 1.0);

            if(!string.IsNullOrEmpty(searchUrl))
            {
                extrasMap.Add("search-server-url", searchUrl);
            }
            if(exportSupported)
            {
                extrasMap.Add("ExportSupported", true);
            }
            if (extrasMap.Count > 0)
            {
                Features.Add("OpenSimExtras", extrasMap);
            }

            /* known OpenSimExtras
             * GrickName:string
             * GridURL:string
             * ExportSupported:bool
             * map-server-url:string
             * say-range:integer
             * shout-range:integer
             * whisper-range:integer
             * SimulatorFPS:double
             * SimulatorFPSFactor:double
             * SimulatorFPSWarnPercent:double
             * SimulatorFPSCritPercent:double
             * search-server-url:string
             * destination-guide-url:string
             * avatar-picker-url:string
             * camera-only-mode:bool
             * special-ui:map(toolbar:string,floaters:map)
             * 
             * known root features:
             * god_names:map(last_names:array(string),full_names:array(string))
             * MeshRezEnabled:bool
             * MeshUploadEnabled:bool
             * MeshXferEnabled:true
             * PhysicsMaterialsEnabled:true
             * RenderMaterialsCapability:double
             * MaxMaterialsPerTransaction:integer
             * DynamicPathfindingEnabled:bool
             * AvatarHoverHeightEnabled:bool
             * PhysicsShapeTypes:map(convex:bool,none:bool,prim:bool)
             */
        }

        public void HttpRequestHandler(HttpRequest httpreq)
        {
            if(httpreq.Method != "GET")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
            }
            else
            {
                using (var res = httpreq.BeginResponse())
                {
                    res.ContentType = "application/llsd+xml";
                    using (var s = res.GetOutputStream())
                    {
                        LlsdXml.Serialize(Features, s);
                    }
                }
            }
        }
    }
}

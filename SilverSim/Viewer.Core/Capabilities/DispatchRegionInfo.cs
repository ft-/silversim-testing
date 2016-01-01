// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using SilverSim.Types.Grid;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace SilverSim.Viewer.Core.Capabilities
{
    public class DispatchRegionInfo : ICapabilityInterface
    {
        readonly ViewerAgent m_Agent;
        readonly SceneInterface m_Scene;

        public DispatchRegionInfo(ViewerAgent agent, SceneInterface scene)
        {
            m_Agent = agent;
            m_Scene = scene;
        }

        public string CapabilityName
        {
            get
            {
                return "DispatchRegionInfo";
            }
        }

        public void HttpRequestHandler(HttpRequest httpreq)
        {
            if (httpreq.Method != "POST")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            Map reqmap;
            try
            {
                reqmap = LlsdXml.Deserialize(httpreq.Body) as Map;
            }
            catch
            {
                httpreq.ErrorResponse(HttpStatusCode.UnsupportedMediaType, "Unsupported Media Type");
                return;
            }
            if (null == reqmap)
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML");
                return;
            }

            m_Scene.RegionSettings.BlockTerraform = reqmap["block_terraform"].AsBoolean;
            m_Scene.RegionSettings.BlockFly = reqmap["block_fly"].AsBoolean;
            //bool blockFlyOver = reqmap["block_fly_over"].AsBoolean;
            m_Scene.RegionSettings.AllowDamage = reqmap["allow_damage"].AsBoolean;
            m_Scene.RegionSettings.AllowLandResell = reqmap["allow_land_resell"].AsBoolean;
            m_Scene.RegionSettings.AgentLimit = (int)decimal.Parse(reqmap["agent_limit"].ToString());
            m_Scene.RegionSettings.ObjectBonus = reqmap["prim_bonus"].AsReal;
            m_Scene.RegionData.Access = (RegionAccess)reqmap["sim_access"].AsUInt;
            m_Scene.RegionSettings.RestrictPushing = reqmap["restrict_pushobject"].AsBoolean;
            m_Scene.RegionSettings.AllowLandResell = reqmap["allow_parcel_changes"].AsBoolean;
            m_Scene.RegionSettings.BlockShowInSearch = reqmap["block_parcel_search"].AsBoolean;
            m_Scene.TriggerRegionSettingsChanged();
            m_Scene.ReregisterRegion();

            using (HttpResponse res = httpreq.BeginResponse("text/plain"))
            {
                /* no further action required */
            }
        }
    }
}

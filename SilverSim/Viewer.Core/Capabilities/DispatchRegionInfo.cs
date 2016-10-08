// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using SilverSim.Types.Grid;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace SilverSim.Viewer.Core.Capabilities
{
    public class DispatchRegionInfo : ICapabilityInterface
    {
        readonly ViewerAgent m_Agent;
        readonly SceneInterface m_Scene;
        readonly string m_RemoteIP;

#if DEBUG
        private static readonly ILog m_Log = LogManager.GetLogger("DISPATCH REGION INFO");
#endif
        public DispatchRegionInfo(ViewerAgent agent, SceneInterface scene, string remoteip)
        {
            m_Agent = agent;
            m_Scene = scene;
            m_RemoteIP = remoteip;
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
            if (httpreq.CallerIP != m_RemoteIP)
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

            if (!m_Scene.IsEstateManager(m_Agent.Owner))
            {
                httpreq.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }

            if (!m_Agent.IsInScene(m_Scene))
            {
                return;
            }

            m_Scene.RegionSettings.BlockTerraform = reqmap["block_terraform"].AsBoolean;
            m_Scene.RegionSettings.BlockFly = reqmap["block_fly"].AsBoolean;
            m_Scene.RegionSettings.BlockFlyOver = reqmap["block_fly_over"].AsBoolean;
            m_Scene.RegionSettings.AllowDamage = reqmap["allow_damage"].AsBoolean;
            m_Scene.RegionSettings.AllowLandResell = reqmap["allow_land_resell"].AsBoolean;
            m_Scene.RegionSettings.AgentLimit = (int)decimal.Parse(reqmap["agent_limit"].ToString());
            m_Scene.RegionSettings.ObjectBonus = reqmap["prim_bonus"].AsReal;
            m_Scene.Access = (RegionAccess)reqmap["sim_access"].AsUInt;
            m_Scene.RegionSettings.RestrictPushing = reqmap["restrict_pushobject"].AsBoolean;
            m_Scene.RegionSettings.AllowLandJoinDivide = reqmap["allow_parcel_changes"].AsBoolean;
            m_Scene.RegionSettings.BlockShowInSearch = reqmap["block_parcel_search"].AsBoolean;
#if DEBUG
            m_Log.DebugFormat("RegionFlags={0} Access={1} AgentLimit={2} ObjectBonus={3}", 
                m_Scene.RegionSettings.AsFlags.ToString(), 
                m_Scene.Access,
                m_Scene.RegionSettings.AgentLimit,
                m_Scene.RegionSettings.ObjectBonus);
#endif

            m_Scene.TriggerRegionSettingsChanged();
            m_Scene.ReregisterRegion();

            using (HttpResponse httpres = httpreq.BeginResponse())
            {
                httpres.ContentType = "application/llsd+xml";
                using (Stream outStream = httpres.GetOutputStream())
                {
                    LlsdXml.Serialize(new Map(), outStream);
                }
            }
        }
    }
}

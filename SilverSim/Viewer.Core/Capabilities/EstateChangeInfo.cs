﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using SilverSim.Types.Estate;
using SilverSim.Types.StructuredData.Llsd;
using System.IO;
using System.Net;

namespace SilverSim.Viewer.Core.Capabilities
{
    public class EstateChangeInfo : ICapabilityInterface
    {
        readonly SceneInterface m_Scene;
        readonly ViewerAgent m_Agent;

        public string CapabilityName
        {
            get
            {
                return "EstateChangeInfo";
            }
        }

        public EstateChangeInfo(ViewerAgent agent, SceneInterface scene)
        {
            m_Agent = agent;
            m_Scene = scene;
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

            string estateName = reqmap["estate_name"].ToString();
            double sun_hour = reqmap["sun_hour"].AsReal;
            bool isSunFixed = reqmap["is_sun_fixed"].AsBoolean;
            bool isExternallyVisible = reqmap["is_externally_visible"].AsBoolean;
            bool allowDirectTeleport = reqmap["allow_direct_teleport"].AsBoolean;
            bool denyAnonymous = reqmap["deny_anonymous"].AsBoolean;
            bool denyAgeUnverified = reqmap["deny_age_unverified"].AsBoolean;
            bool allowVoiceChat = reqmap["allow_voice_chat"].AsBoolean;
            UUID invoiceID = reqmap["invoice"].AsUUID;

            uint estateID;
            EstateInfo estate;
            if(!m_Scene.IsEstateManager(m_Agent.Owner))
            {
                httpreq.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }

            if(m_Scene.EstateService.RegionMap.TryGetValue(m_Scene.ID, out estateID) &&
                m_Scene.EstateService.TryGetValue(estateID, out estate))
            {
                estate.Name = estateName;
                estate.SunPosition = sun_hour - 6;
                estate.UseGlobalTime = (sun_hour < 6);
                if (isSunFixed)
                {
                    estate.Flags |= RegionOptionFlags.SunFixed;
                }
                else
                {
                    estate.Flags &= ~RegionOptionFlags.SunFixed;
                }

                if(isExternallyVisible)
                {
                    estate.Flags |= RegionOptionFlags.ExternallyVisible;
                }
                else
                {
                    estate.Flags &= ~RegionOptionFlags.ExternallyVisible;
                }
                if(allowDirectTeleport)
                {
                    estate.Flags |= RegionOptionFlags.AllowDirectTeleport;
                }
                else
                {
                    estate.Flags &= ~RegionOptionFlags.AllowDirectTeleport;
                }
                if(denyAnonymous)
                {
                    estate.Flags |= RegionOptionFlags.DenyAnonymous;
                }
                else
                {
                    estate.Flags &= ~RegionOptionFlags.DenyAnonymous;
                }
                if(denyAgeUnverified)
                {
                    estate.Flags |= RegionOptionFlags.DenyAgeUnverified;
                }
                else
                {
                    estate.Flags &= ~RegionOptionFlags.DenyAgeUnverified;
                }
                if(allowVoiceChat)
                {
                    estate.Flags |= RegionOptionFlags.AllowVoice;
                }
                else
                {
                    estate.Flags &= ~RegionOptionFlags.AllowVoice;
                }
                m_Scene.EstateService[estate.ID] = estate;

                m_Agent.SendEstateUpdateInfo(invoiceID, UUID.Zero, estate, m_Scene.ID);
                m_Scene.TriggerEstateUpdate();
            }

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

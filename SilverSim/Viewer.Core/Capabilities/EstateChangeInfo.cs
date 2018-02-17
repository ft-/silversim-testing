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
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using SilverSim.Types.Estate;
using SilverSim.Types.StructuredData.Llsd;
using System.Net;

namespace SilverSim.Viewer.Core.Capabilities
{
    public class EstateChangeInfo : ICapabilityInterface
    {
        private readonly SceneInterface m_Scene;
        private readonly ViewerAgent m_Agent;
        private readonly string m_RemoteIP;

        public string CapabilityName => "EstateChangeInfo";

        public EstateChangeInfo(ViewerAgent agent, SceneInterface scene, string remoteip)
        {
            m_Agent = agent;
            m_Scene = scene;
            m_RemoteIP = remoteip;
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
            if (reqmap == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML");
                return;
            }

            var estateName = reqmap["estate_name"].ToString();
            double sun_hour = reqmap["sun_hour"].AsReal;
            bool isSunFixed = reqmap["is_sun_fixed"].AsBoolean;
            bool isExternallyVisible = reqmap["is_externally_visible"].AsBoolean;
            bool allowDirectTeleport = reqmap["allow_direct_teleport"].AsBoolean;
            bool denyAnonymous = reqmap["deny_anonymous"].AsBoolean;
            bool denyAgeUnverified = reqmap["deny_age_unverified"].AsBoolean;
            bool allowVoiceChat = reqmap["allow_voice_chat"].AsBoolean;
            var invoiceID = reqmap["invoice"].AsUUID;

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
                estate.UseGlobalTime = sun_hour < 6;
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
                m_Scene.EstateService.Update(estate);

                m_Agent.SendEstateUpdateInfo(invoiceID, UUID.Zero, estate, m_Scene.ID);
                m_Scene.TriggerEstateUpdate();
            }

            using (var httpres = httpreq.BeginResponse())
            {
                httpres.ContentType = "application/llsd+xml";
                using (var outStream = httpres.GetOutputStream())
                {
                    LlsdXml.Serialize(new Map(), outStream);
                }
            }
        }
    }
}

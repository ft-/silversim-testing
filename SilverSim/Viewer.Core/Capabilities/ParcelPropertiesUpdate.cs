using SilverSim.Scene.Types.Scene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Types.Parcel;
using System.Net;
using SilverSim.Types.StructuredData.Llsd;
using SilverSim.Types;

namespace SilverSim.Viewer.Core.Capabilities
{
    public class ParcelPropertiesUpdate : ICapabilityInterface
    {
        readonly ViewerAgent m_Agent;
        readonly SceneInterface m_Scene;

        public ParcelPropertiesUpdate(ViewerAgent agent, SceneInterface scene)
        {
            m_Agent = agent;
            m_Scene = scene;
        }

        public string CapabilityName
        {
            get
            {
                return "ParcelPropertiesUpdate";
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

            ParcelInfo pInfo;
            if(!m_Scene.Parcels.TryGetValue(reqmap["local_id"].AsInt, out pInfo))
            {
                if(!m_Scene.CanEditParcelDetails(m_Agent.Owner, pInfo))
                {
                    httpreq.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                    return;
                }
                pInfo.Flags = (ParcelFlags)reqmap["parcel_flags"].AsUInt;
                pInfo.Name = reqmap["name"].ToString();
                pInfo.SalePrice = reqmap["sale_price"].AsInt;
                pInfo.Description = reqmap["description"].ToString();
                string music_uri = reqmap["music_url"].ToString();
                pInfo.MusicURI = music_uri != string.Empty ? new URI(music_uri) : null;
                string media_uri = reqmap["media_url"].ToString();
                pInfo.MediaURI = media_uri != string.Empty ? new URI(media_uri) : null;
                pInfo.MediaDescription = reqmap["media_desc"].ToString();
                pInfo.MediaType = reqmap["media_type"].ToString();
                pInfo.MediaWidth = reqmap["media_width"].AsInt;
                pInfo.MediaHeight = reqmap["media_height"].AsInt;
                pInfo.MediaAutoScale = reqmap["auto_scale"].AsBoolean;
                pInfo.MediaLoop = reqmap["media_loop"].AsBoolean;
                pInfo.ObscureMedia = reqmap["obscure_media"].AsBoolean;
                pInfo.ObscureMusic = reqmap["obscure_music"].AsBoolean;
                pInfo.MediaID = reqmap["media_id"].AsUUID;
                pInfo.Group = new UGI(reqmap["group_id"].AsUUID);
                pInfo.PassPrice = reqmap["pass_price"].AsInt;
                pInfo.PassHours = reqmap["pass_hours"].AsReal;
                pInfo.Category = (ParcelCategory)reqmap["category"].AsInt;
                pInfo.AuthBuyer = new UUI(reqmap["auth_buyer_id"].AsUUID);
                pInfo.SnapshotID = reqmap["snapshot_id"].AsUUID;
                pInfo.LandingPosition = reqmap["user_location"].AsVector3;
                pInfo.LandingLookAt = reqmap["user_look_at"].AsVector3;
                pInfo.LandingType = (TeleportLandingType)reqmap["landing_type"].AsInt;

                m_Scene.TriggerParcelUpdate(pInfo);

                using (HttpResponse res = httpreq.BeginResponse("text/plain"))
                {
                    /* no action required here */
                }
            }
        }
    }
}

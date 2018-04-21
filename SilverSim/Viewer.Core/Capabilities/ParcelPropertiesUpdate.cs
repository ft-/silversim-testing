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
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using SilverSim.Types.Parcel;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Net;

namespace SilverSim.Viewer.Core.Capabilities
{
    public class ParcelPropertiesUpdate : ICapabilityInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("PARCEL PROPERTIES UPDATE");
        private readonly ViewerAgent m_Agent;
        private readonly SceneInterface m_Scene;
        private readonly string m_RemoteIP;

        public ParcelPropertiesUpdate(ViewerAgent agent, SceneInterface scene, string remoteip)
        {
            m_Agent = agent;
            m_Scene = scene;
            m_RemoteIP = remoteip;
        }

        public string CapabilityName => "ParcelPropertiesUpdate";

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
            catch (Exception e)
            {
                m_Log.ErrorFormat("Exception {0}: {1}\n{2}", e.GetType().FullName, e.Message, e.StackTrace);
                httpreq.ErrorResponse(HttpStatusCode.UnsupportedMediaType, "Unsupported Media Type");
                return;
            }
            if (reqmap == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML");
                return;
            }

            ParcelInfo pInfo;
            if (!m_Scene.Parcels.TryGetValue(reqmap["local_id"].AsInt, out pInfo))
            {
                httpreq.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }

            if (!m_Scene.CanEditParcelDetails(m_Agent.Owner, pInfo))
            {
                httpreq.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }

            byte[] parcelFlagsBinary = (BinaryData)reqmap["parcel_flags"];
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(parcelFlagsBinary);
            }
            uint parcelFlags = BitConverter.ToUInt32(parcelFlagsBinary, 0);
            pInfo.Flags = (ParcelFlags)parcelFlags;
            pInfo.Name = reqmap["name"].ToString();
            pInfo.SalePrice = reqmap["sale_price"].AsInt;
            pInfo.Description = reqmap["description"].ToString();
            var music_uri = reqmap["music_url"].ToString();
            pInfo.MusicURI = music_uri != string.Empty && Uri.IsWellFormedUriString(music_uri, UriKind.Absolute) ? new URI(music_uri) : null;
            var media_uri = reqmap["media_url"].ToString();
            pInfo.MediaURI = media_uri != string.Empty && Uri.IsWellFormedUriString(media_uri, UriKind.Absolute) ? new URI(media_uri) : null;
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
            pInfo.AuthBuyer = new UGUIWithName(reqmap["auth_buyer_id"].AsUUID);
            pInfo.SnapshotID = reqmap["snapshot_id"].AsUUID;
            pInfo.LandingPosition = reqmap["user_location"].AsVector3;
            pInfo.LandingLookAt = reqmap["user_look_at"].AsVector3;
            pInfo.LandingType = (TeleportLandingType)reqmap["landing_type"].AsInt;
            if(reqmap.ContainsKey("see_avs"))
            {
                pInfo.SeeAvatars = reqmap["see_avs"].AsBoolean;
            }
            if(reqmap.ContainsKey("group_av_sounds"))
            {
                pInfo.GroupAvatarSounds = reqmap["group_av_sounds"].AsBoolean;
            }
            if(reqmap.ContainsKey("any_av_sounds"))
            {
                pInfo.AnyAvatarSounds = reqmap["any_av_sounds"].AsBoolean;
            }
            if(reqmap.ContainsKey("obscure_media"))
            {
                pInfo.ObscureMedia = reqmap["obscure_media"].AsBoolean;
            }
            if(reqmap.ContainsKey("obscure_music"))
            {
                pInfo.ObscureMusic = reqmap["obscure_music"].AsBoolean;
            }

            //media_prevent_camera_zoom
            //media_url_timeout
            //media_allow_navigate
            //media_current_url

            m_Scene.TriggerParcelUpdate(pInfo);

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

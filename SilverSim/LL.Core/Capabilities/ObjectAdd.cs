/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Types.Scene;
using SilverSim.StructuredData.LLSD;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Inventory;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace SilverSim.LL.Core.Capabilities
{
    public class ObjectAdd : ICapabilityInterface
    {
        private SceneInterface m_Scene;
        private UUI m_Creator;
        public string CapabilityName
        {
            get
            {
                return "ObjectAdd";
            }
        }

        public ObjectAdd(SceneInterface scene, UUI creator)
        {
            m_Scene = scene;
            m_Creator = creator;
        }

        UInt32 BinToUInt(IValue v)
        {
            if(!(v is BinaryData))
            {
                throw new ArgumentException();
            }

            BinaryData bd = (BinaryData)v;
            byte[] b = bd;
            if(b.Length != 4)
            {
                throw new ArgumentException();
            }

            if(BitConverter.IsLittleEndian)
            {
                Array.Reverse(b);
            }
            return BitConverter.ToUInt32(b, 0);
        }

        public void HttpRequestHandler(HttpRequest httpreq)
        {
            HttpResponse res;
            IValue iv;
            if (httpreq.Method != "POST")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            if(httpreq.ContentType != "application/llsd+xml")
            {
                httpreq.ErrorResponse(HttpStatusCode.UnsupportedMediaType, "Unsupported Media Type");
                return;
            }
            try
            {
                iv = LLSD_XML.Deserialize(httpreq.Body);
            }
            catch
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                return;
            }

            if(!(iv is Map))
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                return;
            }

            Map rm = (Map)iv;
            Messages.Object.ObjectAdd m = new Messages.Object.ObjectAdd();
            m.AgentID = m_Creator.ID;
            try
            {
                if (rm.ContainsKey("ObjectData"))
                {
                    Map om = rm["ObjectData"] as Map;
                    /* new version */
                    m.BypassRaycast = om["BypassRaycast"].AsBoolean;
                    m.EveryOnePermissions = (InventoryPermissionsMask)BinToUInt(om["EveryoneMask"]);
                    m.AddFlags = BinToUInt(om["Flags"]);
                    m.GroupPermissions = (InventoryPermissionsMask)BinToUInt(om["GroupMask"]);
                    m.Material = (PrimitiveMaterial)om["Material"].AsInt;
                    m.NextOwnerPermissions = (InventoryPermissionsMask)BinToUInt(om["NextOwnerMask"]);
                    m.PCode = (PrimitiveCode)om["PCode"].AsUInt;
                    if (om.ContainsKey("Path"))
                    {
                        if (!(om["Path"] is Map))
                        {
                            httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                            return;
                        }

                        Map pm = om["Path"] as Map;

                        m.PathBegin = (ushort)pm["Begin"].AsUInt;
                        m.PathCurve = (byte)pm["Curve"].AsUInt;
                        m.PathEnd = (ushort)pm["End"].AsUInt;
                        m.PathRadiusOffset = (sbyte)pm["RadiusOffset"].AsInt;
                        m.PathRevolutions = (byte)pm["Revolutions"].AsUInt;
                        m.PathScaleX = (byte)pm["ScaleX"].AsInt;
                        m.PathScaleY = (byte)pm["ScaleY"].AsInt;
                        m.PathShearX = (byte)pm["ShearX"].AsInt;
                        m.PathShearY = (byte)pm["ShearY"].AsInt;
                        m.PathSkew = (sbyte)pm["Skew"].AsInt;
                        m.PathTaperX = (sbyte)pm["TaperX"].AsInt;
                        m.PathTaperY = (sbyte)pm["TaperY"].AsInt;
                        m.PathTwist = (sbyte)pm["Twist"].AsInt;
                        m.PathTwistBegin = (sbyte)pm["TwistBegin"].AsInt;
                    }

                    if (om.ContainsKey("Profile"))
                    {
                        if (!(om["Profile"] is Map))
                        {
                            httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                            return;
                        }

                        Map pm = om["Profile"] as Map;
                        m.ProfileBegin = (ushort)pm["Begin"].AsUInt;
                        m.ProfileCurve = (byte)pm["Curve"].AsUInt;
                        m.ProfileEnd = (ushort)pm["End"].AsUInt;
                        m.ProfileHollow = (ushort)pm["Hollow"].AsUInt;
                    }

                    m.RayEndIsIntersection = om["RayEndIsIntersection"].AsBoolean;
                    m.RayTargetID = om["RayTargetId"].AsUUID;
                    m.State = (byte)om["State"].AsUInt;
                    m.LastAttachPoint = (AttachmentPoint)om["LastAttachPoint"].AsUInt;

                    m.RayEnd = om["RayEnd"].AsVector3;
                    m.RayStart = om["RayStart"].AsVector3;
                    m.Scale = om["Scale"].AsVector3;
                    m.Rotation = om["Rotation"].AsQuaternion;

                    if (rm.ContainsKey("AgentData"))
                    {
                        if (!(rm["AgentData"] is Map))
                        {
                            httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                            return;
                        }

                        Map am = rm["AgentData"] as Map;
                        m.GroupID = am["GroupId"].AsUUID;
                    }
                }
                else
                {
                    m.BypassRaycast = rm["bypass_raycast"].AsBoolean;
                    m.EveryOnePermissions = (InventoryPermissionsMask)BinToUInt(rm["everyone_mask"]);
                    m.AddFlags = BinToUInt(rm["flags"]);
                    m.GroupID = rm["group_id"].AsUUID;
                    m.GroupPermissions = (InventoryPermissionsMask)BinToUInt(rm["group_mask"]);
                    m.ProfileHollow = (ushort)rm["hollow"].AsUInt;
                    m.Material = (PrimitiveMaterial)rm["material"].AsUInt;
                    m.NextOwnerPermissions = (InventoryPermissionsMask)BinToUInt(rm["next_owner_mask"]);
                    m.PCode = (PrimitiveCode)rm["p_code"].AsUInt;
                    m.PathBegin = (ushort)rm["path_begin"].AsUInt;
                    m.PathCurve = (byte)rm["path_curve"].AsUInt;
                    m.PathEnd = (ushort)rm["path_end"].AsUInt;
                    m.PathRadiusOffset = (sbyte)rm["path_radius_offset"].AsInt;
                    m.PathRevolutions = (byte)rm["path_revolutions"].AsUInt;
                    m.PathScaleX = (byte)rm["path_scale_x"].AsUInt;
                    m.PathScaleY = (byte)rm["path_scale_y"].AsUInt;
                    m.PathShearX = (byte)rm["path_shear_x"].AsUInt;
                    m.PathShearY = (byte)rm["path_shear_y"].AsUInt;
                    m.PathSkew = (sbyte)rm["path_skew"].AsInt;
                    m.PathTaperX = (sbyte)rm["path_taper_x"].AsInt;
                    m.PathTaperY = (sbyte)rm["path_taper_y"].AsInt;
                    m.PathTwist = (sbyte)rm["path_twist"].AsInt;
                    m.PathTwistBegin = (sbyte)rm["path_twist_begin"].AsInt;
                    m.ProfileBegin = (ushort)rm["profile_begin"].AsInt;
                    m.ProfileCurve = (byte)rm["profile_curve"].AsInt;
                    m.ProfileEnd = (ushort)rm["profile_end"].AsInt;
                    m.RayEndIsIntersection = rm["ray_end_is_intersection"].AsBoolean;
                    m.RayTargetID = rm["ray_target_id"].AsUUID;

                    m.State = (byte)rm["state"].AsUInt;
                    m.LastAttachPoint = (AttachmentPoint)rm["last_attach_point"].AsUInt;

                    m.RayEnd = rm["RayEnd"].AsVector3;
                    m.RayStart = rm["RayStart"].AsVector3;
                    m.Scale = rm["Scale"].AsVector3;
                    m.Rotation =rm["Rotation"].AsQuaternion;
                }
            }
            catch
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                return;
            }
            UInt32 localID;
            try
            {
                localID = m_Scene.ObjectAdd(m);
            }
            catch
            {
                httpreq.ErrorResponse(HttpStatusCode.InternalServerError, "Internal Server Error");
                return;
            }
            byte[] resultbytes = BitConverter.GetBytes(localID);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(resultbytes);

            res = httpreq.BeginResponse("application/xml");
            using(StreamWriter w = new StreamWriter(res.GetOutputStream(), UTF8NoBOM))
            {
                w.Write(string.Format("<llsd><map><key>local_id</key><binary encoding=\"base64\">{0}</binary></map></llsd>", Convert.ToBase64String(resultbytes)));
                w.Flush();
            }
            res.Close();
        }

        static Encoding UTF8NoBOM = new UTF8Encoding(false);
    }
}

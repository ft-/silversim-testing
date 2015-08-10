// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using SilverSim.Types.Primitive;
using System.Globalization;

namespace SilverSim.Scene.Types.StructuredData
{
    public static partial class ObjectXML2
    {
        private readonly static CultureInfo EnUsCulture = new CultureInfo("en-us");

        public static void Serialize(XmlTextWriter writer, ObjectGroup group)
        {
            writer.WriteStartElement("SceneObjectGroup");

            writer.WriteEndElement();
        }

        private static void Serialize(XmlTextWriter writer, string name, Vector3 v)
        {
            writer.WriteStartElement(name);
            writer.WriteElementString("X", v.X_String);
            writer.WriteElementString("Y", v.Y_String);
            writer.WriteElementString("Z", v.Z_String);
            writer.WriteEndElement();
        }

        private static void Serialize(XmlTextWriter writer, string name, ColorAlpha v)
        {
            writer.WriteStartElement(name);
            writer.WriteElementString("R", v.R_AsByte.ToString());
            writer.WriteElementString("G", v.G_AsByte.ToString());
            writer.WriteElementString("B", v.B_AsByte.ToString());
            writer.WriteElementString("A", v.A_AsByte.ToString());
            writer.WriteEndElement();
        }

        private static void Serialize(XmlTextWriter writer, string name, Quaternion q)
        {
            writer.WriteStartElement(name);
            writer.WriteElementString("X", q.X_String);
            writer.WriteElementString("Y", q.Y_String);
            writer.WriteElementString("Z", q.Z_String);
            writer.WriteElementString("W", q.W_String);
            writer.WriteEndElement();
        }

        private static void Serialize(XmlTextWriter writer, string name, double v)
        {
            writer.WriteElementString(name, string.Format(EnUsCulture, "{0}", v));
        }

        private static void Serialize(XmlTextWriter writer, string name, string v)
        {
            writer.WriteElementString(name, v);
        }

        private static void Serialize(XmlTextWriter writer, string name, bool v)
        {
            writer.WriteElementString(name, v.ToString());
        }

        private static void Serialize(XmlTextWriter writer, string name, int v)
        {
            writer.WriteElementString(name, v.ToString());
        }

        private static void Serialize(XmlTextWriter writer, string name, uint v)
        {
            writer.WriteElementString(name, v.ToString());
        }

        private static void Serialize(XmlTextWriter writer, string name, byte v)
        {
            writer.WriteElementString(name, v.ToString());
        }

        private static void Serialize(XmlTextWriter writer, string name, sbyte v)
        {
            writer.WriteElementString(name, v.ToString());
        }

        private static void Serialize(XmlTextWriter writer, string name, byte[] v)
        {
            if (v.Length == 0)
            {
                writer.WriteElementString(name, "");
            }
            else
            {
                writer.WriteElementString(name, Convert.ToBase64String(v));
            }
        }

        private static void Serialize(XmlTextWriter writer, string name, UUID v)
        {
            if (!string.IsNullOrEmpty(name))
            {
                writer.WriteStartElement(name);
            }
            writer.WriteStartElement("UUID");
            writer.WriteElementString(name, v.ToString());
            writer.WriteEndElement();
            if(!string.IsNullOrEmpty(name))
            {
                writer.WriteEndElement();
            }
        }

        private static void Serialize(XmlTextWriter writer, ObjectPart part)
        {
            writer.WriteStartElement("SceneObjectPart");

            Serialize(writer, "AllowedDrop", part.IsAllowedDrop);
            Serialize(writer, "CreatorID", part.Creator.ID);
            Serialize(writer, "FolderID", UUID.Zero);
            Serialize(writer, "InventorySerial", 1);
            Serialize(writer, "", part.ID);
            Serialize(writer, "LocalId", part.LocalID);
            Serialize(writer, "Name", part.Name);
            Serialize(writer, "Material", (uint)part.Material);
            Serialize(writer, "PassTouches", part.IsPassTouches);
            Serialize(writer, "PassCollisions", part.IsPassCollisions);
            Serialize(writer, "RegionHandle", "0");
            Serialize(writer, "ScriptAccessPin", part.ScriptAccessPin);
            Serialize(writer, "GroupPosition", part.Position);
            Serialize(writer, "OffsetPosition", part.LocalPosition);
            Serialize(writer, "RotationOffset", part.Rotation);
            Serialize(writer, "Velocity", part.Velocity);
            Serialize(writer, "AngularVelocity", part.AngularVelocity);
            Serialize(writer, "Acceleration", part.Acceleration);
            Serialize(writer, "Description", part.Description);
            TextureEntry te = part.TextureEntry;
            ObjectPart.TextParam textparam = part.Text;
            Serialize(writer, "Color", textparam.TextColor);
            Serialize(writer, "Text", textparam.Text);
            Serialize(writer, "SitName", part.SitText);
            Serialize(writer, "TouchName", part.TouchText);
            Serialize(writer, "LinkNum", part.LinkNumber);
            Serialize(writer, "ClickAction", (byte)part.ClickAction);

            ObjectPart.PrimitiveShape shape = part.Shape;
            writer.WriteStartElement("Shape");
            Serialize(writer, "ProfileCurve", shape.ProfileCurve);
            Serialize(writer, "TextureEntry", part.TextureEntryBytes);
            Serialize(writer, "ExtraParams", part.ExtraParamsBytes);
            Serialize(writer, "PathBegin", shape.PathBegin);
            Serialize(writer, "PathCurve", shape.PathCurve);
            Serialize(writer, "PathEnd", shape.PathEnd);
            Serialize(writer, "PathRadiusOffset", shape.PathRadiusOffset);
            Serialize(writer, "PathRevolutions", shape.PathRevolutions);
            Serialize(writer, "PathScaleX", shape.PathScaleX);
            Serialize(writer, "PathScaleY", shape.PathScaleY);
            Serialize(writer, "PathShearX", shape.PathShearX);
            Serialize(writer, "PathShearY", shape.PathShearY);
            Serialize(writer, "PathSkew", shape.PathSkew);
            Serialize(writer, "PathTaperX", shape.PathTaperX);
            Serialize(writer, "PathTaperY", shape.PathTaperY);
            Serialize(writer, "PathTwist", shape.PathTwist);
            Serialize(writer, "PathTwistBegin", shape.PathTwistBegin);
            Serialize(writer, "PCode", (uint)shape.PCode);
            Serialize(writer, "ProfileBegin", shape.ProfileBegin);
            Serialize(writer, "ProfileEnd", shape.ProfileEnd);
            Serialize(writer, "ProfileHollow", shape.ProfileHollow);
            Serialize(writer, "State", shape.State);
            Serialize(writer, "LastAttachPoint", (uint)part.ObjectGroup.AttachPoint);
            /*
            <ProfileShape>Square</ProfileShape>
            <HollowShape>Same</HollowShape>
             */
            //Serialize(writer, "ProfileShape", );
            //Serialize(writer, "HollowShape", (uint)shape.ProfileHollow);
            Serialize(writer, "SculptTexture", shape.SculptMap);
            Serialize(writer, "SculptType", (uint)shape.SculptType);
            ObjectPart.ProjectionParam projparam = part.Projection;
            ObjectPart.FlexibleParam flexparam = part.Flexible;
            ObjectPart.PointLightParam lightparam = part.PointLight;
            Serialize(writer, "FlexiSoftness", flexparam.Softness);
            Serialize(writer, "FlexiTension", flexparam.Tension);
            Serialize(writer, "FlexiDrag", flexparam.Friction);
            Serialize(writer, "FlexiGravity", flexparam.Gravity);
            Serialize(writer, "FlexiWind", flexparam.Wind);
            Serialize(writer, "FlexiForceX", flexparam.Force.X_String);
            Serialize(writer, "FlexiForceY", flexparam.Force.Y_String);
            Serialize(writer, "FlexiForceZ", flexparam.Force.Z_String);
            Serialize(writer, "LightColorR", lightparam.LightColor.R_AsByte);
            Serialize(writer, "LightColorG", lightparam.LightColor.G_AsByte);
            Serialize(writer, "LightColorB", lightparam.LightColor.B_AsByte);
            Serialize(writer, "LightColorA", 1);
            Serialize(writer, "LightRadius", lightparam.Radius);
            Serialize(writer, "LightCutoff", lightparam.Cutoff);
            Serialize(writer, "LightFalloff", lightparam.Falloff);
            Serialize(writer, "LightIntensity", lightparam.Intensity);
            Serialize(writer, "FlexiEntry", flexparam.IsFlexible);
            Serialize(writer, "LightEntry", lightparam.IsLight);
            //Serialize(writer, "SculptEntry", )
            writer.WriteEndElement();

            Serialize(writer, "Scale", part.Size);
            Serialize(writer, "SitTargetOrientation", part.SitTargetOrientation);
            Serialize(writer, "SitTargetPosition", part.SitTargetOffset);
            Serialize(writer, "SitTargetOrientationLL", part.SitTargetOrientation);
            Serialize(writer, "SitTargetPositionLL", part.SitTargetOffset);
            Serialize(writer, "ParentID", part.ObjectGroup.RootPart.LinkNumber);
            Serialize(writer, "CreationDate", part.CreationDate.AsUInt); /* check for prim logic */
            Serialize(writer, "Category", "0");
            Serialize(writer, "SalePrice", "0");
            Serialize(writer, "ObjectSaleType", "0");
            Serialize(writer, "OwnershipCost", "0");
            Serialize(writer, "GroupID", part.ObjectGroup.Group.ID);
            Serialize(writer, "OwnerID", part.ObjectGroup.Owner.ID);
            Serialize(writer, "LastOwnerID", part.ObjectGroup.LastOwner.ID);
            Serialize(writer, "BaseMask", (uint)part.BaseMask);
            Serialize(writer, "OwnerMask", (uint)part.OwnerMask);
            Serialize(writer, "GroupMask", (uint)part.GroupMask);
            Serialize(writer, "EveryoneMask", (uint)part.EveryoneMask);
            Serialize(writer, "NextOwnerMask", (uint)part.NextOwnerMask);
            /*
          <Flags>None</Flags>
             */
            ObjectPart.CollisionSoundParam collparam = part.CollisionSound;
            Serialize(writer, "CollisionSound", collparam.ImpactSound);
            Serialize(writer, "CollisionSoundVolume", collparam.ImpactVolume);
            /*
          <AttachedPos>
            <X>0</X>
            <Y>0</Y>
            <Z>0</Z>
          </AttachedPos>
          */
            //Serialize(writer, "AttachedPos", part.Group.AttachedPos);
            Serialize(writer, "TextureAnimation", part.TextureAnimationBytes);
            Serialize(writer, "ParticleSystem", part.ParticleSystemBytes);
            /*
            <PayPrice0>-2</PayPrice0>
            <PayPrice1>-2</PayPrice1>
            <PayPrice2>-2</PayPrice2>
            <PayPrice3>-2</PayPrice3>
            <PayPrice4>-2</PayPrice4>
              */
            Serialize(writer, "CreatorData", part.Creator.CreatorData);
            writer.WriteEndElement();
        }
    }
}

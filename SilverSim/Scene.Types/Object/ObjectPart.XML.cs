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


using SilverSim.Scene.Types.Object.Localization;
using SilverSim.Scene.Types.Object.Parameters;
using SilverSim.Scene.Types.Physics.Vehicle;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Inventory;
using SilverSim.Types.Primitive;
using SilverSim.Types.StructuredData.Json;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace SilverSim.Scene.Types.Object
{
    public partial class ObjectPart
    {
        #region XML Serialization
        public void ToXml(XmlTextWriter writer, XmlSerializationOptions options = XmlSerializationOptions.None)
        {
            ToXml(writer, UGUI.Unknown, options);
        }

        public void ToXml(XmlTextWriter writer, UGUI nextOwner, XmlSerializationOptions options = XmlSerializationOptions.None)
        {
            lock (m_DataLock)
            {
                writer.WriteStartElement("SceneObjectPart");
                {
                    writer.WriteNamedValue("AllowedDrop", IsAllowedDrop);
                    writer.WriteUUID("CreatorID", Creator.ID);
                    if (!string.IsNullOrEmpty(Creator.CreatorData))
                    {
                        writer.WriteNamedValue("CreatorData", Creator.CreatorData);
                    }

                    writer.WriteUUID("FolderID", UUID.Zero);

                    Inventory.ToXml(writer, nextOwner, options);

                    writer.WriteUUID("UUID", ID);
                    writer.WriteNamedValue("LocalId", LocalID[ObjectGroup?.Scene?.ID ?? UUID.Zero]);
                    writer.WriteNamedValue("Name", Name);
                    writer.WriteNamedValue("Material", (int)Material);
                    writer.WriteNamedValue("IsRotateXEnabled", ObjectGroup.IsRotateXEnabled);
                    writer.WriteNamedValue("IsRotateYEnabled", ObjectGroup.IsRotateYEnabled);
                    writer.WriteNamedValue("IsRotateZEnabled", ObjectGroup.IsRotateZEnabled);
                    writer.WriteNamedValue("PassTouches", PassTouchMode == PassEventMode.Always);
                    writer.WriteNamedValue("PassTouchMode", PassTouchMode.ToString());
                    writer.WriteNamedValue("PassCollisions", PassCollisionMode == PassEventMode.Always);
                    writer.WriteNamedValue("PassCollisionMode", PassCollisionMode.ToString());
                    writer.WriteNamedValue("RegionHandle", ObjectGroup.Scene?.GridPosition.RegionHandle.ToString() ?? "0");
                    writer.WriteNamedValue("ScriptAccessPin", ScriptAccessPin);
                    writer.WriteNamedValue("GroupPosition", GlobalPosition);
                    writer.WriteNamedValue("OffsetPosition", LocalPosition);
                    writer.WriteNamedValue("RotationOffset", LocalRotation);
                    writer.WriteNamedValue("Velocity", Velocity);
                    writer.WriteNamedValue("AngularVelocity", AngularVelocity);
                    writer.WriteNamedValue("Acceleration", Acceleration);
                    writer.WriteNamedValue("Description", Description);
                    TextParam tp = Text;
                    writer.WriteNamedValue("Color", tp.TextColor);
                    writer.WriteNamedValue("Text", tp.Text);
                    writer.WriteNamedValue("SitName", SitText);
                    writer.WriteNamedValue("TouchName", TouchText);
                    writer.WriteNamedValue("LinkNum", LinkNumber);
                    writer.WriteNamedValue("ClickAction", (int)ClickAction);

                    writer.WriteStartElement("Shape");
                    {
                        PrimitiveShape shape = Shape;

                        writer.WriteNamedValue("ProfileCurve", shape.ProfileCurve);
                        writer.WriteNamedValue("TextureEntry", TextureEntryBytes);
                        writer.WriteNamedValue("ExtraParams", ExtraParamsBytes);
                        writer.WriteNamedValue("PathBegin", shape.PathBegin);
                        writer.WriteNamedValue("PathCurve", shape.PathCurve);
                        writer.WriteNamedValue("PathEnd", shape.PathEnd);
                        writer.WriteNamedValue("PathRadiusOffset", shape.PathRadiusOffset);
                        writer.WriteNamedValue("PathRevolutions", shape.PathRevolutions);
                        writer.WriteNamedValue("PathScaleX", shape.PathScaleX);
                        writer.WriteNamedValue("PathScaleY", shape.PathScaleY);
                        writer.WriteNamedValue("PathShearX", shape.PathShearX);
                        writer.WriteNamedValue("PathShearY", shape.PathShearY);
                        writer.WriteNamedValue("PathSkew", shape.PathSkew);
                        writer.WriteNamedValue("PathTaperX", shape.PathTaperX);
                        writer.WriteNamedValue("PathTaperY", shape.PathTaperY);
                        writer.WriteNamedValue("PathTwist", shape.PathTwist);
                        writer.WriteNamedValue("PathTwistBegin", shape.PathTwistBegin);
                        writer.WriteNamedValue("PCode", (int)shape.PCode);
                        writer.WriteNamedValue("ProfileBegin", shape.ProfileBegin);
                        writer.WriteNamedValue("ProfileEnd", shape.ProfileEnd);
                        writer.WriteNamedValue("ProfileHollow", shape.ProfileHollow);
                        writer.WriteNamedValue("State", (int)shape.State);
                        writer.WriteNamedValue("LastAttachPoint", (int)ObjectGroup.AttachPoint);
                        byte profilecurve = shape.ProfileCurve;
                        writer.WriteNamedValue("ProfileShape", ((PrimitiveProfileShape)(profilecurve & 0x0F)).ToString());
                        writer.WriteNamedValue("HollowShape", ((PrimitiveProfileHollowShape)(profilecurve & 0xF0)).ToString());
                        writer.WriteUUID("SculptTexture", shape.SculptMap);
                        writer.WriteNamedValue("SculptType", (int)shape.SculptType);

                        FlexibleParam fp = Flexible;
                        PointLightParam plp = PointLight;

                        writer.WriteNamedValue("FlexiSoftness", fp.Softness);
                        writer.WriteNamedValue("FlexiTension", fp.Tension);
                        writer.WriteNamedValue("FlexiDrag", fp.Friction);
                        writer.WriteNamedValue("FlexiGravity", fp.Gravity);
                        writer.WriteNamedValue("FlexiWind", fp.Wind);
                        writer.WriteNamedValue("FlexiForce", fp.Force, true);

                        writer.WriteNamedValue("LightColor", plp.LightColor, true);
                        writer.WriteNamedValue("LightRadius", plp.Radius);
                        writer.WriteNamedValue("LightCutoff", plp.Cutoff);
                        writer.WriteNamedValue("LightFalloff", plp.Falloff);
                        writer.WriteNamedValue("LightIntensity", plp.Intensity);

                        writer.WriteNamedValue("FlexiEntry", fp.IsFlexible);
                        writer.WriteNamedValue("LightEntry", plp.IsLight);
                        writer.WriteNamedValue("SculptEntry", shape.Type == PrimitiveShapeType.Sculpt);
                        PrimitiveMedia media = Media;
                        if (media != null)
                        {
                            writer.WriteStartElement("Media");
                            using (var ms = new MemoryStream())
                            {
                                using (XmlTextWriter innerWriter = ms.UTF8XmlTextWriter())
                                {
                                    Media.ToXml(innerWriter);
                                }
                                writer.WriteValue(ms.ToArray().FromUTF8Bytes());
                            }
                            writer.WriteEndElement();
                        }
                    }
                    writer.WriteEndElement();

                    writer.WriteNamedValue("Scale", Size);
                    writer.WriteNamedValue("SitTargetActive", IsSitTargetActive);
                    writer.WriteNamedValue("SitTargetOrientation", SitTargetOrientation);
                    writer.WriteNamedValue("SitTargetPosition", SitTargetOffset);
                    writer.WriteNamedValue("SitTargetPositionLL", SitTargetOffset);
                    writer.WriteNamedValue("SitTargetOrientationLL", SitTargetOrientation);
                    string sitanim = SitAnimation;
                    if(sitanim?.Length != 0)
                    {
                        writer.WriteNamedValue("SitAnimation", sitanim);
                    }
                    writer.WriteNamedValue("UnSitTargetActive", IsUnSitTargetActive);
                    writer.WriteNamedValue("UnSitTargetOrientation", UnSitTargetOrientation);
                    writer.WriteNamedValue("UnSitTargetPosition", UnSitTargetOffset);
                    writer.WriteNamedValue("ParentID", ObjectGroup.RootPart.ID);
                    writer.WriteNamedValue("CreationDate", CreationDate.AsULong.ToString());
                    if ((options & XmlSerializationOptions.WriteRezDate) != XmlSerializationOptions.None)
                    {
                        writer.WriteNamedValue("RezDate", RezDate.AsULong.ToString());
                    }
                    writer.WriteNamedValue("Category", ObjectGroup.Category);
                    if (this == ObjectGroup.RootPart)
                    {
                        writer.WriteNamedValue("SalePrice", ObjectGroup.SalePrice);
                        writer.WriteNamedValue("ObjectSaleType", (int)ObjectGroup.SaleType);
                    }
                    else
                    {
                        writer.WriteNamedValue("SalePrice", 10);
                        writer.WriteNamedValue("ObjectSaleType", (int)ObjectPartInventoryItem.SaleInfoData.SaleType.NoSale);
                    }
                    writer.WriteNamedValue("OwnershipCost", ObjectGroup.OwnershipCost);
                    if (XmlSerializationOptions.None != (options & XmlSerializationOptions.WriteOwnerInfo))
                    {
                        writer.WriteUUID("GroupID", ObjectGroup.Group.ID);
                        writer.WriteUUID("OwnerID", ObjectGroup.Owner.ID);
                        writer.WriteUUID("LastOwnerID", ObjectGroup.LastOwner.ID);
                    }
                    else if (XmlSerializationOptions.None != (options & XmlSerializationOptions.AdjustForNextOwner))
                    {
                        writer.WriteUUID("GroupID", UUID.Zero);
                        writer.WriteUUID("OwnerID", nextOwner.ID);
                        writer.WriteUUID("LastOwnerID", ObjectGroup.Owner.ID);
                    }
                    else
                    {
                        writer.WriteUUID("GroupID", UUID.Zero);
                        writer.WriteUUID("OwnerID", UUID.Zero);
                        writer.WriteUUID("LastOwnerID", ObjectGroup.LastOwner.ID);
                    }
                    writer.WriteNamedValue("BaseMask", (uint)BaseMask);
                    if (XmlSerializationOptions.None == (options & XmlSerializationOptions.AdjustForNextOwner))
                    {
                        writer.WriteNamedValue("OwnerMask", (uint)OwnerMask);
                    }
                    else
                    {
                        writer.WriteNamedValue("OwnerMask", (uint)NextOwnerMask);
                    }
                    writer.WriteNamedValue("GroupMask", (uint)GroupMask);
                    writer.WriteNamedValue("EveryoneMask", (uint)EveryoneMask);
                    writer.WriteNamedValue("NextOwnerMask", (uint)NextOwnerMask);
                    writer.WriteNamedValue("Damage", Damage);
                    PrimitiveFlags flags = Flags;
                    var flagsStrs = new List<string>();
                    if ((flags & PrimitiveFlags.Physics) != 0)
                    {
                        flagsStrs.Add("Physics");
                    }
                    if (IsScripted)
                    {
                        flagsStrs.Add("Scripted");
                    }
                    if ((flags & PrimitiveFlags.Touch) != 0)
                    {
                        flagsStrs.Add("Touch");
                    }
                    if ((flags & PrimitiveFlags.TakesMoney) != 0)
                    {
                        flagsStrs.Add("Money");
                    }
                    if ((flags & PrimitiveFlags.Phantom) != 0)
                    {
                        flagsStrs.Add("Phantom");
                    }
                    if ((flags & PrimitiveFlags.IncludeInSearch) != 0)
                    {
                        flagsStrs.Add("JointWheel"); /* yes, its name is a messed up naming in OpenSim object xml */
                    }
                    if ((flags & PrimitiveFlags.AllowInventoryDrop) != 0)
                    {
                        flagsStrs.Add("AllowInventoryDrop");
                    }
                    if ((flags & PrimitiveFlags.CameraDecoupled) != 0)
                    {
                        flagsStrs.Add("CameraDecoupled");
                    }
                    if ((flags & PrimitiveFlags.AnimSource) != 0)
                    {
                        flagsStrs.Add("AnimSource");
                    }
                    if ((flags & PrimitiveFlags.CameraSource) != 0)
                    {
                        flagsStrs.Add("CameraSource");
                    }
                    if ((flags & PrimitiveFlags.TemporaryOnRez) != 0 || (flags & PrimitiveFlags.Temporary) != 0)
                    {
                        flagsStrs.Add("Temporary");
                    }
                    writer.WriteNamedValue("Flags", string.Join(",", flagsStrs));
                    CollisionSoundParam sp = CollisionSound;
                    writer.WriteUUID("CollisionSound", sp.ImpactSound);
                    writer.WriteNamedValue("CollisionSoundVolume", sp.ImpactVolume);
                    writer.WriteNamedValue("CollisionSoundRadius", sp.ImpactSoundRadius);
                    writer.WriteNamedValue("CollisionSoundUseHitpoint", sp.ImpactUseHitpoint);
                    writer.WriteNamedValue("CollisionSoundUseChilds", sp.ImpactUseChilds);
                    if (!string.IsNullOrEmpty(MediaURL))
                    {
                        writer.WriteNamedValue("MediaUrl", MediaURL);
                    }
                    writer.WriteNamedValue("AttachedPos", ObjectGroup.AttachedPos);
                    writer.WriteNamedValue("AttachedRot", ObjectGroup.AttachedRot);

                    writer.WriteStartElement("DynAttrs");
                    LlsdXml.Serialize(DynAttrs, writer);
                    writer.WriteEndElement();

                    if (XmlSerializationOptions.None != (options & XmlSerializationOptions.WriteOwnerInfo))
                    {
                        writer.WriteNamedValue("RezzerID", ObjectGroup.RezzingObjectID);
                    }
                    writer.WriteNamedValue("TextureAnimation", TextureAnimationBytes);
                    writer.WriteNamedValue("ParticleSystem", ParticleSystemBytes);
                    writer.WriteNamedValue("PayPrice0", ObjectGroup.PayPrice0);
                    writer.WriteNamedValue("PayPrice1", ObjectGroup.PayPrice1);
                    writer.WriteNamedValue("PayPrice2", ObjectGroup.PayPrice2);
                    writer.WriteNamedValue("PayPrice3", ObjectGroup.PayPrice3);
                    writer.WriteNamedValue("PayPrice4", ObjectGroup.PayPrice4);
                    writer.WriteNamedValue("Buoyancy", (float)Buoyancy);
                    writer.WriteNamedValue("PhysicsShapeType", (int)PhysicsShapeType);
                    writer.WriteNamedValue("VolumeDetectActive", IsVolumeDetect);
                    writer.WriteNamedValue("Density", (float)PhysicsDensity);
                    writer.WriteNamedValue("Friction", (float)PhysicsFriction);
                    writer.WriteNamedValue("Bounce", (float)PhysicsRestitution);
                    writer.WriteNamedValue("GravityModifier", (float)PhysicsGravityMultiplier);
                    writer.WriteNamedValue("AllowUnsit", AllowUnsit);
                    writer.WriteNamedValue("ScriptedSitOnly", IsScriptedSitOnly);
                    writer.WriteNamedValue("WalkableCoefficientAvatar", WalkableCoefficientAvatar);
                    writer.WriteNamedValue("WalkableCoefficientA", WalkableCoefficientA);
                    writer.WriteNamedValue("WalkableCoefficientB", WalkableCoefficientB);
                    writer.WriteNamedValue("WalkableCoefficientC", WalkableCoefficientC);
                    writer.WriteNamedValue("WalkableCoefficientD", WalkableCoefficientD);
                    foreach(ObjectPartLocalizedInfo l in NamedLocalizations)
                    {
                        writer.WriteStartElement("LocalizationData");
                        writer.WriteAttributeString("name", l.LocalizationName);
                        byte[] serialization = l.Serialization;
                        writer.WriteBase64(serialization, 0, serialization.Length);
                        writer.WriteEndElement();
                    }
                    if (VehicleType != VehicleType.None)
                    {
                        writer.WriteStartElement("Vehicle");
                        {
                            writer.WriteNamedValue("TYPE", (int)VehicleType);
                            writer.WriteNamedValue("FLAGS", (int)VehicleFlags);

                            /* linear */
                            writer.WriteNamedValue("LMDIR", VehicleParams[VehicleVectorParamId.LinearMotorDirection]);
                            writer.WriteNamedValue("LMFTIME", VehicleParams[VehicleVectorParamId.LinearFrictionTimescale]);
                            writer.WriteNamedValue("LMDTIME", VehicleParams[VehicleVectorParamId.LinearMotorDecayTimescale].Length);
                            writer.WriteNamedValue("LMTIME", VehicleParams[VehicleVectorParamId.LinearMotorTimescale].Length);
                            writer.WriteNamedValue("LMOFF", VehicleParams[VehicleVectorParamId.LinearMotorOffset]);

                            /* linear extension (must be written after float value due to loading concept) */
                            writer.WriteNamedValue("LinearMotorDecayTimescaleVector", VehicleParams[VehicleVectorParamId.LinearMotorDecayTimescale]);
                            writer.WriteNamedValue("LinearMotorTimescaleVector", VehicleParams[VehicleVectorParamId.LinearMotorTimescale]);
                            writer.WriteNamedValue("LinearMotorAccelPosTimescaleVector", VehicleParams[VehicleVectorParamId.LinearMotorAccelPosTimescale]);
                            writer.WriteNamedValue("LinearMotorDecelPosTimescaleVector", VehicleParams[VehicleVectorParamId.LinearMotorDecelNegTimescale]);
                            writer.WriteNamedValue("LinearMotorAccelNegTimescaleVector", VehicleParams[VehicleVectorParamId.LinearMotorAccelPosTimescale]);
                            writer.WriteNamedValue("LinearMotorDecelNegTimescaleVector", VehicleParams[VehicleVectorParamId.LinearMotorDecelNegTimescale]);

                            /* angular */
                            writer.WriteNamedValue("AMDIR", VehicleParams[VehicleVectorParamId.AngularMotorDirection]);
                            writer.WriteNamedValue("AMTIME", VehicleParams[VehicleVectorParamId.AngularMotorTimescale].Length);
                            writer.WriteNamedValue("AMDTIME", VehicleParams[VehicleVectorParamId.AngularMotorDecayTimescale].Length);
                            writer.WriteNamedValue("AMFTIME", VehicleParams[VehicleVectorParamId.AngularFrictionTimescale]);

                            /* angular extension (must be written after float value due to loading concept) */
                            writer.WriteNamedValue("AngularMotorDecayTimescaleVector", VehicleParams[VehicleVectorParamId.AngularMotorDecayTimescale]);
                            writer.WriteNamedValue("AngularMotorTimescaleVector", VehicleParams[VehicleVectorParamId.AngularMotorTimescale]);
                            writer.WriteNamedValue("AngularMotorAccelPosTimescaleVector", VehicleParams[VehicleVectorParamId.AngularMotorAccelPosTimescale]);
                            writer.WriteNamedValue("AngularMotorDecelPosTimescaleVector", VehicleParams[VehicleVectorParamId.AngularMotorDecelPosTimescale]);
                            writer.WriteNamedValue("AngularMotorAccelNegTimescaleVector", VehicleParams[VehicleVectorParamId.AngularMotorAccelNegTimescale]);
                            writer.WriteNamedValue("AngularMotorDecelNegTimescaleVector", VehicleParams[VehicleVectorParamId.AngularMotorDecelNegTimescale]);

                            /* deflection */
                            writer.WriteNamedValue("ADEFF", VehicleParams[VehicleVectorParamId.AngularDeflectionEfficiency].Length);
                            writer.WriteNamedValue("ADTIME", VehicleParams[VehicleVectorParamId.AngularDeflectionTimescale].Length);
                            writer.WriteNamedValue("LDEFF", VehicleParams[VehicleVectorParamId.LinearDeflectionEfficiency].Length);
                            writer.WriteNamedValue("LDTIME", VehicleParams[VehicleVectorParamId.LinearDeflectionTimescale].Length);

                            /* extension (must be written after float value due to loading concept) */
                            writer.WriteNamedValue("AngularDeflectionEfficiencyVector", VehicleParams[VehicleVectorParamId.AngularDeflectionEfficiency]);
                            writer.WriteNamedValue("AngularDeflectionTimescaleVector", VehicleParams[VehicleVectorParamId.AngularDeflectionTimescale]);
                            writer.WriteNamedValue("LinearDeflectionEfficiencyVector", VehicleParams[VehicleVectorParamId.LinearDeflectionEfficiency]);
                            writer.WriteNamedValue("LinearDeflectionTimescaleVector", VehicleParams[VehicleVectorParamId.LinearDeflectionTimescale]);

                            /* banking */
                            writer.WriteNamedValue("BEFF", VehicleParams[VehicleFloatParamId.BankingEfficiency]);
                            writer.WriteNamedValue("BMIX", VehicleParams[VehicleFloatParamId.BankingMix]);
                            writer.WriteNamedValue("BTIME", VehicleParams[VehicleFloatParamId.BankingTimescale]);
                            writer.WriteNamedValue("BankingAzimuth", VehicleParams[VehicleFloatParamId.BankingAzimuth]);
                            writer.WriteNamedValue("InvertedBankingModifier", VehicleParams[VehicleFloatParamId.InvertedBankingModifier]);

                            /* hover and buoyancy */
                            writer.WriteNamedValue("HHEI", VehicleParams[VehicleFloatParamId.HoverHeight]);
                            writer.WriteNamedValue("HEFF", VehicleParams[VehicleFloatParamId.HoverEfficiency]);
                            writer.WriteNamedValue("HTIME", VehicleParams[VehicleFloatParamId.HoverTimescale]);
                            writer.WriteNamedValue("VBUO", VehicleParams[VehicleFloatParamId.Buoyancy]);

                            /* attractor */
                            writer.WriteNamedValue("VAEFF", VehicleParams[VehicleVectorParamId.VerticalAttractionEfficiency].Length);
                            writer.WriteNamedValue("VATIME", VehicleParams[VehicleVectorParamId.VerticalAttractionTimescale].Length);

                            /* extension (must be written after float value due to loading concept) */
                            writer.WriteNamedValue("VerticalAttractorEfficiencyVector", VehicleParams[VehicleVectorParamId.VerticalAttractionEfficiency]);
                            writer.WriteNamedValue("VerticalAttractorTimescaleVector", VehicleParams[VehicleVectorParamId.VerticalAttractionTimescale]);

                            /* reference */
                            writer.WriteNamedValue("REF_FRAME", VehicleParams[VehicleRotationParamId.ReferenceFrame]);

                            /* wind */
                            writer.WriteNamedValue("LinearWindEfficiency", VehicleParams[VehicleVectorParamId.LinearWindEfficiency]);
                            writer.WriteNamedValue("AngularWindEfficiency", VehicleParams[VehicleVectorParamId.AngularWindEfficiency]);

                            /* current */
                            writer.WriteNamedValue("LinearCurrentEfficiency", VehicleParams[VehicleVectorParamId.LinearCurrentEfficiency]);
                            writer.WriteNamedValue("AngularCurrentEfficiency", VehicleParams[VehicleVectorParamId.AngularCurrentEfficiency]);

                            /* mouselook */
                            writer.WriteNamedValue("MouselookAzimuth", VehicleParams[VehicleFloatParamId.MouselookAzimuth]);
                            writer.WriteNamedValue("MouselookAltitude", VehicleParams[VehicleFloatParamId.MouselookAltitude]);

                            /* disable motors */
                            writer.WriteNamedValue("DisableMotorsAbove", VehicleParams[VehicleFloatParamId.DisableMotorsAbove]);
                            writer.WriteNamedValue("DisableMotorsAfter", VehicleParams[VehicleFloatParamId.DisableMotorsAfter]);

                            /* move to target */
                            writer.WriteNamedValue("LinearMoveToTargetEfficiency", VehicleParams[VehicleVectorParamId.LinearMoveToTargetEfficiency]);
                            writer.WriteNamedValue("LinearMoveToTargetTimescale", VehicleParams[VehicleVectorParamId.LinearMoveToTargetTimescale]);
                            writer.WriteNamedValue("LinearMoveToTargetEpsilon", VehicleParams[VehicleVectorParamId.LinearMoveToTargetEpsilon]);
                            writer.WriteNamedValue("LinearMoveToTargetMaxOutput", VehicleParams[VehicleVectorParamId.LinearMoveToTargetMaxOutput]);

                            writer.WriteNamedValue("AngularMoveToTargetEfficiency", VehicleParams[VehicleVectorParamId.AngularMoveToTargetEfficiency]);
                            writer.WriteNamedValue("AngularMoveToTargetTimescale", VehicleParams[VehicleVectorParamId.AngularMoveToTargetTimescale]);
                            writer.WriteNamedValue("AngularMoveToTargetEpsilon", VehicleParams[VehicleVectorParamId.AngularMoveToTargetEpsilon]);
                            writer.WriteNamedValue("AngularMoveToTargetMaxOutput", VehicleParams[VehicleVectorParamId.AngularMoveToTargetMaxOutput]);
                        }
                        writer.WriteEndElement();
                    }
                }
                writer.WriteEndElement();
            }
        }
        #endregion

        #region XML Deserialization
        private static void PayPriceFromXml(ObjectPart part, ObjectGroup rootGroup, XmlTextReader reader)
        {
            int paypriceidx = 0;
            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidObjectXmlException();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name == "int")
                        {
                            int payprice = reader.ReadElementValueAsInt();
                            switch (paypriceidx++)
                            {
                                case 0:
                                    if (rootGroup != null)
                                    {
                                        rootGroup.PayPrice0 = payprice;
                                    }
                                    break;
                                case 1:
                                    if (rootGroup != null)
                                    {
                                        rootGroup.PayPrice1 = payprice;
                                    }
                                    break;
                                case 2:
                                    if (rootGroup != null)
                                    {
                                        rootGroup.PayPrice2 = payprice;
                                    }
                                    break;
                                case 3:
                                    if (rootGroup != null)
                                    {
                                        rootGroup.PayPrice3 = payprice;
                                    }
                                    break;
                                case 4:
                                    if (rootGroup != null)
                                    {
                                        rootGroup.PayPrice4 = payprice;
                                    }
                                    break;
                            }
                        }
                        else
                        {
                            reader.ReadToEndElement();
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "PayPrice")
                        {
                            throw new InvalidObjectXmlException();
                        }
                        return;
                }
            }
            throw new InvalidObjectXmlException();
        }

        private static void ShapeFromXml(ObjectPart part, ObjectGroup rootGroup, XmlTextReader reader)
        {
            var shape = new PrimitiveShape();
            bool have_attachpoint = false;

            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidObjectXmlException();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.IsEmptyElement)
                        {
                            break;
                        }
                        switch (reader.Name)
                        {
                            case "ProfileCurve":
                                shape.ProfileCurve = (byte)reader.ReadElementValueAsUInt();
                                break;

                            case "TextureEntry":
                                part.TextureEntryBytes = reader.ReadContentAsBase64();
                                break;

                            case "ExtraParams":
                                part.ExtraParamsBytes = reader.ReadContentAsBase64();
                                break;

                            case "PathBegin":
                                shape.PathBegin = (ushort)reader.ReadElementValueAsUInt();
                                break;

                            case "PathCurve":
                                shape.PathCurve = (byte)reader.ReadElementValueAsUInt();
                                break;

                            case "PathEnd":
                                shape.PathEnd = (ushort)reader.ReadElementValueAsUInt();
                                break;

                            case "PathRadiusOffset":
                                shape.PathRadiusOffset = (sbyte)reader.ReadElementValueAsInt();
                                break;

                            case "PathRevolutions":
                                shape.PathRevolutions = (byte)reader.ReadElementValueAsUInt();
                                break;

                            case "PathScaleX":
                                shape.PathScaleX = (byte)reader.ReadElementValueAsUInt();
                                break;

                            case "PathScaleY":
                                shape.PathScaleY = (byte)reader.ReadElementValueAsUInt();
                                break;

                            case "PathShearX":
                                shape.PathShearX = (byte)reader.ReadElementValueAsUInt();
                                break;

                            case "PathShearY":
                                shape.PathShearY = (byte)reader.ReadElementValueAsUInt();
                                break;

                            case "PathSkew":
                                shape.PathSkew = (sbyte)reader.ReadElementValueAsInt();
                                break;

                            case "PathTaperX":
                                shape.PathTaperX = (sbyte)reader.ReadElementValueAsInt();
                                break;

                            case "PathTaperY":
                                shape.PathTaperY = (sbyte)reader.ReadElementValueAsInt();
                                break;

                            case "PathTwist":
                                shape.PathTwist = (sbyte)reader.ReadElementValueAsInt();
                                break;

                            case "PathTwistBegin":
                                shape.PathTwistBegin = (sbyte)reader.ReadElementValueAsInt();
                                break;

                            case "PCode":
                                shape.PCode = (PrimitiveCode)reader.ReadElementValueAsUInt();
                                break;

                            case "ProfileBegin":
                                shape.ProfileBegin = (ushort)reader.ReadElementValueAsUInt();
                                break;

                            case "ProfileEnd":
                                shape.ProfileEnd = (ushort)reader.ReadElementValueAsUInt();
                                break;

                            case "ProfileHollow":
                                shape.ProfileHollow = (ushort)reader.ReadElementValueAsUInt();
                                break;

                            case "State":
                                shape.State = (byte)reader.ReadElementValueAsUInt();
                                break;

                            case "LastAttachPoint":
                                if (rootGroup != null)
                                {
                                    have_attachpoint = true;
                                    rootGroup.AttachPoint = (AttachmentPoint)reader.ReadElementValueAsUInt();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "ProfileShape":
                                {
                                    var p = (byte)reader.ReadContentAsEnumValue<PrimitiveProfileShape>();
                                    shape.ProfileCurve = (byte)((shape.ProfileCurve & (byte)0xF0) | p);
                                }
                                break;

                            case "HollowShape":
                                {
                                    var p = (byte)reader.ReadContentAsEnumValue<PrimitiveProfileHollowShape>();
                                    shape.ProfileCurve = (byte)((shape.ProfileCurve & (byte)0x0F) | p);
                                }
                                break;

                            case "SculptTexture":
                                shape.SculptMap = reader.ReadContentAsUUID();
                                break;

                            case "FlexiSoftness":
                                {
                                    var flexparam = part.Flexible;
                                    flexparam.Softness = reader.ReadElementValueAsInt();
                                    part.Flexible = flexparam;
                                }
                                break;

                            case "FlexiTension":
                                {
                                    var flexparam = part.Flexible;
                                    flexparam.Tension = reader.ReadElementValueAsDouble();
                                    part.Flexible = flexparam;
                                }
                                break;

                            case "FlexiDrag":
                                {
                                    var flexparam = part.Flexible;
                                    flexparam.Friction = reader.ReadElementValueAsDouble();
                                    part.Flexible = flexparam;
                                }
                                break;

                            case "FlexiGravity":
                                {
                                    var flexparam = part.Flexible;
                                    flexparam.Gravity = reader.ReadElementValueAsDouble();
                                    part.Flexible = flexparam;
                                }
                                break;

                            case "FlexiWind":
                                {
                                    var flexparam = part.Flexible;
                                    flexparam.Wind = reader.ReadElementValueAsDouble();
                                    part.Flexible = flexparam;
                                }
                                break;

                            case "FlexiForceX":
                                {
                                    var flexparam = part.Flexible;
                                    var v = flexparam.Force;
                                    v.X = reader.ReadElementValueAsDouble();
                                    flexparam.Force = v;
                                    part.Flexible = flexparam;
                                }
                                break;

                            case "FlexiForceY":
                                {
                                    var flexparam = part.Flexible;
                                    var v = flexparam.Force;
                                    v.Y = reader.ReadElementValueAsDouble();
                                    flexparam.Force = v;
                                    part.Flexible = flexparam;
                                }
                                break;

                            case "FlexiForceZ":
                                {
                                    var flexparam = part.Flexible;
                                    var v = flexparam.Force;
                                    v.Z = reader.ReadElementValueAsDouble();
                                    flexparam.Force = v;
                                    part.Flexible = flexparam;
                                }
                                break;

                            case "LightColorR":
                                {
                                    var lightparam = part.PointLight;
                                    var c = lightparam.LightColor;
                                    c.R = reader.ReadElementValueAsDouble().Clamp(0, 1);
                                    lightparam.LightColor = c;
                                    part.PointLight = lightparam;
                                }
                                break;

                            case "LightColorG":
                                {
                                    var lightparam = part.PointLight;
                                    var c = lightparam.LightColor;
                                    c.G = reader.ReadElementValueAsDouble().Clamp(0, 1);
                                    lightparam.LightColor = c;
                                    part.PointLight = lightparam;
                                }
                                break;

                            case "LightColorB":
                                {
                                    var lightparam = part.PointLight;
                                    var c = lightparam.LightColor;
                                    c.B = reader.ReadElementValueAsDouble().Clamp(0, 1);
                                    lightparam.LightColor = c;
                                    part.PointLight = lightparam;
                                }
                                break;

                            case "LightRadius":
                                {
                                    var lightparam = part.PointLight;
                                    lightparam.Radius = reader.ReadElementValueAsDouble();
                                    part.PointLight = lightparam;
                                }
                                break;

                            case "LightCutoff":
                                {
                                    var lightparam = part.PointLight;
                                    lightparam.Cutoff = reader.ReadElementValueAsDouble();
                                    part.PointLight = lightparam;
                                }
                                break;

                            case "LightFalloff":
                                {
                                    var lightparam = part.PointLight;
                                    lightparam.Falloff = reader.ReadElementValueAsDouble();
                                    part.PointLight = lightparam;
                                }
                                break;

                            case "LightIntensity":
                                {
                                    var lightparam = part.PointLight;
                                    lightparam.Intensity = reader.ReadElementValueAsDouble();
                                    part.PointLight = lightparam;
                                }
                                break;

                            case "FlexiEntry":
                                {
                                    var flexparam = part.Flexible;
                                    flexparam.IsFlexible = reader.ReadElementValueAsBoolean();
                                    part.Flexible = flexparam;
                                }
                                break;

                            case "LightEntry":
                                {
                                    var lightparam = part.PointLight;
                                    lightparam.IsLight = reader.ReadElementValueAsBoolean();
                                    part.PointLight = lightparam;
                                }
                                break;

                            case "SculptEntry":
                                reader.ReadElementValueAsBoolean();
                                break;

                            case "Media":
                                part.m_DefaultLocalization.RestoreMedia(PrimitiveMedia.FromXml(reader));
                                break;

                            case "PhysicsShapeType":
                                switch (reader.ReadElementValueAsString())
                                {
                                    case "Prim":
                                        part.PhysicsShapeType = PrimitivePhysicsShapeType.Prim;
                                        break;

                                    case "None":
                                        part.PhysicsShapeType = PrimitivePhysicsShapeType.None;
                                        break;

                                    case "ConvexHull":
                                        part.PhysicsShapeType = PrimitivePhysicsShapeType.Convex;
                                        break;
                                }
                                break;

                            case "SculptType":
                                shape.SculptType = (PrimitiveSculptType)reader.ReadElementValueAsUInt();
                                break;

                            default:
                                reader.ReadToEndElement();
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "Shape")
                        {
                            throw new InvalidObjectXmlException();
                        }
                        if (!have_attachpoint && rootGroup != null)
                        {
                            rootGroup.AttachPoint = (AttachmentPoint)shape.State;
                        }
                        /* fixup wrong parameters */
                        if (shape.SculptMap == UUID.Zero && shape.SculptType != PrimitiveSculptType.None)
                        {
                            shape.SculptType = PrimitiveSculptType.None;
                        }
                        part.Shape = shape;
                        return;

                    default:
                        break;
                }
            }
        }

        private static Map DynAttrsFromXml(XmlTextReader reader)
        {
            if (reader.IsEmptyElement)
            {
                return new Map();
            }
            var damap = LlsdXml.Deserialize(reader) as Map;
            if (damap != null)
            {
                foreach (var key in damap.Keys)
                {
                    if (!(damap[key] is Map))
                    {
                        /* remove everything that is not a map */
                        damap.Remove(key);
                    }
                }
            }

            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidObjectXmlException();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        reader.ReadToEndElement();
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "DynAttrs")
                        {
                            throw new InvalidObjectXmlException();
                        }
                        return damap;
                }
            }
        }

        private static PrimitiveFlags ReadPrimitiveFlagsEnum(XmlTextReader reader)
        {
            string value = reader.ReadElementValueAsString();
            if (value.Contains(" ") && !value.Contains(","))
            {
                value = value.Replace(" ", ", ");
            }
            string[] elems = value.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            var flags = PrimitiveFlags.None;
            foreach (string elem in elems)
            {
                switch (elem)
                {
                    case "Physics":
                        flags |= PrimitiveFlags.Physics;
                        break;

                    case "Money":
                        flags |= PrimitiveFlags.TakesMoney;
                        break;

                    case "Phantom":
                        flags |= PrimitiveFlags.Phantom;
                        break;

                    case "InventoryEmpty":
                        flags |= PrimitiveFlags.InventoryEmpty;
                        break;

                    case "JointWheel": /* yes, its name is a messed up naming in OpenSim object xml */
                        flags |= PrimitiveFlags.IncludeInSearch;
                        break;

                    case "AllowInventoryDrop":
                        flags |= PrimitiveFlags.AllowInventoryDrop;
                        break;

                    case "CameraDecoupled":
                        flags |= PrimitiveFlags.CameraDecoupled;
                        break;

                    case "AnimSource":
                        flags |= PrimitiveFlags.AnimSource;
                        break;

                    case "CameraSource":
                        flags |= PrimitiveFlags.CameraSource;
                        break;

                    case "TemporaryOnRez":
                    case "Temporary":
                        flags |= PrimitiveFlags.TemporaryOnRez;
                        break;
                }
            }

            return flags;
        }

        public static void LoadVehicleParams(XmlTextReader reader, ObjectPart part)
        {
            if (reader.IsEmptyElement)
            {
                return;
            }

            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidObjectXmlException();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.Name)
                        {
                            case "TYPE":
                                part.VehicleType = (VehicleType)reader.ReadElementValueAsInt();
                                break;

                            case "FLAGS":
                                part.VehicleFlags = (VehicleFlags)reader.ReadElementValueAsInt();
                                break;

                            case "LMDIR":
                                part.VehicleParams[VehicleVectorParamId.LinearMotorDirection] = reader.ReadElementChildsAsVector3();
                                break;

                            case "LMFTIME":
                                part.VehicleParams[VehicleVectorParamId.LinearFrictionTimescale] = reader.ReadElementChildsAsVector3();
                                break;

                            case "LMDTIME":
                                part.VehicleParams[VehicleVectorParamId.LinearMotorDecayTimescale] = new Vector3(reader.ReadElementValueAsDouble());
                                break;

                            case "LMTIME":
                                part.VehicleParams[VehicleVectorParamId.LinearMotorTimescale] = new Vector3(reader.ReadElementValueAsDouble());
                                break;

                            case "LMOFF":
                                part.VehicleParams[VehicleVectorParamId.LinearMotorOffset] = reader.ReadElementChildsAsVector3();
                                break;

                            case "LinearMotorDecayTimescaleVector":
                                part.VehicleParams[VehicleVectorParamId.LinearMotorDecayTimescale] = reader.ReadElementChildsAsVector3();
                                break;

                            case "LinearMotorTimescaleVector":
                                part.VehicleParams[VehicleVectorParamId.LinearMotorTimescale] = reader.ReadElementChildsAsVector3();
                                break;

                            case "LinearMotorAccelPosTimescaleVector":
                                part.VehicleParams[VehicleVectorParamId.LinearMotorAccelPosTimescale] = reader.ReadElementChildsAsVector3();
                                break;

                            case "LinearMotorDecelPosTimescaleVector":
                                part.VehicleParams[VehicleVectorParamId.LinearMotorDecelPosTimescale] = reader.ReadElementChildsAsVector3();
                                break;

                            case "LinearMotorAccelNegTimescaleVector":
                                part.VehicleParams[VehicleVectorParamId.LinearMotorAccelNegTimescale] = reader.ReadElementChildsAsVector3();
                                break;

                            case "LinearMotorDecelNegTimescaleVector":
                                part.VehicleParams[VehicleVectorParamId.LinearMotorDecelNegTimescale] = reader.ReadElementChildsAsVector3();
                                break;

                            case "AMDIR":
                                part.VehicleParams[VehicleVectorParamId.AngularMotorDirection] = reader.ReadElementChildsAsVector3();
                                break;

                            case "AMTIME":
                                part.VehicleParams[VehicleVectorParamId.AngularMotorTimescale] = new Vector3(reader.ReadElementValueAsDouble());
                                break;

                            case "AMDTIME":
                                part.VehicleParams[VehicleVectorParamId.AngularMotorDecayTimescale] = new Vector3(reader.ReadElementValueAsDouble());
                                break;

                            case "AMFTIME":
                                part.VehicleParams[VehicleVectorParamId.AngularFrictionTimescale] = reader.ReadElementChildsAsVector3();
                                break;

                            case "AngularMotorTimescaleVector":
                                part.VehicleParams[VehicleVectorParamId.AngularMotorTimescale] = reader.ReadElementChildsAsVector3();
                                break;

                            case "AngularMotorAccelPosTimescaleVector":
                                part.VehicleParams[VehicleVectorParamId.AngularMotorAccelPosTimescale] = reader.ReadElementChildsAsVector3();
                                break;

                            case "AngularMotorDecelPosTimescaleVector":
                                part.VehicleParams[VehicleVectorParamId.AngularMotorDecelPosTimescale] = reader.ReadElementChildsAsVector3();
                                break;

                            case "AngularMotorAccelNegTimescaleVector":
                                part.VehicleParams[VehicleVectorParamId.AngularMotorAccelNegTimescale] = reader.ReadElementChildsAsVector3();
                                break;

                            case "AngularMotorDecelNegTimescaleVector":
                                part.VehicleParams[VehicleVectorParamId.AngularMotorDecelNegTimescale] = reader.ReadElementChildsAsVector3();
                                break;

                            case "AngularMotorDecayTimescaleVector":
                                part.VehicleParams[VehicleVectorParamId.AngularMotorDecayTimescale] = reader.ReadElementChildsAsVector3();
                                break;

                            case "ADEFF":
                                part.VehicleParams[VehicleVectorParamId.AngularDeflectionEfficiency] = new Vector3(reader.ReadElementValueAsDouble());
                                break;

                            case "AngularDeflectionEfficiencyVector":
                                part.VehicleParams[VehicleVectorParamId.AngularDeflectionEfficiency] = reader.ReadElementChildsAsVector3();
                                break;

                            case "ADTIME":
                                part.VehicleParams[VehicleVectorParamId.AngularDeflectionTimescale] = new Vector3(reader.ReadElementValueAsDouble());
                                break;

                            case "AngularDeflectionTimescaleVector":
                                part.VehicleParams[VehicleVectorParamId.AngularDeflectionTimescale] = reader.ReadElementChildsAsVector3();
                                break;

                            case "LDEFF":
                                part.VehicleParams[VehicleVectorParamId.LinearDeflectionEfficiency] = new Vector3(reader.ReadElementValueAsDouble());
                                break;

                            case "LinearDeflectionEfficiencyVector":
                                part.VehicleParams[VehicleVectorParamId.LinearDeflectionEfficiency] = reader.ReadElementChildsAsVector3();
                                break;

                            case "LDTIME":
                                part.VehicleParams[VehicleVectorParamId.LinearDeflectionTimescale] = new Vector3(reader.ReadElementValueAsDouble());
                                break;

                            case "LinearDeflectionTimescaleVector":
                                part.VehicleParams[VehicleVectorParamId.LinearDeflectionTimescale] = reader.ReadElementChildsAsVector3();
                                break;

                            case "BEFF":
                                part.VehicleParams[VehicleFloatParamId.BankingEfficiency] = reader.ReadElementValueAsDouble();
                                break;

                            case "BMIX":
                                part.VehicleParams[VehicleFloatParamId.BankingMix] = reader.ReadElementValueAsDouble();
                                break;

                            case "BTIME":
                                part.VehicleParams[VehicleFloatParamId.BankingTimescale] = reader.ReadElementValueAsDouble();
                                break;

                            case "BankingAzimuth":
                                part.VehicleParams[VehicleFloatParamId.BankingAzimuth] = reader.ReadElementValueAsDouble();
                                break;

                            case "InvertedBankingModifier":
                                part.VehicleParams[VehicleFloatParamId.InvertedBankingModifier] = reader.ReadElementValueAsDouble();
                                break;

                            case "HHEI":
                                part.VehicleParams[VehicleFloatParamId.HoverHeight] = reader.ReadElementValueAsDouble();
                                break;

                            case "HEFF":
                                part.VehicleParams[VehicleFloatParamId.HoverEfficiency] = reader.ReadElementValueAsDouble();
                                break;

                            case "HTIME":
                                part.VehicleParams[VehicleFloatParamId.HoverTimescale] = reader.ReadElementValueAsDouble();
                                break;

                            case "VBUO":
                                part.VehicleParams[VehicleFloatParamId.Buoyancy] = reader.ReadElementValueAsDouble();
                                break;

                            case "VAEFF":
                                part.VehicleParams[VehicleVectorParamId.VerticalAttractionEfficiency] = new Vector3(reader.ReadElementValueAsDouble());
                                break;

                            case "VerticalAttractionEfficiencyVector":
                                part.VehicleParams[VehicleVectorParamId.VerticalAttractionEfficiency] = reader.ReadElementChildsAsVector3();
                                break;

                            case "VATIME":
                                part.VehicleParams[VehicleVectorParamId.VerticalAttractionTimescale] = new Vector3(reader.ReadElementValueAsDouble());
                                break;

                            case "VerticalAttractionTimescaleVector":
                                part.VehicleParams[VehicleVectorParamId.VerticalAttractionTimescale] = reader.ReadElementChildsAsVector3();
                                break;

                            case "REF_FRAME":
                                part.VehicleParams[VehicleRotationParamId.ReferenceFrame] = reader.ReadElementChildsAsQuaternion();
                                break;

                            case "LinearWindEfficiency":
                                part.VehicleParams[VehicleVectorParamId.LinearWindEfficiency] = reader.ReadElementChildsAsVector3();
                                break;

                            case "AngularWindEfficiency":
                                part.VehicleParams[VehicleVectorParamId.AngularWindEfficiency] = reader.ReadElementChildsAsVector3();
                                break;

                            case "LinearCurrentEfficiency":
                                part.VehicleParams[VehicleVectorParamId.LinearCurrentEfficiency] = reader.ReadElementChildsAsVector3();
                                break;

                            case "AngularCurrentEfficiency":
                                part.VehicleParams[VehicleVectorParamId.AngularCurrentEfficiency] = reader.ReadElementChildsAsVector3();
                                break;

                            case "MouselookAzimuth":
                                part.VehicleParams[VehicleFloatParamId.MouselookAzimuth] = reader.ReadElementValueAsDouble();
                                break;

                            case "MouselookAltitude":
                                part.VehicleParams[VehicleFloatParamId.MouselookAltitude] = reader.ReadElementValueAsDouble();
                                break;

                            case "DisableMotorsAbove":
                                part.VehicleParams[VehicleFloatParamId.DisableMotorsAbove] = reader.ReadElementValueAsDouble();
                                break;

                            case "DisableMotorsAfter":
                                part.VehicleParams[VehicleFloatParamId.DisableMotorsAfter] = reader.ReadElementValueAsDouble();
                                break;

                            case "LinearMoveToTargetEfficiency":
                                part.VehicleParams[VehicleVectorParamId.LinearMoveToTargetEfficiency] = reader.ReadElementChildsAsVector3();
                                break;

                            case "LinearMoveToTargetTimescale":
                                part.VehicleParams[VehicleVectorParamId.LinearMoveToTargetTimescale] = reader.ReadElementChildsAsVector3();
                                break;

                            case "LinearMoveToTargetEpsilon":
                                part.VehicleParams[VehicleVectorParamId.LinearMoveToTargetEpsilon] = reader.ReadElementChildsAsVector3();
                                break;

                            case "LinearMoveToTargetMaxOutput":
                                part.VehicleParams[VehicleVectorParamId.LinearMoveToTargetMaxOutput] = reader.ReadElementChildsAsVector3();
                                break;

                            case "AngularMoveToTargetEfficiency":
                                part.VehicleParams[VehicleVectorParamId.AngularMoveToTargetEfficiency] = reader.ReadElementChildsAsVector3();
                                break;

                            case "AngularMoveToTargetTimescale":
                                part.VehicleParams[VehicleVectorParamId.AngularMoveToTargetTimescale] = reader.ReadElementChildsAsVector3();
                                break;

                            case "AngularMoveToTargetEpsilon":
                                part.VehicleParams[VehicleVectorParamId.AngularMoveToTargetEpsilon] = reader.ReadElementChildsAsVector3();
                                break;

                            case "AngularMoveToTargetMaxOutput":
                                part.VehicleParams[VehicleVectorParamId.AngularMoveToTargetMaxOutput] = reader.ReadElementChildsAsVector3();
                                break;

                            default:
                                reader.ReadToEndElement();
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "Vehicle")
                        {
                            throw new InvalidObjectXmlException();
                        }
                        return;
                }
            }
        }

        public static ObjectPart FromXml(XmlTextReader reader, ObjectGroup rootGroup, UGUI currentOwner, XmlDeserializationOptions options)
        {
            var part = new ObjectPart
            {
                Owner = currentOwner
            };
            int InventorySerial = 1;
            bool IsVolumeDetect = false;
            bool isSitTargetActiveFound = false;

            if (reader.IsEmptyElement)
            {
                throw new InvalidObjectXmlException();
            }

            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidObjectXmlException();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.IsEmptyElement)
                        {
                            break;
                        }
                        switch (reader.Name)
                        {
                            case "PayPrice":
                                PayPriceFromXml(part, rootGroup, reader);
                                break;

                            case "AllowedDrop":
                                part.IsAllowedDrop = reader.ReadElementValueAsBoolean();
                                break;

                            case "AllowUnsit":
                                part.AllowUnsit = reader.ReadElementValueAsBoolean();
                                break;

                            case "ScriptedSitOnly":
                                part.IsScriptedSitOnly = reader.ReadElementValueAsBoolean();
                                break;

                            case "ForceMouselook": /* boolean field */
                                part.ForceMouselook = reader.ReadElementValueAsBoolean();
                                break;

                            case "Vehicle":
                                LoadVehicleParams(reader, part);
                                break;

                            case "RezzerID":
                                if ((options & XmlDeserializationOptions.RestoreIDs) != 0)
                                {
                                    /* only makes sense to restore when keeping the other object ids same */
                                    part.ObjectGroup.RezzingObjectID = reader.ReadContentAsUUID();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "CreatorID":
                                {
                                    var creator = part.Creator;
                                    creator.ID = reader.ReadContentAsUUID();
                                    part.Creator = creator;
                                }
                                break;

                            case "CreatorData":
                                try
                                {
                                    var creator = part.Creator;
                                    creator.CreatorData = reader.ReadElementValueAsString();
                                    part.Creator = creator;
                                }
                                catch
                                {
                                    /* if it fails, we have to skip it and leave it partial */
                                }
                                break;

                            case "InventorySerial":
                                InventorySerial = reader.ReadElementValueAsInt();
                                break;

                            case "TaskInventory":
                                part.Inventory.FillFromXml(reader, currentOwner, options);
                                break;

                            case "UUID":
                                if ((options & XmlDeserializationOptions.RestoreIDs) != 0)
                                {
                                    part.ID = reader.ReadContentAsUUID();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "LocalId":
                                /* unnecessary to use the LocalId */
                                part.LoadedLocalID = reader.ReadElementValueAsUInt();
                                break;

                            case "Name":
                                part.Name = reader.ReadElementValueAsString();
                                break;

                            case "Material":
                                part.Material = (PrimitiveMaterial)reader.ReadElementValueAsInt();
                                break;

                            case "VolumeDetectActive":
                                IsVolumeDetect = reader.ReadElementValueAsBoolean();
                                break;

                            case "Buoyancy":
                                part.Buoyancy = reader.ReadElementValueAsDouble();
                                break;

                            case "IsRotateXEnabled":
                                part.IsRotateXEnabled = reader.ReadElementValueAsBoolean();
                                break;

                            case "IsRotateYEnabled":
                                part.IsRotateYEnabled = reader.ReadElementValueAsBoolean();
                                break;

                            case "IsRotateZEnabled":
                                part.IsRotateZEnabled = reader.ReadElementValueAsBoolean();
                                break;

                            case "PassTouch":
                            case "PassTouches":
                                part.PassTouchMode = reader.ReadElementValueAsBoolean() ? PassEventMode.Always : PassEventMode.IfNotHandled;
                                break;

                            case "PassTouchMode":
                                switch(reader.ReadElementValueAsString().ToLower())
                                {
                                    case "ifnothandled":
                                        part.PassTouchMode = PassEventMode.IfNotHandled;
                                        break;

                                    case "always":
                                        part.PassTouchMode = PassEventMode.Always;
                                        break;

                                    case "never":
                                        part.PassTouchMode = PassEventMode.Never;
                                        break;
                                }
                                break;

                            case "PassCollisions":
                                part.PassCollisionMode = reader.ReadElementValueAsBoolean() ? PassEventMode.Always : PassEventMode.IfNotHandled;
                                break;

                            case "PassCollisionMode":
                                switch(reader.ReadElementValueAsString().ToLower())
                                {
                                    case "ifnothandled":
                                        part.PassCollisionMode = PassEventMode.IfNotHandled;
                                        break;

                                    case "always":
                                        part.PassCollisionMode = PassEventMode.Always;
                                        break;

                                    case "never":
                                        part.PassCollisionMode = PassEventMode.Never;
                                        break;
                                }
                                break;

                            case "ScriptAccessPin":
                                part.ScriptAccessPin = reader.ReadElementValueAsInt();
                                break;

                            case "GroupPosition":
                                /* needed in case of attachments */
                                if (rootGroup != null)
                                {
                                    rootGroup.AttachedPos = reader.ReadElementChildsAsVector3();
                                    part.LocalPosition = rootGroup.AttachedPos; /* needs to be restored for load oar */
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "CameraEyeOffset": /* extension, not defined in OpenSim format but in WhiteCore format */
                                part.CameraEyeOffset = reader.ReadElementChildsAsVector3();
                                break;

                            case "CameraAtOffset": /* extension, not defined in OpenSim format but in WhiteCore format */
                                part.CameraAtOffset = reader.ReadElementChildsAsVector3();
                                break;

                            case "OffsetPosition":
                                if (rootGroup == null)
                                {
                                    part.LocalPosition = reader.ReadElementChildsAsVector3();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "RotationOffset":
                                part.LocalRotation = reader.ReadElementChildsAsQuaternion();
                                if(rootGroup != null)
                                {
                                    rootGroup.AttachedRot = part.LocalRotation;
                                }
                                break;

                            case "Velocity":
                                part.Velocity = reader.ReadElementChildsAsVector3();
                                break;

                            case "RotationalVelocity":
                            case "AngularVelocity":
                                part.AngularVelocity = reader.ReadElementChildsAsVector3();
                                break;

                            case "SitTargetAvatar":
                                reader.ReadToEndElement();
                                break;

                            case "Acceleration":
                                part.Acceleration = reader.ReadElementChildsAsVector3();
                                break;

                            case "Description":
                                part.Description = reader.ReadElementValueAsString();
                                break;

                            case "Color":
                                {
                                    var tp = part.Text;
                                    tp.TextColor = reader.ReadElementChildsAsColorAlpha();
                                    part.Text = tp;
                                }
                                break;

                            case "Text":
                                {
                                    var tp = part.Text;
                                    tp.Text = reader.ReadElementValueAsString();
                                    part.Text = tp;
                                }
                                break;

                            case "SitName":
                                part.SitText = reader.ReadElementValueAsString();
                                break;

                            case "TouchName":
                                part.TouchText = reader.ReadElementValueAsString();
                                break;

                            case "LinkNum":
                                part.LoadedLinkNumber = reader.ReadElementValueAsInt();
                                break;

                            case "ClickAction":
                                part.ClickAction = (ClickActionType)reader.ReadElementValueAsUInt();
                                break;

                            case "Shape":
                                ShapeFromXml(part, rootGroup, reader);
                                break;

                            case "Scale":
                                part.Size = reader.ReadElementChildsAsVector3();
                                break;

                            case "SitTargetActive":
                                part.IsSitTargetActive = reader.ReadElementValueAsBoolean();
                                isSitTargetActiveFound = true;
                                break;

                            case "SitTargetOrientation":
                                part.SitTargetOrientation = reader.ReadElementChildsAsQuaternion();
                                if (!part.SitTargetOrientation.ApproxEquals(Quaternion.Identity, double.Epsilon) && !isSitTargetActiveFound)
                                {
                                    part.IsSitTargetActive = true;
                                }
                                break;

                            case "SitTargetPosition":
                                part.SitTargetOffset = reader.ReadElementChildsAsVector3();
                                if (!part.SitTargetOffset.ApproxEquals(Vector3.Zero, double.Epsilon) && !isSitTargetActiveFound)
                                {
                                    part.IsSitTargetActive = true;
                                }
                                break;

                            case "UnSitTargetActive":
                                part.IsUnSitTargetActive = reader.ReadElementValueAsBoolean();
                                break;

                            case "UnSitTargetOrientation":
                                part.UnSitTargetOrientation = reader.ReadElementChildsAsQuaternion();
                                break;

                            case "UnSitTargetPosition":
                                part.UnSitTargetOffset = reader.ReadElementChildsAsVector3();
                                break;

                            case "SitAnimation":
                                part.SitAnimation = reader.ReadElementValueAsString();
                                break;

                            case "ParentID":
                                reader.ReadToEndElement();
                                break;

                            case "CreationDate":
                                part.CreationDate = reader.ReadElementValueAsCrazyDate();
                                break;

                            case "RezDate":
                                part.RezDate = reader.ReadElementValueAsCrazyDate();
                                break;

                            case "Category":
                                if (rootGroup != null)
                                {
                                    rootGroup.Category = (UInt32)reader.ReadElementValueAsInt();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "SalePrice":
                                if (rootGroup != null)
                                {
                                    rootGroup.SalePrice = reader.ReadElementValueAsInt();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "ObjectSaleType":
                                if (rootGroup != null)
                                {
                                    rootGroup.SaleType = (InventoryItem.SaleInfoData.SaleType)reader.ReadElementValueAsUInt();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "OwnershipCost":
                                if (rootGroup != null)
                                {
                                    rootGroup.OwnershipCost = reader.ReadElementValueAsInt();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "GroupID":
                                if (rootGroup != null)
                                {
                                    rootGroup.Group.ID = reader.ReadContentAsUUID();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "LastOwnerID":
                                if (rootGroup != null)
                                {
                                    rootGroup.LastOwner.ID = reader.ReadContentAsUUID();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "BaseMask":
                                part.BaseMask = (InventoryPermissionsMask)reader.ReadElementValueAsUInt();
                                break;

                            case "OwnerMask":
                                part.OwnerMask = (InventoryPermissionsMask)reader.ReadElementValueAsUInt();
                                break;

                            case "GroupMask":
                                part.GroupMask = (InventoryPermissionsMask)reader.ReadElementValueAsUInt();
                                break;

                            case "EveryoneMask":
                                part.EveryoneMask = (InventoryPermissionsMask)reader.ReadElementValueAsUInt();
                                break;

                            case "NextOwnerMask":
                                part.NextOwnerMask = (InventoryPermissionsMask)reader.ReadElementValueAsUInt();
                                break;

                            case "Damage":
                                part.Damage = reader.ReadElementValueAsDouble();
                                break;

                            case "Flags":
                                part.Flags = ReadPrimitiveFlagsEnum(reader);
                                break;

                            case "ObjectFlags":
                                part.Flags = (PrimitiveFlags)reader.ReadElementValueAsUInt();
                                break;

                            case "CollisionSound":
                                {
                                    CollisionSoundParam sp = part.CollisionSound;
                                    sp.ImpactSound = reader.ReadContentAsUUID();
                                    part.CollisionSound = sp;
                                }
                                break;

                            case "CollisionSoundVolume":
                                {
                                    CollisionSoundParam sp = part.CollisionSound;
                                    sp.ImpactVolume = reader.ReadElementValueAsDouble().Clamp(0, 1);
                                    part.CollisionSound = sp;
                                }
                                break;

                            case "CollisionSoundRadius":
                                {
                                    CollisionSoundParam sp = part.CollisionSound;
                                    sp.ImpactSoundRadius = Math.Max(reader.ReadElementValueAsDouble(), 0);
                                    part.CollisionSound = sp;
                                }
                                break;

                            case "CollisionSoundUseHitpoint":
                                {
                                    CollisionSoundParam sp = part.CollisionSound;
                                    sp.ImpactUseHitpoint = reader.ReadElementValueAsBoolean();
                                    part.CollisionSound = sp;
                                }
                                break;

                            case "CollisionSoundUseChilds":
                                {
                                    CollisionSoundParam sp = part.CollisionSound;
                                    sp.ImpactUseChilds = reader.ReadElementValueAsBoolean();
                                    part.CollisionSound = sp;
                                }
                                break;

                            case "MediaUrl":
                                part.MediaURL = reader.ReadElementValueAsString();
                                break;

                            case "AttachedPos":
                            case "SavedAttachmentPos":
                                if (rootGroup != null)
                                {
                                    rootGroup.AttachedPos = reader.ReadElementChildsAsVector3();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "AttachedRot":
                                if(rootGroup != null)
                                {
                                    rootGroup.AttachedRot = reader.ReadElementChildsAsQuaternion();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "SavedAttachmentPoint":
                                rootGroup.AttachPoint = (AttachmentPoint)reader.ReadElementValueAsInt();
                                break;

                            case "TextureAnimation":
                                part.TextureAnimationBytes = reader.ReadContentAsBase64();
                                break;

                            case "ParticleSystem":
                                part.ParticleSystemBytes = reader.ReadContentAsBase64();
                                break;

                            case "PayPrice0":
                                if (rootGroup != null)
                                {
                                    rootGroup.PayPrice0 = reader.ReadElementValueAsInt();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "PayPrice1":
                                if (rootGroup != null)
                                {
                                    rootGroup.PayPrice1 = reader.ReadElementValueAsInt();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "PayPrice2":
                                if (rootGroup != null)
                                {
                                    rootGroup.PayPrice2 = reader.ReadElementValueAsInt();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "PayPrice3":
                                if (rootGroup != null)
                                {
                                    rootGroup.PayPrice3 = reader.ReadElementValueAsInt();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "PayPrice4":
                                if (rootGroup != null)
                                {
                                    rootGroup.PayPrice4 = reader.ReadElementValueAsInt();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "PhysicsShapeType":
                                part.PhysicsShapeType = (PrimitivePhysicsShapeType)reader.ReadElementValueAsInt();
                                break;

                            case "Density":
                                part.PhysicsDensity = reader.ReadElementValueAsDouble();
                                break;

                            case "Friction":
                                part.PhysicsFriction = reader.ReadElementValueAsDouble();
                                break;

                            case "Bounce":
                                part.PhysicsRestitution = reader.ReadElementValueAsDouble();
                                break;

                            case "GravityModifier":
                                part.PhysicsGravityMultiplier = reader.ReadElementValueAsDouble();
                                break;

                            case "DynAttrs":
                                part.m_DynAttrMap = DynAttrsFromXml(reader);
                                break;

                            case "Components":
                                {
                                    string json = reader.ReadElementValueAsString();
                                    try
                                    {
                                        if (!string.IsNullOrEmpty(json))
                                        {
                                            using (var ms = new MemoryStream(json.ToUTF8Bytes()))
                                            {
                                                var m = Json.Deserialize(ms) as Map;
                                                if (m != null)
                                                {
                                                    if (m.ContainsKey("SavedAttachedPos") && m["SavedAttachedPos"] is AnArray && rootGroup != null)
                                                    {
                                                        var a = (AnArray)(m["SavedAttachedPos"]);
                                                        if (a.Count == 3)
                                                        {
                                                            rootGroup.AttachedPos = new Vector3(a[0].AsReal, a[1].AsReal, a[2].AsReal);
                                                        }
                                                    }

                                                    if (m.ContainsKey("SavedAttachmentPoint") && rootGroup != null)
                                                    {
                                                        rootGroup.AttachPoint = (AttachmentPoint)(m["SavedAttachmentPoint"].AsInt);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        /* do not pass exceptions to caller */
                                    }
                                }
                                break;

                            case "RegionHandle": /* <= why was this ever serialized, it breaks partly the deduplication attempt */
                            case "OwnerID": /* <= do not trust this thing ever! */
                            case "FolderID":
                            case "UpdateFlag":
                            case "SitTargetOrientationLL":
                            case "SitTargetPositionLL":
                                reader.ReadToEndElement();
                                break;

                            case "SoundID":
                            case "Sound":
                                {
                                    SoundParam sp = part.Sound;
                                    sp.SoundID = reader.ReadContentAsUUID();
                                    part.Sound = sp;
                                }
                                break;

                            case "SoundGain":
                                {
                                    SoundParam sp = part.Sound;
                                    sp.Gain = reader.ReadElementValueAsFloat();
                                    part.Sound = sp;
                                }
                                break;

                            case "SoundFlags":
                                {
                                    SoundParam sp = part.Sound;
                                    sp.Flags = (PrimitiveSoundFlags)reader.ReadElementValueAsInt();
                                    part.Sound = sp;
                                }
                                break;

                            case "SoundRadius":
                                {
                                    SoundParam sp = part.Sound;
                                    sp.Radius = reader.ReadElementValueAsFloat();
                                    part.Sound = sp;
                                }
                                break;

                            case "SoundQueueing":
                                part.IsSoundQueueing = reader.ReadElementValueAsBoolean();
                                break;

                            case "Force":
                                reader.ReadToEndElement();
                                break;

                            case "Torque":
                                reader.ReadToEndElement();
                                break;

                            case "WalkableCoefficientAvatar":
                                part.WalkableCoefficientAvatar = reader.ReadElementValueAsDouble();
                                break;

                            case "WalkableCoefficientA":
                                part.WalkableCoefficientA = reader.ReadElementValueAsDouble();
                                break;

                            case "WalkableCoefficientB":
                                part.WalkableCoefficientB = reader.ReadElementValueAsDouble();
                                break;

                            case "WalkableCoefficientC":
                                part.WalkableCoefficientC = reader.ReadElementValueAsDouble();
                                break;

                            case "WalkableCoefficientD":
                                part.WalkableCoefficientD = reader.ReadElementValueAsDouble();
                                break;

                            case "LocalizationData":
                                {
                                    string localizationName = string.Empty;
                                    if (reader.MoveToFirstAttribute())
                                    {
                                        do
                                        {
                                            switch (reader.Name)
                                            {
                                                case "name":
                                                    localizationName = reader.Value;
                                                    break;

                                                default:
                                                    break;
                                            }
                                        }
                                        while (reader.MoveToNextAttribute());
                                    }
                                    if(!string.IsNullOrEmpty(localizationName))
                                    {
                                        part.GetOrCreateLocalization(localizationName).Serialization =
                                            Convert.FromBase64String(reader.ReadElementValueAsString("LocalizationData"));
                                    }
                                }
                                break;

                            default:
                                m_Log.DebugFormat("Unknown element {0} encountered in object xml", reader.Name);
                                reader.ReadToEndElement();
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "SceneObjectPart")
                        {
                            throw new InvalidObjectXmlException();
                        }
                        /* get rid of every flag, we do create internally */
                        if (rootGroup != null)
                        {
                            if ((part.Flags & PrimitiveFlags.TemporaryOnRez) != 0)
                            {
                                rootGroup.IsTemporary = true;
                            }
                        }

                        if ((part.Flags & PrimitiveFlags.Physics) != 0)
                        {
                            part.IsPhysics = true;
                        }

                        if ((part.Flags & PrimitiveFlags.VolumeDetect) != 0 || IsVolumeDetect)
                        {
                            part.IsVolumeDetect = true;
                        }

                        if (part.Inventory.CountScripts == 0)
                        {
                            part.Flags &= ~(PrimitiveFlags.Touch | PrimitiveFlags.TakesMoney);
                        }

                        part.Flags &= ~(
                            PrimitiveFlags.InventoryEmpty | PrimitiveFlags.Physics | PrimitiveFlags.Temporary | PrimitiveFlags.TemporaryOnRez |
                            PrimitiveFlags.AllowInventoryDrop | PrimitiveFlags.Scripted | PrimitiveFlags.VolumeDetect |
                            PrimitiveFlags.ObjectGroupOwned | PrimitiveFlags.ObjectYouOwner | PrimitiveFlags.ObjectOwnerModify);
                        part.Inventory.InventorySerial = InventorySerial;
                        return part;

                    default:
                        break;
                }
            }
        }
        #endregion
    }
}

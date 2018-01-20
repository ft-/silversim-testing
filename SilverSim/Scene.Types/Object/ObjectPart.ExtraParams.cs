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

using SilverSim.Scene.Types.Object.Parameters;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Primitive;
using System;
using System.Threading;

namespace SilverSim.Scene.Types.Object
{
    public partial class ObjectPart
    {
        private byte[] m_ExtraParamsBytes = new byte[0];
        readonly ReaderWriterLock m_ExtraParamsLock = new ReaderWriterLock();

        private bool m_IsFacelightDisabled;
        private bool m_IsAttachmentLightsDisabled;
        private double m_FacelightLimitIntensity = 1;
        private double m_AttachmentLightLimitIntensity = 1;

        public double FacelightLimitIntensity
        {
            get { return m_FacelightLimitIntensity; }

            set
            {
                lock (m_DataLock)
                {
                    if (value > 1)
                    {
                        value = 1;
                    }
                    else if (value < 0)
                    {
                        value = 0;
                    }
                    m_FacelightLimitIntensity = value;
                }
                if (PointLight.IsLight)
                {
                    UpdateExtraParams();
                    TriggerOnUpdate(0);
                }
            }
        }

        public double AttachmentLightLimitIntensity
        {
            get { return m_AttachmentLightLimitIntensity; }

            set
            {
                lock (m_DataLock)
                {
                    if (value > 1)
                    {
                        value = 1;
                    }
                    else if (value < 0)
                    {
                        value = 0;
                    }
                    m_AttachmentLightLimitIntensity = value;
                }
                if (PointLight.IsLight)
                {
                    UpdateExtraParams();
                    TriggerOnUpdate(0);
                }
            }
        }

        public bool IsFacelightDisabled
        {
            get { return m_IsFacelightDisabled; }

            set
            {
                lock (m_DataLock)
                {
                    m_IsFacelightDisabled = value;
                }
                if (PointLight.IsLight)
                {
                    UpdateExtraParams();
                    TriggerOnUpdate(0);
                }
            }
        }

        public bool IsAttachmentLightsDisabled
        {
            get { return m_IsAttachmentLightsDisabled; }

            set
            {
                lock (m_DataLock)
                {
                    m_IsAttachmentLightsDisabled = value;
                }
                if (PointLight.IsLight)
                {
                    UpdateExtraParams();
                    TriggerOnUpdate(0);
                }
            }
        }

        private readonly FlexibleParam m_Flexible = new FlexibleParam();

        private readonly PointLightParam m_PointLight = new PointLightParam();

        private readonly ProjectionParam m_Projection = new ProjectionParam();

        private readonly ExtendedMeshParams m_ExtendedMesh = new ExtendedMeshParams();


        private const ushort FlexiEP = 0x10;
        private const ushort LightEP = 0x20;
        private const ushort SculptEP = 0x30;
        private const ushort ProjectionEP = 0x40;
        private const ushort MeshEP = 0x60;
        private const ushort ExtendedMeshEP = 0x70;

        public byte[] ExtraParamsBytes
        {
            get
            {
                return m_ExtraParamsLock.AcquireReaderLock(() =>
                {
                    var b = new byte[m_ExtraParamsBytes.Length];
                    Buffer.BlockCopy(m_ExtraParamsBytes, 0, b, 0, m_ExtraParamsBytes.Length);
                    return b;
                });
            }
            set
            {
                m_ExtraParamsLock.AcquireWriterLock(() =>
                {
                    m_ExtraParamsBytes = value;
                    var light = new PointLightParam();
                    var proj = new ProjectionParam();
                    var flexi = new FlexibleParam();
                    var emesh = new ExtendedMeshParams();

                    flexi.IsFlexible = false;
                    proj.IsProjecting = false;
                    light.IsLight = false;
                    bool isSculpt = false;

                    if (value.Length < 1)
                    {
                        m_ExtraParamsBytes = new byte[1];
                        m_ExtraParamsBytes[0] = 0;
                        Flexible = flexi;
                        Projection = proj;
                        PointLight = light;
                        var shape = Shape;
                        if (shape.Type == PrimitiveShapeType.Sculpt)
                        {
                            shape.SculptType = PrimitiveSculptType.None;
                            Shape = shape;
                        }
                    }
                    else
                    {
                        int paramCount = value[0];
                        int pos = 1;
                        for (int paramIdx = 0; paramIdx < paramCount; ++paramIdx)
                        {
                            if (pos + 6 > value.Length)
                            {
                                break;
                            }
                            var type = (ushort)(value[pos] | (value[pos + 1] << 8));
                            var len = (UInt32)(value[pos + 2] | (value[pos + 3] << 8) | (value[pos + 4] << 16) | (value[pos + 5] << 24));
                            pos += 6;

                            if (pos + len > value.Length)
                            {
                                break;
                            }

                            switch (type)
                            {
                                case FlexiEP:
                                    if (len < 16)
                                    {
                                        break;
                                    }
                                    flexi.IsFlexible = true;
                                    flexi.Softness = ((value[pos] & 0x80) >> 6) | ((value[pos + 1] & 0x80) >> 7);
                                    flexi.Tension = (value[pos++] & 0x7F) / 10.0f;
                                    flexi.Friction = (value[pos++] & 0x7F) / 10.0f;
                                    flexi.Gravity = (value[pos++] / 10.0f) - 10.0f;
                                    flexi.Wind = value[pos++] / 10.0f;
                                    flexi.Force.FromBytes(value, pos);
                                    pos += 12;
                                    break;

                                case LightEP:
                                    if (len < 16)
                                    {
                                        break;
                                    }
                                    light.IsLight = true;
                                    light.LightColor.R_AsByte = value[pos++];
                                    light.LightColor.G_AsByte = value[pos++];
                                    light.LightColor.B_AsByte = value[pos++];
                                    light.Intensity = value[pos++] / 255f;
                                    light.Radius = ConversionMethods.LEBytes2Float(value, pos);
                                    pos += 4;
                                    light.Cutoff = ConversionMethods.LEBytes2Float(value, pos);
                                    pos += 4;
                                    light.Falloff = ConversionMethods.LEBytes2Float(value, pos);
                                    pos += 4;
                                    break;

                                case SculptEP:
                                    if (len < 17)
                                    {
                                        break;
                                    }
                                    lock (m_Shape)
                                    {
                                        m_Shape.SculptMap.FromBytes(value, pos);
                                        m_Shape.SculptType = (PrimitiveSculptType)value[pos + 16];
                                    }
                                    pos += 17;
                                    isSculpt = true;
                                    break;

                                case ProjectionEP:
                                    if (len < 28)
                                    {
                                        break;
                                    }
                                    proj.IsProjecting = true;
                                    proj.ProjectionTextureID.FromBytes(value, pos);
                                    pos += 16;
                                    proj.ProjectionFOV = ConversionMethods.LEBytes2Float(value, pos);
                                    pos += 4;
                                    proj.ProjectionFocus = ConversionMethods.LEBytes2Float(value, pos);
                                    pos += 4;
                                    proj.ProjectionAmbience = ConversionMethods.LEBytes2Float(value, pos);
                                    pos += 4;
                                    break;

                                case ExtendedMeshEP:
                                    if (len < 4)
                                    {
                                        break;
                                    }
                                    if (!BitConverter.IsLittleEndian)
                                    {
                                        byte[] b = new byte[4];
                                        Buffer.BlockCopy(value, pos, b, 0, 4);
                                        emesh.Flags = (ExtendedMeshParams.MeshFlags)BitConverter.ToUInt32(b, 0);
                                    }
                                    else
                                    {
                                        emesh.Flags = (ExtendedMeshParams.MeshFlags)BitConverter.ToUInt32(value, pos);
                                    }
                                    break;

                                default:
                                    break;
                            }
                        }
                    }

                    Projection = proj;
                    PointLight = light;
                    Flexible = flexi;
                    ExtendedMesh = emesh;

                    if (!isSculpt)
                    {
                        lock (m_Shape)
                        {
                            m_Shape.SculptType = PrimitiveSculptType.None;
                        }
                    }
                });
            }
        }

        bool IsPrivateAttachmentOrNone(AttachmentPoint ap)
        {
            switch(ap)
            {
                case AttachmentPoint.NotAttached:
                case AttachmentPoint.HudCenter2:
                case AttachmentPoint.HudTopRight:
                case AttachmentPoint.HudTopCenter:
                case AttachmentPoint.HudTopLeft:
                case AttachmentPoint.HudCenter1:
                case AttachmentPoint.HudBottomLeft:
                case AttachmentPoint.HudBottom:
                case AttachmentPoint.HudBottomRight:
                    return true;

                default:
                    return false;
            }
        }

        private void UpdateExtraParams() => m_ExtraParamsLock.AcquireWriterLock(() =>
        {
            int i = 0;
            uint totalBytesLength = 1;
            uint extraParamsNum = 0;

            var flexi = Flexible;
            var light = PointLight;
            var proj = Projection;
            var shape = Shape;
            var emesh = ExtendedMesh;
            bool isFlexible = flexi.IsFlexible;
            bool isSculpt = shape.Type == PrimitiveShapeType.Sculpt;
            bool isLight = light.IsLight &&
                (!m_IsAttachmentLightsDisabled || !IsPrivateAttachmentOrNone(ObjectGroup.AttachPoint)) &&
                (!m_IsFacelightDisabled || (ObjectGroup.AttachPoint != AttachmentPoint.LeftHand && ObjectGroup.AttachPoint != SilverSim.Types.Agent.AttachmentPoint.RightHand));
            bool isProjecting = proj.IsProjecting;
            ExtendedMeshParams.MeshFlags emeshFlags = emesh.Flags;
            if (isFlexible)
            {
                ++extraParamsNum;
                totalBytesLength += 16;
                totalBytesLength += 2 + 4;
            }

            if (isSculpt)
            {
                ++extraParamsNum;
                totalBytesLength += 17;
                totalBytesLength += 2 + 4;
            }

            if (isLight)
            {
                ++extraParamsNum;
                totalBytesLength += 16;
                totalBytesLength += 2 + 4;
            }

            if (isProjecting)
            {
                ++extraParamsNum;
                totalBytesLength += 28;
                totalBytesLength += 2 + 4;
            }

            if (emeshFlags != ExtendedMeshParams.MeshFlags.None)
            {
                ++extraParamsNum;
                totalBytesLength += 4;
                totalBytesLength += 2 + 4;
            }

            var updatebytes = new byte[totalBytesLength];
            updatebytes[i++] = (byte)extraParamsNum;

            if (isFlexible)
            {
                updatebytes[i++] = FlexiEP % 256;
                updatebytes[i++] = FlexiEP / 256;

                updatebytes[i++] = 16;
                updatebytes[i++] = 0;
                updatebytes[i++] = 0;
                updatebytes[i++] = 0;

                updatebytes[i++] = (byte)((byte)((byte)(flexi.Tension * 10.01f) & 0x7F) | (byte)((flexi.Softness & 2) << 6));
                updatebytes[i++] = (byte)((byte)((byte)(flexi.Friction * 10.01f) & 0x7F) | (byte)((flexi.Softness & 1) << 7));
                updatebytes[i++] = (byte)((flexi.Gravity + 10.0f) * 10.01f);
                updatebytes[i++] = (byte)(flexi.Wind * 10.01f);
                flexi.Force.ToBytes(updatebytes, i);
                i += 12;
            }

            if (isSculpt)
            {
                updatebytes[i++] = SculptEP % 256;
                updatebytes[i++] = SculptEP / 256;
                updatebytes[i++] = 17;
                updatebytes[i++] = 0;
                updatebytes[i++] = 0;
                updatebytes[i++] = 0;
                shape.SculptMap.ToBytes(updatebytes, i);
                i += 16;
                updatebytes[i++] = (byte)shape.SculptType;
            }

            if (isLight)
            {
                updatebytes[i++] = LightEP % 256;
                updatebytes[i++] = LightEP / 256;
                updatebytes[i++] = 16;
                updatebytes[i++] = 0;
                updatebytes[i++] = 0;
                updatebytes[i++] = 0;
                Buffer.BlockCopy(light.LightColor.AsByte, 0, updatebytes, i, 3);

                double intensity = light.Intensity;
                if (intensity > m_FacelightLimitIntensity &&
                    (ObjectGroup.AttachPoint == AttachmentPoint.LeftHand ||
                    ObjectGroup.AttachPoint == AttachmentPoint.RightHand))
                {
                    intensity = m_FacelightLimitIntensity;
                }
                else if (intensity > m_AttachmentLightLimitIntensity &&
                    !IsPrivateAttachmentOrNone(ObjectGroup.AttachPoint))
                {
                    intensity = m_AttachmentLightLimitIntensity;
                }

                updatebytes[i + 3] = (byte)(intensity * 255f);
                i += 4;
                ConversionMethods.Float2LEBytes((float)light.Radius, updatebytes, i);
                i += 4;
                ConversionMethods.Float2LEBytes((float)light.Cutoff, updatebytes, i);
                i += 4;
                ConversionMethods.Float2LEBytes((float)light.Falloff, updatebytes, i);
                i += 4;
            }

            if (isProjecting)
            {
                updatebytes[i++] = (ProjectionEP % 256);
                updatebytes[i++] = (ProjectionEP / 256);
                updatebytes[i++] = 28;
                updatebytes[i++] = 0;
                updatebytes[i++] = 0;
                updatebytes[i++] = 0;
                proj.ProjectionTextureID.ToBytes(updatebytes, i);
                i += 16;
                ConversionMethods.Float2LEBytes((float)proj.ProjectionFOV, updatebytes, i);
                i += 4;
                ConversionMethods.Float2LEBytes((float)proj.ProjectionFocus, updatebytes, i);
                i += 4;
                ConversionMethods.Float2LEBytes((float)proj.ProjectionAmbience, updatebytes, i);
            }

            if (emeshFlags != ExtendedMeshParams.MeshFlags.None)
            {
                updatebytes[i++] = (ExtendedMeshEP % 256);
                updatebytes[i++] = (ExtendedMeshEP / 256);
                updatebytes[i++] = 4;
                updatebytes[i++] = 0;
                updatebytes[i++] = 0;
                updatebytes[i++] = 0;
                updatebytes[i++] = (byte)(((uint)emeshFlags) & 0xFF);
                updatebytes[i++] = (byte)((((uint)emeshFlags) >> 8) & 0xFF);
                updatebytes[i++] = (byte)((((uint)emeshFlags) >> 16) & 0xFF);
                updatebytes[i++] = (byte)((((uint)emeshFlags) >> 24) & 0xFF);
            }

            m_ExtraParamsBytes = updatebytes;
        });

        public ExtendedMeshParams ExtendedMesh
        {
            get
            {
                var res = new ExtendedMeshParams();
                lock(m_ExtendedMesh)
                {
                    res.Flags = m_ExtendedMesh.Flags;
                }
                return res;
            }
            set
            {
                lock(m_ExtendedMesh)
                {
                    m_ExtendedMesh.Flags = value.Flags;
                }
                UpdateExtraParams();
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.Shape);
            }
        }

        public FlexibleParam Flexible
        {
            get
            {
                var res = new FlexibleParam();
                lock (m_Flexible)
                {
                    res.Force = m_Flexible.Force;
                    res.Friction = m_Flexible.Friction;
                    res.Gravity = m_Flexible.Gravity;
                    res.IsFlexible = m_Flexible.IsFlexible;
                    res.Softness = m_Flexible.Softness;
                    res.Tension = m_Flexible.Tension;
                    res.Wind = m_Flexible.Wind;
                }
                return res;
            }
            set
            {
                lock (m_Flexible)
                {
                    m_Flexible.Force = value.Force;
                    m_Flexible.Friction = value.Friction;
                    m_Flexible.Gravity = value.Gravity;
                    m_Flexible.IsFlexible = value.IsFlexible;
                    m_Flexible.Softness = value.Softness;
                    m_Flexible.Tension = value.Tension;
                    m_Flexible.Wind = value.Wind;
                }
                UpdateExtraParams();
                IsChanged = m_IsChangedEnabled;
                IncrementPhysicsShapeUpdateSerial();
                IncrementPhysicsParameterUpdateSerial();
                TriggerOnUpdate(UpdateChangedFlags.Shape);
            }
        }

        public ProjectionParam Projection
        {
            get
            {
                var res = new ProjectionParam();
                lock (m_Projection)
                {
                    res.IsProjecting = m_Projection.IsProjecting;
                    res.ProjectionTextureID = m_Projection.ProjectionTextureID;
                    res.ProjectionFocus = m_Projection.ProjectionFocus;
                    res.ProjectionFOV = m_Projection.ProjectionFOV;
                    res.ProjectionAmbience = m_Projection.ProjectionAmbience;
                }
                return res;
            }
            set
            {
                lock (m_Projection)
                {
                    m_Projection.IsProjecting = value.IsProjecting;
                    m_Projection.ProjectionTextureID = value.ProjectionTextureID;
                    m_Projection.ProjectionFocus = value.ProjectionFocus;
                    m_Projection.ProjectionFOV = value.ProjectionFOV;
                    m_Projection.ProjectionAmbience = value.ProjectionAmbience;
                }
                UpdateExtraParams();
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public PointLightParam PointLight
        {
            get
            {
                var res = new PointLightParam();
                lock (m_PointLight)
                {
                    res.Falloff = m_PointLight.Falloff;
                    res.Intensity = m_PointLight.Intensity;
                    res.IsLight = m_PointLight.IsLight;
                    res.LightColor = new Color(m_PointLight.LightColor);
                    res.Radius = m_PointLight.Radius;
                }
                return res;
            }
            set
            {
                lock (m_PointLight)
                {
                    m_PointLight.Falloff = value.Falloff;
                    m_PointLight.Intensity = value.Intensity;
                    m_PointLight.IsLight = value.IsLight;
                    m_PointLight.LightColor = new Color(value.LightColor);
                    m_PointLight.Radius = value.Radius;
                }
                UpdateExtraParams();
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }
    }
}

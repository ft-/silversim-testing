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
using SilverSim.Types.Agent;
using SilverSim.Types.Primitive;
using System;
using System.Threading;

namespace SilverSim.Scene.Types.Object.Localization
{
    public sealed partial class ObjectPartLocalizedInfo
    {
        private byte[] m_ExtraParamsBytes = new byte[0];
        private byte[] m_ExtraParamsBytesLimitedLight = new byte[0];
        private readonly object m_ExtraParamsLock = new object();

        private ProjectionParam m_Projection;

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
                return m_ExtraParamsBytes;
            }
        }

        public byte[] ExtraParamsBytesLimitedLight
        {
            get
            {
                return m_ExtraParamsBytesLimitedLight;
            }
        }

        private bool IsPrivateAttachmentOrNone(AttachmentPoint ap)
        {
            switch (ap)
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

        internal void UpdateExtraParams()
        {
            lock (m_ExtraParamsLock)
            {
                int i = 0;
                int limitedi = 0;
                uint totalBytesLength = 1;
                uint extraParamsNum = 0;

                uint limitedTotalBytesLength = 1;
                uint limitedExtraParamsNum = 0;

                var flexi = m_Part.Flexible;
                var light = m_Part.PointLight;
                var proj = Projection;
                var shape = m_Part.Shape;
                var emesh = m_Part.ExtendedMesh;
                bool isFlexible = flexi.IsFlexible;
                bool isSculpt = shape.Type == PrimitiveShapeType.Sculpt;
                ObjectGroup objectGroup = m_Part.ObjectGroup;
                bool isFullLight = light.IsLight;
                AttachmentPoint attachPoint = objectGroup.AttachPoint;
                bool isLimitedLight = isFullLight &&
                    (!m_Part.IsAttachmentLightsDisabled || !IsPrivateAttachmentOrNone(attachPoint)) &&
                    (!m_Part.IsFacelightDisabled || (attachPoint != AttachmentPoint.LeftHand && attachPoint != AttachmentPoint.RightHand)) &&
                    (!m_Part.IsUnattachedLightsDisabled || attachPoint != AttachmentPoint.NotAttached);
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

                limitedExtraParamsNum = extraParamsNum;
                limitedTotalBytesLength = totalBytesLength;

                if (isLimitedLight)
                {
                    ++limitedExtraParamsNum;
                    limitedTotalBytesLength += 16;
                    limitedTotalBytesLength += 2 + 4;
                }

                if (isFullLight)
                {
                    ++extraParamsNum;
                    totalBytesLength += 16;
                    totalBytesLength += 2 + 4;
                }

                var updatebytes = new byte[totalBytesLength];
                var updatebyteslimited = new byte[limitedTotalBytesLength];
                updatebyteslimited[limitedi++] = (byte)limitedExtraParamsNum;
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

                Buffer.BlockCopy(updatebytes, 1, updatebyteslimited, 1, i - 1);
                limitedi = i;

                if (isFullLight)
                {
                    updatebytes[i++] = LightEP % 256;
                    updatebytes[i++] = LightEP / 256;
                    updatebytes[i++] = 16;
                    updatebytes[i++] = 0;
                    updatebytes[i++] = 0;
                    updatebytes[i++] = 0;
                    Buffer.BlockCopy(light.LightColor.AsByte, 0, updatebytes, i, 3);

                    updatebytes[i + 3] = (byte)(light.Intensity * 255f);
                    i += 4;
                    ConversionMethods.Float2LEBytes((float)light.Radius, updatebytes, i);
                    i += 4;
                    ConversionMethods.Float2LEBytes((float)light.Cutoff, updatebytes, i);
                    i += 4;
                    ConversionMethods.Float2LEBytes((float)light.Falloff, updatebytes, i);
                    i += 4;
                }

                if (isLimitedLight)
                {
                    updatebyteslimited[limitedi++] = LightEP % 256;
                    updatebyteslimited[limitedi++] = LightEP / 256;
                    updatebyteslimited[limitedi++] = 16;
                    updatebyteslimited[limitedi++] = 0;
                    updatebyteslimited[limitedi++] = 0;
                    updatebyteslimited[limitedi++] = 0;
                    Buffer.BlockCopy(light.LightColor.AsByte, 0, updatebyteslimited, i, 3);

                    double intensity = light.Intensity;
                    double radius = light.Radius;

                    if(m_Part.ObjectGroup.AttachPoint == AttachmentPoint.NotAttached)
                    {
                        intensity = Math.Min(m_Part.UnattachedLightLimitIntensity, intensity);
                        radius = Math.Min(m_Part.UnattachedLightLimitRadius, radius);
                    }
                    else if (IsPrivateAttachmentOrNone(m_Part.ObjectGroup.AttachPoint))
                    {
                        /* skip these as they are anyways hidden from anyone else */
                    }
                    else
                    { 
                        if (m_Part.ObjectGroup.AttachPoint != AttachmentPoint.LeftHand &&
                            m_Part.ObjectGroup.AttachPoint != AttachmentPoint.RightHand)
                        {
                            intensity = Math.Min(m_Part.FacelightLimitIntensity, intensity);
                            radius = Math.Min(m_Part.FacelightLimitRadius, radius);
                        }
                        intensity = Math.Min(m_Part.AttachmentLightLimitIntensity, intensity);
                        radius = Math.Min(m_Part.AttachmentLightLimitRadius, radius);
                    }

                    updatebyteslimited[limitedi + 3] = (byte)(intensity * 255f);
                    limitedi += 4;
                    ConversionMethods.Float2LEBytes((float)light.Radius, updatebyteslimited, limitedi);
                    limitedi += 4;
                    ConversionMethods.Float2LEBytes((float)light.Cutoff, updatebyteslimited, limitedi);
                    limitedi += 4;
                    ConversionMethods.Float2LEBytes((float)light.Falloff, updatebyteslimited, limitedi);
                    limitedi += 4;
                }

                if (isProjecting)
                {
                    /* full block */
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

                    /* limited block */
                    updatebyteslimited[limitedi++] = (ProjectionEP % 256);
                    updatebyteslimited[limitedi++] = (ProjectionEP / 256);
                    updatebyteslimited[limitedi++] = 28;
                    updatebyteslimited[limitedi++] = 0;
                    updatebyteslimited[limitedi++] = 0;
                    updatebyteslimited[limitedi++] = 0;
                    proj.ProjectionTextureID.ToBytes(updatebyteslimited, limitedi);
                    limitedi += 16;
                    ConversionMethods.Float2LEBytes((float)proj.ProjectionFOV, updatebytes, limitedi);
                    limitedi += 4;
                    ConversionMethods.Float2LEBytes((float)proj.ProjectionFocus, updatebytes, limitedi);
                    limitedi += 4;
                    ConversionMethods.Float2LEBytes((float)proj.ProjectionAmbience, updatebytes, limitedi);
                }

                if (emeshFlags != ExtendedMeshParams.MeshFlags.None)
                {
                    updatebyteslimited[limitedi++] = (ExtendedMeshEP % 256);
                    updatebyteslimited[limitedi++] = (ExtendedMeshEP / 256);
                    updatebyteslimited[limitedi++] = 4;
                    updatebyteslimited[limitedi++] = 0;
                    updatebyteslimited[limitedi++] = 0;
                    updatebyteslimited[limitedi++] = 0;
                    updatebyteslimited[limitedi++] = (byte)(((uint)emeshFlags) & 0xFF);
                    updatebyteslimited[limitedi++] = (byte)((((uint)emeshFlags) >> 8) & 0xFF);
                    updatebyteslimited[limitedi++] = (byte)((((uint)emeshFlags) >> 16) & 0xFF);
                    updatebyteslimited[limitedi++] = (byte)((((uint)emeshFlags) >> 24) & 0xFF);
                }

                m_ExtraParamsBytes = updatebytes;
                m_ExtraParamsBytesLimitedLight = updatebyteslimited;
            }
        }

        public ProjectionParam Projection
        {
            get
            {
                ProjectionParam ip = m_Projection;
                if(ip == null)
                {
                    return m_ParentInfo.Projection;
                }
                else
                {
                    return new ProjectionParam(ip);
                }
            }
            set
            {
                bool changed;
                if (value == null)
                {
                    if (m_ParentInfo == null)
                    {
                        throw new InvalidOperationException();
                    }
                    changed = Interlocked.Exchange(ref m_Projection, null) != null;
                }
                else
                {
                    ProjectionParam oldParam = Interlocked.Exchange(ref m_Projection, new ProjectionParam(value));
                    changed = oldParam?.IsDifferent(value) ?? true;
                }
                if (changed)
                {
                    UpdateExtraParams();
                    m_Part.TriggerOnUpdate(0);
                }
            }
        }
    }
}

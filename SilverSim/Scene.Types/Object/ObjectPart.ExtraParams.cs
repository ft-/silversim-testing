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

using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using SilverSim.Types.Primitive;
using System;
using System.Threading;

namespace SilverSim.Scene.Types.Object
{
    public partial class ObjectPart
    {
        private byte[] m_ExtraParamsBytes = new byte[0];
        private ReaderWriterLock m_ExtraParamsLock = new ReaderWriterLock();

        private bool m_IsFacelightDisabled = false;
        private bool m_IsAttachmentLightsDisabled = false;

        public bool IsFacelightDisabled
        {
            get
            {
                return m_IsFacelightDisabled;
            }
            set
            {
                lock (this)
                {
                    m_IsFacelightDisabled = value;
                }
                UpdateExtraParams();
                TriggerOnUpdate(0);
            }
        }

        public bool IsAttachmentLightsDisabled
        {
            get
            {
                return m_IsAttachmentLightsDisabled;
            }
            set
            {
                lock (this)
                {
                    m_IsAttachmentLightsDisabled = value;
                }
                UpdateExtraParams();
                TriggerOnUpdate(0);
            }
        }

        public class FlexibleParam
        {
            #region Constructor
            public FlexibleParam()
            {

            }
            #endregion

            #region Fields
            public bool IsFlexible = false;
            public int Softness = 0;
            public double Gravity = 0;
            public double Friction = 0;
            public double Wind = 0;
            public double Tension = 0;
            public Vector3 Force = Vector3.Zero;
            #endregion
        }
        private readonly FlexibleParam m_Flexible = new FlexibleParam();
        public class PointLightParam
        {
            #region Constructor
            public PointLightParam()
            {

            }
            #endregion

            #region Fields
            public bool IsLight = false;
            public Color LightColor = new Color();
            public double Intensity = 0;
            public double Radius = 0;
            public double Cutoff = 0;
            public double Falloff = 0;
            #endregion
        }
        private readonly PointLightParam m_PointLight = new PointLightParam();

        public class ProjectionParam
        {
            #region Constructor
            public ProjectionParam()
            {

            }
            #endregion

            #region Fields
            public bool IsProjecting = false;
            public UUID ProjectionTextureID = UUID.Zero;
            public double ProjectionFOV;
            public double ProjectionFocus;
            public double ProjectionAmbience;
            #endregion
        }

        private readonly ProjectionParam m_Projection = new ProjectionParam();

        private void Float2LEBytes(float v, byte[] b, int offset)
        {
            byte[] i = BitConverter.GetBytes(v);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(i);
            }
            Buffer.BlockCopy(i, 0, b, offset, 4);
        }

        private float LEBytes2Float(byte[] b, int offset)
        {
            if (!BitConverter.IsLittleEndian)
            {
                byte[] i = new byte[4];
                Buffer.BlockCopy(b, offset, i, 0, 4);
                Array.Reverse(i);
                return BitConverter.ToSingle(i, 0);
            }
            else
            {
                return BitConverter.ToSingle(b, offset);
            }
        }

        public byte[] ExtraParamsBytes
        {
            get
            {
                m_ExtraParamsLock.AcquireReaderLock(-1);
                try
                {
                    byte[] b = new byte[m_ExtraParamsBytes.Length];
                    Buffer.BlockCopy(m_ExtraParamsBytes, 0, b, 0, m_ExtraParamsBytes.Length);
                    return b;
                }
                finally
                {
                    m_ExtraParamsLock.ReleaseReaderLock();
                }
            }
            set
            {
                m_ExtraParamsLock.AcquireWriterLock(-1);
                try
                {
                    m_ExtraParamsBytes = value;
                    PointLightParam light = new PointLightParam();
                    ProjectionParam proj = new ProjectionParam();
                    FlexibleParam flexi = new FlexibleParam();

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
                        PrimitiveShape shape = Shape;
                        if (shape.Type == PrimitiveShapeType.Sculpt)
                        {
                            shape.Type = PrimitiveShapeType.Sphere;
                            shape.SculptType = PrimitiveSculptType.Sphere;
                            Shape = shape;
                        }
                    }
                    else
                    {
                        const ushort FlexiEP = 0x10;
                        const ushort LightEP = 0x20;
                        const ushort SculptEP = 0x30;
                        const ushort ProjectionEP = 0x40;

                        int paramCount = value[0];
                        int pos = 0;
                        for (int paramIdx = 0; paramIdx < paramCount; ++paramIdx)
                        {
                            if (pos + 6 > value.Length)
                            {
                                break;
                            }
                            ushort type = (ushort)(value[pos] | (value[pos + 1] << 8));
                            UInt32 len = (UInt32)(value[pos + 2] | (value[pos + 3] << 8) | (value[pos + 4] << 16) | (value[pos + 5] << 24));
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
                                    flexi.Tension = (float)(value[pos++] & 0x7F) / 10.0f;
                                    flexi.Friction = (float)(value[pos++] & 0x7F) / 10.0f;
                                    flexi.Gravity = (float)(value[pos++] / 10.0f) - 10.0f;
                                    flexi.Wind = (float)value[pos++] / 10.0f;
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
                                    light.Radius = LEBytes2Float(value, pos);
                                    pos += 4;
                                    light.Cutoff = LEBytes2Float(value, pos);
                                    pos += 4;
                                    light.Falloff = LEBytes2Float(value, pos);
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
                                    proj.ProjectionFOV = LEBytes2Float(value, pos);
                                    pos += 4;
                                    proj.ProjectionFocus = LEBytes2Float(value, pos);
                                    pos += 4;
                                    proj.ProjectionAmbience = LEBytes2Float(value, pos);
                                    pos += 4;
                                    break;
                            }
                        }
                    }

                    Projection = proj;
                    PointLight = light;
                    Flexible = flexi;

                    if (!isSculpt)
                    {
                        lock (m_Shape)
                        {
                            if (m_Shape.SculptType < PrimitiveSculptType.Sphere || m_Shape.SculptType > PrimitiveSculptType.Sphere)
                            {
                                m_Shape.SculptType = PrimitiveSculptType.Sphere;
                                m_Shape.Type = PrimitiveShapeType.Sphere;
                            }
                        }
                    }
                }
                finally
                {
                    m_ExtraParamsLock.ReleaseWriterLock();
                }
            }
        }

        private void UpdateExtraParams()
        {
            const ushort FlexiEP = 0x10;
            const ushort LightEP = 0x20;
            const ushort SculptEP = 0x30;
            const ushort ProjectionEP = 0x40;

            m_ExtraParamsLock.AcquireWriterLock(-1);
            try
            {
                int i = 0;
                uint totalBytesLength = 1;
                uint extraParamsNum = 0;

                FlexibleParam flexi = Flexible;
                PointLightParam light = PointLight;
                ProjectionParam proj = Projection;
                PrimitiveShape shape = Shape;
                if (flexi.IsFlexible)
                {
                    ++extraParamsNum;
                    totalBytesLength += 16;
                    totalBytesLength += 2 + 4;
                }

                if (shape.Type == PrimitiveShapeType.Sculpt)
                {
                    ++extraParamsNum;
                    totalBytesLength += 17;
                    totalBytesLength += 2 + 4;
                }

                if (light.IsLight)
                {
                    ++extraParamsNum;
                    totalBytesLength += 16;
                    totalBytesLength += 2 + 4;
                }

                if (proj.IsProjecting)
                {
                    ++extraParamsNum;
                    totalBytesLength += 28;
                    totalBytesLength += 2 + 4;
                }

                byte[] updatebytes = new byte[totalBytesLength];
                updatebytes[i++] = (byte)extraParamsNum;

                if (flexi.IsFlexible)
                {
                    updatebytes[i++] = (byte)(FlexiEP % 256);
                    updatebytes[i++] = (byte)(FlexiEP / 256);

                    updatebytes[i++] = 16;
                    updatebytes[i++] = 0;
                    updatebytes[i++] = 0;
                    updatebytes[i++] = 0;

                    updatebytes[i++] = (byte)((byte)((byte)(flexi.Tension * 10.01f) & 0x7F) | (byte)((flexi.Softness & 2) << 6));
                    updatebytes[i++] = (byte)((byte)((byte)(flexi.Friction * 10.01f) & 0x7F) | (byte)((flexi.Softness & 1) << 7));
                    updatebytes[i++] = (byte)((flexi.Gravity + 10.0f) * 10.01f);
                    updatebytes[i++] = (byte)(flexi.Wind * 10.01f);
                    flexi.Force.ToBytes(updatebytes, i);
                    i += 16;
                }

                if (shape.Type == PrimitiveShapeType.Sculpt)
                {
                    updatebytes[i++] = (byte)(SculptEP % 256);
                    updatebytes[i++] = (byte)(SculptEP / 256);
                    updatebytes[i++] = 17;
                    updatebytes[i++] = 0;
                    updatebytes[i++] = 0;
                    updatebytes[i++] = 0;
                    shape.SculptMap.ToBytes(updatebytes, i);
                    i += 16;
                    updatebytes[i++] = (byte)shape.SculptType;
                }

                if (light.IsLight &&
                    (!m_IsAttachmentLightsDisabled || ObjectGroup.AttachPoint != SilverSim.Types.Agent.AttachmentPoint.NotAttached) &&
                    (!m_IsFacelightDisabled || (ObjectGroup.AttachPoint != SilverSim.Types.Agent.AttachmentPoint.LeftHand && ObjectGroup.AttachPoint != SilverSim.Types.Agent.AttachmentPoint.RightHand)))
                {
                    updatebytes[i++] = (byte)(LightEP % 256);
                    updatebytes[i++] = (byte)(LightEP / 256);
                    updatebytes[i++] = 16;
                    updatebytes[i++] = 0;
                    updatebytes[i++] = 0;
                    updatebytes[i++] = 0;
                    Buffer.BlockCopy(light.LightColor.AsByte, 0, updatebytes, i, 4);
                    updatebytes[i + 3] = (byte)(light.Intensity * 255f);
                    i += 4;
                    Float2LEBytes((float)light.Radius, updatebytes, i);
                    i += 4;
                    Float2LEBytes((float)light.Cutoff, updatebytes, i);
                    i += 4;
                    Float2LEBytes((float)light.Falloff, updatebytes, i);
                    i += 4;
                }

                if (proj.IsProjecting)
                {
                    updatebytes[i++] = (byte)(ProjectionEP % 256);
                    updatebytes[i++] = (byte)(ProjectionEP / 256);
                    updatebytes[i++] = 28;
                    updatebytes[i++] = 0;
                    updatebytes[i++] = 0;
                    updatebytes[i++] = 0;
                    proj.ProjectionTextureID.ToBytes(updatebytes, i);
                    i += 16;
                    Float2LEBytes((float)proj.ProjectionFOV, updatebytes, i);
                    i += 4;
                    Float2LEBytes((float)proj.ProjectionFocus, updatebytes, i);
                    i += 4;
                    Float2LEBytes((float)proj.ProjectionAmbience, updatebytes, i);
                    i += 4;
                }
            }
            finally
            {
                m_ExtraParamsLock.ReleaseWriterLock();
            }
        }


        public FlexibleParam Flexible
        {
            get
            {
                FlexibleParam res = new FlexibleParam();
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
                TriggerOnUpdate(ChangedEvent.ChangedFlags.Shape);
            }
        }

        public ProjectionParam Projection
        {
            get
            {
                ProjectionParam res = new ProjectionParam();
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
                PointLightParam res = new PointLightParam();
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

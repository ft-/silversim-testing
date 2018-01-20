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

using SilverSim.Types;
using System;

namespace SilverSim.Scene.Types.Object.Parameters
{
    public sealed class PointLightParam
    {
        #region Fields
        public bool IsLight;
        public Color LightColor = new Color();
        public double Intensity;
        public double Radius;
        public double Cutoff;
        public double Falloff;
        #endregion

        public static PointLightParam FromUdpDataBlock(byte[] value)
        {
            if (value.Length < 16)
            {
                return new PointLightParam();
            }

            return new PointLightParam
            {
                IsLight = true,
                LightColor = new Color { R_AsByte = value[0], G_AsByte = value[1], B_AsByte = value[2] },
                Intensity = value[3] / 255f,
                Radius = ConversionMethods.LEBytes2Float(value, 4),
                Cutoff = ConversionMethods.LEBytes2Float(value, 8),
                Falloff = ConversionMethods.LEBytes2Float(value, 12)
            };
        }

        public byte[] DbSerialization
        {
            get
            {
                var serialized = new byte[36];
                serialized[0] = IsLight ? (byte)1 : (byte)0;
                serialized[1] = LightColor.R_AsByte;
                serialized[2] = LightColor.G_AsByte;
                serialized[3] = LightColor.B_AsByte;
                Buffer.BlockCopy(BitConverter.GetBytes(Intensity), 0, serialized, 4, 8);
                Buffer.BlockCopy(BitConverter.GetBytes(Radius), 0, serialized, 12, 8);
                Buffer.BlockCopy(BitConverter.GetBytes(Cutoff), 0, serialized, 20, 8);
                Buffer.BlockCopy(BitConverter.GetBytes(Falloff), 0, serialized, 28, 8);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(serialized, 4, 8);
                    Array.Reverse(serialized, 12, 8);
                    Array.Reverse(serialized, 20, 8);
                    Array.Reverse(serialized, 28, 8);
                }
                return serialized;
            }
            set
            {
                if (value.Length != 36)
                {
                    throw new ArgumentException("Array length must be 36.");
                }
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(value, 4, 8);
                    Array.Reverse(value, 12, 8);
                    Array.Reverse(value, 20, 8);
                    Array.Reverse(value, 28, 8);
                }
                IsLight = value[0] != 0;
                LightColor.R_AsByte = value[1];
                LightColor.G_AsByte = value[2];
                LightColor.B_AsByte = value[3];
                Intensity = BitConverter.ToDouble(value, 4);
                Radius = BitConverter.ToDouble(value, 12);
                Cutoff = BitConverter.ToDouble(value, 20);
                Falloff = BitConverter.ToDouble(value, 28);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(value, 4, 8);
                    Array.Reverse(value, 12, 8);
                    Array.Reverse(value, 20, 8);
                    Array.Reverse(value, 28, 8);
                }

            }
        }
    }
}

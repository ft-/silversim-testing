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
    public sealed class ProjectionParam
    {
        #region Fields
        public bool IsProjecting;
        public UUID ProjectionTextureID = UUID.Zero;
        public double ProjectionFOV;
        public double ProjectionFocus;
        public double ProjectionAmbience;
        #endregion

        public ProjectionParam()
        {
        }

        public ProjectionParam(ProjectionParam src)
        {
            IsProjecting = src.IsProjecting;
            ProjectionTextureID = src.ProjectionTextureID;
            ProjectionFOV = src.ProjectionFOV;
            ProjectionFocus = src.ProjectionFocus;
            ProjectionAmbience = src.ProjectionAmbience;
        }

        public static ProjectionParam FromUdpDataBlock(byte[] value)
        {
            if (value.Length < 28)
            {
                return new ProjectionParam();
            }
            return new ProjectionParam
            {
                IsProjecting = true,
                ProjectionTextureID = new UUID(value, 0),
                ProjectionFOV = ConversionMethods.LEBytes2Float(value, 16),
                ProjectionFocus = ConversionMethods.LEBytes2Float(value, 20),
                ProjectionAmbience = ConversionMethods.LEBytes2Float(value, 24)
            };
        }

        public byte[] DbSerialization
        {
            get
            {
                var serialized = new byte[41];
                ProjectionTextureID.ToBytes(serialized, 0);
                Buffer.BlockCopy(BitConverter.GetBytes(ProjectionFOV), 0, serialized, 16, 8);
                Buffer.BlockCopy(BitConverter.GetBytes(ProjectionFocus), 0, serialized, 24, 8);
                Buffer.BlockCopy(BitConverter.GetBytes(ProjectionAmbience), 0, serialized, 32, 8);
                serialized[40] = IsProjecting ? (byte)1 : (byte)0;
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(serialized, 16, 8);
                    Array.Reverse(serialized, 24, 8);
                    Array.Reverse(serialized, 32, 8);
                }
                return serialized;
            }
            set
            {
                if (value.Length == 0)
                {
                    /* zero-length comes from migration */
                    IsProjecting = false;
                    return;
                }
                if (value.Length != 41)
                {
                    throw new ArgumentException("Array length must be 41.");
                }
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(value, 16, 8);
                    Array.Reverse(value, 24, 8);
                    Array.Reverse(value, 32, 8);
                }

                ProjectionTextureID.FromBytes(value, 0);
                ProjectionFOV = BitConverter.ToDouble(value, 16);
                ProjectionFocus = BitConverter.ToDouble(value, 24);
                ProjectionAmbience = BitConverter.ToDouble(value, 32);

                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(value, 16, 8);
                    Array.Reverse(value, 24, 8);
                    Array.Reverse(value, 32, 8);
                }
            }
        }
    }
}

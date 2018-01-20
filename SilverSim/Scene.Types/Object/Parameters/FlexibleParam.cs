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
    public sealed class FlexibleParam
    {
        #region Fields
        public bool IsFlexible;
        public int Softness;
        public double Gravity;
        public double Friction;
        public double Wind;
        public double Tension;
        public Vector3 Force = Vector3.Zero;
        #endregion

        public static FlexibleParam FromUdpDataBlock(byte[] value)
        {
            if (value.Length < 16)
            {
                return new FlexibleParam();
            }

            return new FlexibleParam
            {
                Softness = ((value[0] & 0x80) >> 6) | ((value[1] & 0x80) >> 7),
                Tension = (value[0] & 0x7F) / 10.0f,
                Friction = (value[1] & 0x7F) / 10.0f,
                Gravity = (value[2] / 10.0f) - 10.0f,
                Wind = value[3] / 10.0f,
                Force = new Vector3(value, 4)
            };
        }

        public byte[] DbSerialization
        {
            get
            {
                var serialized = new byte[49];
                Force.ToBytes(serialized, 0);
                Buffer.BlockCopy(BitConverter.GetBytes(Softness), 0, serialized, 12, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(Gravity), 0, serialized, 16, 8);
                Buffer.BlockCopy(BitConverter.GetBytes(Friction), 0, serialized, 24, 8);
                Buffer.BlockCopy(BitConverter.GetBytes(Wind), 0, serialized, 32, 8);
                Buffer.BlockCopy(BitConverter.GetBytes(Tension), 0, serialized, 40, 8);
                serialized[48] = IsFlexible ? (byte)1 : (byte)0;
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(serialized, 12, 4);
                    Array.Reverse(serialized, 16, 8);
                    Array.Reverse(serialized, 24, 8);
                    Array.Reverse(serialized, 32, 8);
                    Array.Reverse(serialized, 40, 8);
                }
                return serialized;
            }

            set
            {
                if (value.Length != 49)
                {
                    throw new ArgumentException("Array length must be 49.");
                }
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(value, 12, 4);
                    Array.Reverse(value, 16, 8);
                    Array.Reverse(value, 24, 8);
                    Array.Reverse(value, 32, 8);
                    Array.Reverse(value, 40, 8);
                }

                Force.FromBytes(value, 0);
                Softness = BitConverter.ToInt32(value, 12);
                Gravity = BitConverter.ToDouble(value, 16);
                Friction = BitConverter.ToDouble(value, 24);
                Wind = BitConverter.ToDouble(value, 32);
                Tension = BitConverter.ToDouble(value, 40);
                IsFlexible = value[48] != 0;

                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(value, 12, 4);
                    Array.Reverse(value, 16, 8);
                    Array.Reverse(value, 24, 8);
                    Array.Reverse(value, 32, 8);
                    Array.Reverse(value, 40, 8);
                }
            }
        }
    }
}

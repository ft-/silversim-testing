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
    public class CollisionSoundParam
    {
        #region Fields
        public UUID ImpactSound = UUID.Zero;
        public double ImpactVolume;
        #endregion

        public CollisionSoundParam()
        {
        }

        public CollisionSoundParam(CollisionSoundParam src)
        {
            ImpactSound = src.ImpactSound;
            ImpactVolume = src.ImpactVolume;
        }

        public byte[] Serialization
        {
            get
            {
                var serialized = new byte[24];
                ImpactSound.ToBytes(serialized, 0);
                Buffer.BlockCopy(BitConverter.GetBytes(ImpactVolume), 0, serialized, 16, 8);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(serialized, 16, 8);
                }
                return serialized;
            }

            set
            {
                if (value.Length != 24)
                {
                    throw new ArgumentException("Array length must be 24.");
                }
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(value, 16, 8);
                }
                ImpactSound.FromBytes(value, 0);
                ImpactVolume = BitConverter.ToDouble(value, 16);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(value, 16, 8);
                }
            }
        }
    }
}

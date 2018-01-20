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

using System;

namespace SilverSim.Scene.Types.Object.Parameters
{
    public sealed class ExtendedMeshParams
    {
        [Flags]
        public enum MeshFlags : uint
        {
            None = 0,
            AnimatedMeshEnabled = 1
        }

        #region Fields
        public MeshFlags Flags;
        #endregion

        public static ExtendedMeshParams FromUdpDataBlock(byte[] value)
        {
            if (value.Length < 4)
            {
                return new ExtendedMeshParams();
            }
            var p = new ExtendedMeshParams();
            if (!BitConverter.IsLittleEndian)
            {
                var b = new byte[4];
                Buffer.BlockCopy(value, 0, b, 0, 4);
                Array.Reverse(b);
                p.Flags = (MeshFlags)BitConverter.ToUInt32(b, 0);
            }
            else
            {
                p.Flags = (MeshFlags)BitConverter.ToUInt32(value, 0);
            }
            return p;
        }

        public byte[] DbSerialization
        {
            get
            {
                var serialized = new byte[4];
                Buffer.BlockCopy(BitConverter.GetBytes((uint)Flags), 0, serialized, 0, 4);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(serialized, 0, 4);
                }
                return serialized;
            }
            set
            {
                if (value.Length == 0)
                {
                    /* zero-length comes from migration */
                    Flags = MeshFlags.None;
                    return;
                }
                if (value.Length != 4)
                {
                    throw new ArgumentException("Array length must be 4.");
                }
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(value, 0, 4);
                }

                Flags = (MeshFlags)BitConverter.ToUInt32(value, 0);

                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(value, 0, 4);
                }
            }
        }
    }
}

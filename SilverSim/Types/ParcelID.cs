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

namespace SilverSim.Types
{
    public struct ParcelID
    {
        public GridVector Location;
        public uint RegionPosX;
        public uint RegionPosY;
        public uint RegionPosZ;

        public ParcelID(GridVector location, Vector3 pos)
        {
            Location = location;
            RegionPosX = (uint)pos.X;
            RegionPosY = (uint)pos.Y;
            RegionPosZ = (uint)Math.Ceiling(pos.Z).Clamp(0, 65535);
        }

        public ParcelID(byte[] data, int offset)
        {
            uint x;
            uint y;
            y = data[offset + 3];
            y = (y << 8) | data[offset + 2];
            y = (y << 8) | data[offset + 1];
            y = (y << 8) | data[offset + 0];
            x = data[offset + 7];
            x = (x << 8) | data[offset + 6];
            x = (x << 8) | data[offset + 5];
            x = (x << 8) | data[offset + 4];
            Location = new GridVector(x, y);
            RegionPosX = data[9];
            RegionPosX = (RegionPosX << 8) | data[8];
            RegionPosZ = data[11];
            RegionPosZ = (RegionPosZ << 8) | data[10];
            RegionPosY = data[13];
            RegionPosY = (RegionPosY << 8) | data[12];
        }

        public Vector3 RegionPos
        {
            get
            {
                return new Vector3(RegionPosX, RegionPosY, 0);
            }
        }

        public byte[] GetBytes()
        {
            uint x = Location.X;
            uint y = Location.Y;
            return new byte[]
            {
                (byte)y, (byte)(y >> 8), (byte)(y >> 16), (byte)(y >> 24),
                (byte)x, (byte)(x >> 8), (byte)(x >> 16), (byte)(x >> 24),
                (byte)RegionPosX, (byte)(RegionPosX >> 8), (byte)RegionPosZ, (byte)(RegionPosZ >> 8),
                (byte)RegionPosY, (byte)(RegionPosY >> 8), 0, 0
            };
        }
    }
}

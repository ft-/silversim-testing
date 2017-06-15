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
    public struct GridVector
    {
        public uint X; /* in m */
        public uint Y; /* in m */

        #region Constructors
        public GridVector(GridVector v)
        {
            X = v.X;
            Y = v.Y;
        }

        public GridVector(uint x, uint y)
        {
            X = x;
            Y = y;
        }

        public GridVector(string v)
        {
            var x = v.Split(new char[] { ',' });
            if(x.Length != 2)
            {
                throw new ArgumentException("v is not a valid GridVector string");
            }
            if(!uint.TryParse(x[0], out X) || !uint.TryParse(x[1], out Y))
            {
                throw new ArgumentException("v is not a valid GridVector string");
            }
        }

        public GridVector(string v, uint multiplier)
        {
            var x = v.Split(new char[] { ',' });
            if (x.Length != 2)
            {
                throw new ArgumentException("v is not a valid GridVector string");
            }
            if (!uint.TryParse(x[0], out X) || !uint.TryParse(x[1], out Y))
            {
                throw new ArgumentException("v is not a valid GridVector string");
            }
            X *= multiplier;
            Y *= multiplier;
        }

        public GridVector(ulong regionHandle)
        {
            X = (uint)(regionHandle >> 32);
            Y = (uint)(regionHandle & 0xFFFFFFFF);
        }
        #endregion

        #region Operators
        public static GridVector operator +(GridVector a, GridVector b) => new GridVector(a.X + b.X, a.Y + b.Y);

        public static Vector3 operator -(GridVector a, GridVector b) => new Vector3((double)a.X - (double)b.X, (double)a.Y - (double)b.Y, 0f);
        public static implicit operator Vector3(GridVector v) => new Vector3(v.X / 256f, v.Y / 256f, 0f);

        public static bool operator ==(GridVector a, GridVector b) => a.X == b.X && a.Y == b.Y;
        public static bool operator !=(GridVector a, GridVector b) => a.X != b.X || a.Y != b.Y;

        public override bool Equals(object o)
        {
            return (o is GridVector) ? this == (GridVector)o : false;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode();
        }

        #endregion

        public GridVector(byte[] data, int offset)
        {
            X = data[offset];
            X = (X << 8) | data[offset + 1];
            X = (X << 8) | data[offset + 2];
            X = (X << 8) | data[offset + 3];
            Y = data[offset + 4];
            Y = (Y << 8) | data[offset + 5];
            Y = (Y << 8) | data[offset + 6];
            Y = (Y << 8) | data[offset + 7];
        }

        #region Properties
        public byte[] AsBytes
        {
            get
            {
                ulong handle = RegionHandle;
                return new byte[]
                {
                    (byte)((handle >> 56) & 0xff),
                    (byte)((handle >> 48) & 0xff),
                    (byte)((handle >> 40) & 0xff),
                    (byte)((handle >> 32) & 0xff),
                    (byte)((handle >> 24) & 0xff),
                    (byte)((handle >> 16) & 0xff),
                    (byte)((handle >> 8) & 0xff),
                    (byte)(handle & 0xff)
                };
            }
        }

        public ulong RegionHandle
        {
            get { return (((ulong)X) << 32) | (ulong)Y; }

            set
            {
                Y = (uint)(value & 0xFFFFFFFF);
                X = (uint)(value >> 32);
            }
        }

        public int Length => (int)Math.Sqrt(X * X + Y * Y);

        public GridVector AlignToZoomlevel(int zoomlevel)
        {
            var zoomborder = (uint)(256 << (zoomlevel - 1));
            return new GridVector()
            {
                X = X - (X % zoomborder),
                Y = Y - (Y % zoomborder)
            };
        }

        public static GridVector Zero => new GridVector();

        public override string ToString() => String.Format("{0},{1}", X, Y);

        public string GridLocation => string.Format("{0},{1}", GridX, GridY);

        public ushort GridX
        {
            get { return (ushort)(X / 256); }
            set { X = (uint)value * 256; }
        }

        public ushort GridY
        {
            get { return (ushort)(Y / 256); }
            set { Y = (uint)value * 256; }
        }
        #endregion
    }
}

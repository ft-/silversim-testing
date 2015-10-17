// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
            string[] x = v.Split(new char[] { ',' });
            if(x.Length != 2)
            {
                throw new ArgumentException("v is not a valid GridVector string");
            }
            X = uint.Parse(x[0]);
            Y = uint.Parse(x[1]);
        }

        public GridVector(string v, uint multiplier)
        {
            string[] x = v.Split(new char[] { ',' });
            if (x.Length != 2)
            {
                throw new ArgumentException("v is not a valid GridVector string");
            }
            X = uint.Parse(x[0]) * multiplier;
            Y = uint.Parse(x[1]) * multiplier;
        }

        public GridVector(ulong regionHandle)
        {
            X = (uint)(regionHandle & 0xFFFFFFFF);
            Y = (uint)(regionHandle >> 32);
        }
        #endregion

        #region Operators
        public static GridVector operator+(GridVector a, GridVector b)
        {
            return new GridVector(a.X + b.X, a.Y + b.Y);
        }

        public static Vector3 operator-(GridVector a, GridVector b)
        {
            return new Vector3((double)a.X - (double)b.X, (double)a.Y - (double)b.Y, 0f);
        }
        public static implicit operator Vector3(GridVector v)
        {
            return new Vector3(v.X / 256f, v.Y / 256f, 0f);
        }
        #endregion

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
            get
            {
                ulong val = (ulong)Y;
                val <<= 32;
                return val | (ulong)X;
            }
            set
            {
                X = (uint)(value & 0xFFFFFFFF);
                Y = (uint)(value >> 32);
            }
        }

        public int Length
        {
            get
            {
                return (int)Math.Sqrt(X * X + Y * Y);
            }
        }

        public static GridVector Zero
        {
            get
            {
                return new GridVector();
            }
        }

        public new string ToString()
        {
            return String.Format("{0},{1}", X, Y);
        }

        public ushort GridX
        {
            get
            {
                return (ushort)(X / 256);
            }
            set
            {
                X = (uint)value * 256;
            }
        }

        public ushort GridY
        {
            get
            {
                return (ushort)(Y / 256);
            }
            set
            {
                Y = (uint)value * 256;
            }
        }
        #endregion
    }
}

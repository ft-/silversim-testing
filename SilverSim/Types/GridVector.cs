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
                throw new ArgumentException();
            }
            X = uint.Parse(x[0]);
            Y = uint.Parse(x[1]);
        }

        public GridVector(string v, uint multiplier)
        {
            string[] x = v.Split(new char[] { ',' });
            if (x.Length != 2)
            {
                throw new ArgumentException();
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

        public static GridVector operator-(GridVector a, GridVector b)
        {
            return new GridVector(a.X - b.X, a.Y - b.Y);
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
        }

        public ushort GridY
        {
            get
            {
                return (ushort)(Y / 256);
            }
        }
        #endregion
    }
}

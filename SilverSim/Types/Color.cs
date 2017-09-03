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
using System.Globalization;

namespace SilverSim.Types
{
    public struct Color : IEquatable<Color>
    {
        public double R;
        public double G;
        public double B;

        #region Constructors
        public static Color FromRgb(uint r, uint g, uint b)
        {
            return new Color(r / 255.0, g / 255.0, b / 255.0);
        }

        public Color(double r, double g, double b)
        {
            R = r.Clamp(0f, 1f);
            G = g.Clamp(0f, 1f);
            B = b.Clamp(0f, 1f);
        }

        public Color(Vector3 v)
        {
            R = v.X.Clamp(0f, 1f);
            G = v.Y.Clamp(0f, 1f);
            B = v.Z.Clamp(0f, 1f);
        }

        public Color(Color v)
        {
            R = v.R;
            G = v.G;
            B = v.B;
        }
        #endregion

        #region Operators
        public static implicit operator Vector3(Color v) => new Vector3(v.R, v.G, v.B);

        public static Color operator +(Color a, Color b) => new Color(
                (a.R + b.R).Clamp(0, 1),
                (a.G + b.G).Clamp(0, 1),
                (a.B + b.B).Clamp(0, 1));

        public Color Lerp(Color b, double mix) => new Color(
                R.Lerp(b.R, mix),
                G.Lerp(b.G, mix),
                B.Lerp(b.B, mix));

        public static Color operator *(Color a, Color b) => new Color(
                (a.R * b.R).Clamp(0, 1),
                (a.G * b.G).Clamp(0, 1),
                (a.B * b.B).Clamp(0, 1));

        public static Color operator *(Color a, double b) => new Color(
                (a.R * b).Clamp(0, 1),
                (a.G * b).Clamp(0, 1),
                (a.B * b).Clamp(0, 1));
        #endregion

        #region Properties
        public double GetR => R;
        public double GetG => G;
        public double GetB => B;
        public Vector3 AsVector3 => new Vector3(R, G, B);

        public byte R_AsByte
        {
            get
            {
                if (R > 1)
                {
                    return 255;
                }
                else if (R < 0)
                {
                    return 0;
                }
                else
                {
                    return (byte)(R * 255);
                }
            }

            set { R = value / 255f; }
        }

        public byte G_AsByte
        {
            get
            {
                if (G > 1)
                {
                    return 255;
                }
                else if (G < 0)
                {
                    return 0;
                }
                else
                {
                    return (byte)(G * 255);
                }
            }

            set { G = value / 255f; }
        }

        public byte B_AsByte
        {
            get
            {
                if (B > 1)
                {
                    return 255;
                }
                else if (B < 0)
                {
                    return 0;
                }
                else
                {
                    return (byte)(B * 255);
                }
            }

            set { B = value / 255f; }
        }

        public byte[] AsByte => new byte[] { R_AsByte, G_AsByte, B_AsByte };

        public override string ToString()
        {
            return string.Format("R={0},G={1},B={2}", R.ToString(CultureInfo.InvariantCulture), G.ToString(CultureInfo.InvariantCulture), B.ToString(CultureInfo.InvariantCulture));
        }


        public bool Equals(Color other)
        {
            return R == other.R && G == other.G && B == other.B;
        }

        public override int GetHashCode()
        {
            return R.GetHashCode() ^ G.GetHashCode() ^ B.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj is Color) ? Equals((Color)obj) : false;
        }

        public static bool operator ==(Color a, Color b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Color a, Color b)
        {
            return !a.Equals(b);
        }
        #endregion
    }
}

﻿// SilverSim is distributed under the terms of the
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
    public struct ColorAlpha : IEquatable<ColorAlpha>
    {
        public double R;
        public double G;
        public double B;
        public double A;

        public static Color FromRgb(uint r, uint g, uint b) => new Color(r / 255.0, g / 255.0, b / 255.0);

        public static ColorAlpha FromRgba(uint r, uint g, uint b, uint a) => new ColorAlpha(r / 255.0, g / 255.0, b / 255.0, a / 255.0);

        #region Constructors
        public ColorAlpha(double r, double g, double b, double alpha)
        {
            R = r.Clamp(0f, 1f);
            G = g.Clamp(0f, 1f);
            B = b.Clamp(0f, 1f);
            A = alpha.Clamp(0f, 1f);
        }

        public ColorAlpha(Vector3 v, double alpha)
        {
            R = v.X.Clamp(0f, 1f);
            G = v.Y.Clamp(0f, 1f);
            B = v.Z.Clamp(0f, 1f);
            A = alpha.Clamp(0f, 1f);
        }

        public ColorAlpha(Color v, double alpha)
        {
            R = v.R;
            G = v.G;
            B = v.B;
            A = alpha.Clamp(0f, 1f);
        }

        public ColorAlpha(Color v)
        {
            R = v.R;
            G = v.G;
            B = v.B;
            A = 1;
        }

        public ColorAlpha(ColorAlpha v)
        {
            R = v.R;
            G = v.G;
            B = v.B;
            A = v.A;
        }

        public ColorAlpha(byte[] b)
        {
            R = b[0] / 255f;
            G = b[1] / 255f;
            B = b[2] / 255f;
            A = b[3] / 255f;
        }

        public ColorAlpha(byte[] b, int pos)
        {
            R = b[pos + 0] / 255f;
            G = b[pos + 1] / 255f;
            B = b[pos + 2] / 255f;
            A = b[pos + 3] / 255f;
        }
        #endregion

        #region Operators
        public static implicit operator Vector3(ColorAlpha v) => new Vector3(v.R, v.G, v.B);

        public static ColorAlpha operator +(ColorAlpha a, ColorAlpha b) => new ColorAlpha(
                (a.R + b.R).Clamp(0, 1),
                (a.G + b.G).Clamp(0, 1),
                (a.B + b.B).Clamp(0, 1),
                (a.A + b.A).Clamp(0, 1));

        public ColorAlpha Lerp(ColorAlpha b, double mix) => new ColorAlpha(
                R.Lerp(b.R, mix),
                G.Lerp(b.G, mix),
                B.Lerp(b.B, mix),
                A.Lerp(b.A, mix));

        public static ColorAlpha operator *(ColorAlpha a, ColorAlpha b) => new ColorAlpha(
                (a.R * b.R).Clamp(0, 1),
                (a.G * b.G).Clamp(0, 1),
                (a.B * b.B).Clamp(0, 1),
                (a.A * b.A).Clamp(0, 1));

        public static ColorAlpha operator *(ColorAlpha a, double b) => new ColorAlpha(
                (a.R * b).Clamp(0, 1),
                (a.G * b).Clamp(0, 1),
                (a.B * b).Clamp(0, 1),
                (a.A * b).Clamp(0, 1));
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

        public byte A_AsByte
        {
            get
            {
                if (A > 1)
                {
                    return 255;
                }
                else if (A < 0)
                {
                    return 0;
                }
                else
                {
                    return (byte)(A * 255);
                }
            }

            set { A = value / 255f; }
        }

        public static explicit operator ColorAlpha(Color c) => new ColorAlpha(c);
        public static implicit operator Color(ColorAlpha c) => new Color(c.R, c.G, c.B);

        public byte[] AsByte => new byte[] { R_AsByte, G_AsByte, B_AsByte, A_AsByte };

        public override string ToString() => string.Format("R={0},G={1},B={2},A={3}", R.ToString(CultureInfo.InvariantCulture), G.ToString(CultureInfo.InvariantCulture), B.ToString(CultureInfo.InvariantCulture), A.ToString(CultureInfo.InvariantCulture));

        public bool Equals(ColorAlpha other) => R == other.R && G == other.G && B == other.B && A == other.A;

        public override int GetHashCode() => R.GetHashCode() ^ G.GetHashCode() ^ B.GetHashCode() ^ A.GetHashCode();

        public override bool Equals(object obj) => (obj is ColorAlpha) && Equals((ColorAlpha)obj);

        public static bool operator ==(ColorAlpha a, ColorAlpha b) => a.Equals(b);

        public static bool operator !=(ColorAlpha a, ColorAlpha b) => !a.Equals(b);
        #endregion

        /// <summary>A Color4 with zero RGB values and fully opaque (alpha 1.0)</summary>
        public static readonly ColorAlpha Black = new ColorAlpha(0f, 0f, 0f, 1f);

        /// <summary>A Color4 with full RGB values (1.0) and fully opaque (alpha 1.0)</summary>
        public static readonly ColorAlpha White = new ColorAlpha(1f, 1f, 1f, 1f);
    }
}

// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.Scene.Types
{
    public struct Angle : IEquatable<Angle>, IComparable<Angle>
    {
        #region Constructor
        public Angle(Angle v)
        {
            Radians = v.Radians;
        }

        private Angle(double v)
        {
            Radians = v;
        }
        #endregion

        #region Properties
        public double Degrees
        {
            get
            {
                return Radians * 180f / Math.PI;
            }
            set
            {
                Radians = value * Math.PI / 180f;
            }
        }

        public double Radians;
        #endregion

        #region Operators
        public static Angle operator+(Angle a, Angle b)
        {
            Angle o;
            o.Radians = a.Radians + b.Radians;
            return o;
        }

        public static Angle operator -(Angle a, Angle b)
        {
            Angle o;
            o.Radians = a.Radians - b.Radians;
            return o;
        }

        public static bool operator==(Angle a, Angle b)
        {
            return a.Equals(b);
        }

        public static bool operator!=(Angle a, Angle b)
        {
            return !a.Equals(b);
        }

        public static bool operator>(Angle a, Angle b)
        {
            return a.Radians > b.Radians;
        }

        public static bool operator <(Angle a, Angle b)
        {
            return a.Radians < b.Radians;
        }
        #endregion

        public int CompareTo(Angle a)
        {
            return Radians.CompareTo(a.Radians);
        }

        public bool Equals(Angle a)
        {
            return Radians.Equals(a.Radians);
        }

        public override bool Equals(object a)
        {
            if (a is Angle)
            {
                return Radians.Equals((Angle)a);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Radians.GetHashCode();
        }

        public static readonly Angle Zero = new Angle(0f);
    }
}

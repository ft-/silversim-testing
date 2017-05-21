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
            get { return Radians * 180f / Math.PI; }

            set { Radians = value * Math.PI / 180f; }
        }

        public double Radians;
        #endregion

        #region Operators
        public static Angle operator +(Angle a, Angle b) => new Angle(a.Radians + b.Radians);

        public static Angle operator -(Angle a, Angle b) => new Angle(a.Radians - b.Radians);

        public static bool operator ==(Angle a, Angle b) => a.Equals(b);

        public static bool operator !=(Angle a, Angle b) => !a.Equals(b);

        public static bool operator >(Angle a, Angle b) => a.Radians > b.Radians;

        public static bool operator <(Angle a, Angle b) => a.Radians < b.Radians;
        #endregion

        public int CompareTo(Angle a) => Radians.CompareTo(a.Radians);

        public bool Equals(Angle a) => Radians.Equals(a.Radians);

        public override bool Equals(object a) => (a is Angle) ? Radians.Equals((Angle)a) : false;

        public override int GetHashCode() => Radians.GetHashCode();

        public static readonly Angle Zero = new Angle(0f);
    }
}

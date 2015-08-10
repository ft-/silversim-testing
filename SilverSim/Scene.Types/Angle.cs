// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.Scene.Types
{
    public struct Angle : IEquatable<Angle>, IComparable<Angle>
    {
        private double m_Value;

        #region Constructor
        public Angle(Angle v)
        {
            m_Value = v.m_Value;
        }

        private Angle(double v)
        {
            m_Value = v;
        }
        #endregion

        #region Properties
        public double Degrees
        {
            get
            {
                return m_Value * 180f / Math.PI;
            }
            set
            {
                m_Value = value * Math.PI / 180f;
            }
        }

        public double Radians
        {
            get
            {
                return m_Value;
            }
            set
            {
                m_Value = value;
            }
        }
        #endregion

        #region Operators
        public static Angle operator+(Angle a, Angle b)
        {
            Angle o;
            o.m_Value = a.m_Value + b.m_Value;
            return o;
        }

        public static Angle operator -(Angle a, Angle b)
        {
            Angle o;
            o.m_Value = a.m_Value - b.m_Value;
            return o;
        }
        #endregion

        public int CompareTo(Angle a)
        {
            return m_Value.CompareTo(a.m_Value);
        }

        public bool Equals(Angle a)
        {
            return m_Value.Equals(a.m_Value);
        }

        public static readonly Angle Zero = new Angle(0f);
    }
}

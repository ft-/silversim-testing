/*
 * ArribaSim is distributed under the terms of the
 * GNU General Public License v2 
 * with the following clarification and special exception.
 * 
 * Linking this code statically or dynamically with other modules is
 * making a combined work based on this code. Thus, the terms and
 * conditions of the GNU General Public License cover the whole
 * combination.
 * 
 * As a special exception, the copyright holders of this code give you
 * permission to link this code with independent modules to produce an
 * executable, regardless of the license terms of these independent
 * modules, and to copy and distribute the resulting executable under
 * terms of your choice, provided that you also meet, for each linked
 * independent module, the terms and conditions of the license of that
 * module. An independent module is a module which is not derived from
 * or based on this code. If you modify this code, you may extend
 * this exception to your version of the code, but you are not
 * obligated to do so. If you do not wish to do so, delete this
 * exception statement from your version.
 * 
 * License text is derived from GNU classpath text
 */

using System;

namespace ArribaSim.Scene.Types
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

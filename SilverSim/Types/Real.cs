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
using System.Globalization;

namespace SilverSim.Types
{
    [Serializable]
    public sealed class Real : IComparable<Real>, IEquatable<Real>, IValue
    {
        private double m_Value;

        public Real()
        {
            m_Value = 0f;
        }

        #region Properties
        public ValueType Type
        {
            get
            {
                return ValueType.Real;
            }
        }

        public LSLValueType LSL_Type
        {
            get
            {
                return LSLValueType.Float;
            }
        }
        #endregion Properties

        public Real(float val)
        {
            m_Value = val;
        }

        public Real(double val)
        {
            m_Value = val;
        }

        public int CompareTo(Real v)
        {
            return m_Value.CompareTo(v.m_Value);
        }

        public bool Equals(Real v)
        {
            return m_Value.Equals(v.m_Value);
        }

        public override bool Equals(object v)
        {
            if (typeof(IValue).IsAssignableFrom(v.GetType()))
            {
                return Equals(((IValue)v).AsReal);
            }
            return false;
        }

        public override string ToString()
        {
            return string.Format(EnUsCulture, "{0}", m_Value);
        }

        #region Serialization
        public void FromBytes(byte[] byteArray, int pos, bool normalized)
        {
            if (!BitConverter.IsLittleEndian)
            {
                // Big endian architecture
                byte[] conversionBuffer = new byte[8];

                Buffer.BlockCopy(byteArray, pos, conversionBuffer, 0, 8);

                System.Array.Reverse(conversionBuffer, 0, 8);

                m_Value = BitConverter.ToSingle(conversionBuffer, 0);
            }
            else
            {
                // Little endian architecture
                m_Value = BitConverter.ToSingle(byteArray, pos);
            }
        }

        public void ToBytes(byte[] dest, int pos)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(m_Value), 0, dest, pos + 0, 8);

            if (!BitConverter.IsLittleEndian)
            {
                System.Array.Reverse(dest, pos + 0, 8);
            }
        }
        #endregion Serialization


        #region Operators
        public static Real operator -(Real v)
        {
            return new Real(-v.m_Value);
        }

        public static Real operator +(Real a, Real b)
        {
            return new Real(a.m_Value + b.m_Value);
        }

        public static Real operator -(Real a, Real b)
        {
            return new Real(a.m_Value - b.m_Value);
        }

        public static Real operator *(Real a, Real b)
        {
            return new Real(a.m_Value * b.m_Value);
        }

        public static Real operator /(Real a, Real b)
        {
            return new Real(a.m_Value / b.m_Value);
        }

        public static Real operator %(Real a, Real b)
        {
            return new Real(a.m_Value % b.m_Value);
        }

        public static explicit operator Integer(Real v)
        {
            return new Integer((Int32)v.m_Value);
        }

        public static explicit operator AString(Real v)
        {
            return new AString(v.m_Value.ToString());
        }

        public static explicit operator string(Real v)
        {
            return v.m_Value.ToString();
        }

        public static implicit operator double(Real v)
        {
            return v.m_Value;
        }
        public static bool operator ==(Real a, Real b)
        {
            return a.m_Value == b.m_Value;
        }

        public static bool operator !=(Real a, Real b)
        {
            return a.m_Value != b.m_Value;
        }

        public static bool operator <(Real a, Real b)
        {
            return a.m_Value < b.m_Value;
        }

        public static bool operator >(Real a, Real b)
        {
            return a.m_Value > b.m_Value;
        }

        public static bool operator <=(Real a, Real b)
        {
            return a.m_Value <= b.m_Value;
        }

        public static bool operator >=(Real a, Real b)
        {
            return a.m_Value >= b.m_Value;
        }

        public override int GetHashCode()
        {
            return (int)m_Value;
        }
        #endregion Operators

        public static Real Parse(string v)
        {
            return new Real(Double.Parse(v.Trim(), EnUsCulture));
        }

        #region Helpers
        public ABoolean AsBoolean { get { return new ABoolean(Math.Abs(m_Value) >= Single.Epsilon); } }
        public Integer AsInteger { get { return new Integer((int)m_Value); } }
        public Quaternion AsQuaternion { get { return new Quaternion(0, 0, 0, m_Value); } }
        public Real AsReal { get { return new Real(m_Value); } }
        public AString AsString { get { return new AString(ToString()); } }
        public UUID AsUUID { get { return new UUID(); } }
        public Vector3 AsVector3 { get { return new Vector3(m_Value); } }
        public uint AsUInt { get { return (uint)m_Value; } }
        public int AsInt { get { return (int)m_Value; } }
        public ulong AsULong { get { return (ulong)m_Value; } }
        #endregion


        private readonly static CultureInfo EnUsCulture = new CultureInfo("en-us");
    }
}

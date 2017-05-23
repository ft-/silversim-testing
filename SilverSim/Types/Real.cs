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
    [Serializable]
    public sealed class Real : IComparable<Real>, IEquatable<Real>, IValue
    {
        private double m_Value;

        public Real()
        {
            m_Value = 0f;
        }

        #region Properties
        public ValueType Type => ValueType.Real;

        public LSLValueType LSL_Type => LSLValueType.Float;
        #endregion Properties

        public Real(float val)
        {
            m_Value = val;
        }

        public Real(double val)
        {
            m_Value = val;
        }

        public int CompareTo(Real v) => m_Value.CompareTo(v.m_Value);

        public bool Equals(Real v) => m_Value.Equals(v.m_Value);

        public override bool Equals(object v)
        {
            if (typeof(IValue).IsAssignableFrom(v.GetType()))
            {
                return Equals(((IValue)v).AsReal);
            }
            return false;
        }

        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "{0}", m_Value);

        #region Serialization
        public void FromBytes(byte[] byteArray, int pos)
        {
            if (!BitConverter.IsLittleEndian)
            {
                // Big endian architecture
                var conversionBuffer = new byte[8];

                Buffer.BlockCopy(byteArray, pos, conversionBuffer, 0, 8);

                Array.Reverse(conversionBuffer, 0, 8);

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
                Array.Reverse(dest, pos + 0, 8);
            }
        }
        #endregion Serialization

        #region Operators
        public static Real operator -(Real v) => new Real(-v.m_Value);

        public static Real operator +(Real a, Real b) => new Real(a.m_Value + b.m_Value);

        public static Real operator -(Real a, Real b) => new Real(a.m_Value - b.m_Value);

        public static Real operator *(Real a, Real b) => new Real(a.m_Value * b.m_Value);

        public static Real operator /(Real a, Real b) => new Real(a.m_Value / b.m_Value);

        public static Real operator %(Real a, Real b) => new Real(a.m_Value % b.m_Value);

        public static explicit operator Integer(Real v) => new Integer((Int32)v.m_Value);

        public static explicit operator AString(Real v) => new AString(v.m_Value.ToString());

        public static explicit operator string(Real v) => v.m_Value.ToString();

        public static implicit operator double(Real v) => v.m_Value;
        public static bool operator ==(Real a, Real b) => Math.Abs(a.m_Value - b.m_Value) < Double.Epsilon;

        public static bool operator !=(Real a, Real b) => Math.Abs(a.m_Value - b.m_Value) >= Double.Epsilon;

        public static bool operator <(Real a, Real b) => a.m_Value < b.m_Value;

        public static bool operator >(Real a, Real b) => a.m_Value > b.m_Value;

        public static bool operator <=(Real a, Real b) => a.m_Value <= b.m_Value;

        public static bool operator >=(Real a, Real b) => a.m_Value >= b.m_Value;

        public override int GetHashCode() => (int)m_Value;
        #endregion Operators

        public static Real Parse(string v) => new Real(Double.Parse(v.Trim(), CultureInfo.InvariantCulture));

        public static bool TryParse(string v, out Real res)
        {
            res = default(Real);
            double f;
            if(!double.TryParse(v.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out f))
            {
                return false;
            }
            res = new Real(f);
            return true;
        }

        #region Helpers
        public ABoolean AsBoolean => new ABoolean(Math.Abs(m_Value) >= Single.Epsilon);
        public Integer AsInteger => new Integer((int)m_Value);
        public Quaternion AsQuaternion => new Quaternion(0, 0, 0, m_Value);
        public Real AsReal => new Real(m_Value);
        public AString AsString => new AString(ToString());
        public UUID AsUUID => new UUID();
        public Vector3 AsVector3 => new Vector3(m_Value);
        public uint AsUInt => (uint)m_Value;
        public int AsInt => (int)m_Value;
        public ulong AsULong => (ulong)m_Value;
        public long AsLong => (long)m_Value;
        #endregion
    }
}

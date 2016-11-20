// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Diagnostics.CodeAnalysis;
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
            return string.Format(CultureInfo.InvariantCulture, "{0}", m_Value);
        }

        #region Serialization
        public void FromBytes(byte[] byteArray, int pos, bool normalized)
        {
            if (!BitConverter.IsLittleEndian)
            {
                // Big endian architecture
                byte[] conversionBuffer = new byte[8];

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
            return Math.Abs(a.m_Value - b.m_Value) < Double.Epsilon;
        }

        public static bool operator !=(Real a, Real b)
        {
            return Math.Abs(a.m_Value - b.m_Value) >= Double.Epsilon;
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

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public static Real Parse(string v)
        {
            return new Real(Double.Parse(v.Trim(), CultureInfo.InvariantCulture));
        }

        public static bool TryParse(string v, out Real res)
        {
            res = default(Real);
            double f;
            if(!Double.TryParse(v.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out f))
            {
                return false;
            }
            res = new Real(f);
            return true;
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
    }
}

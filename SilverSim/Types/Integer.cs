// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.Types
{
    [Serializable]
    public sealed class Integer : IComparable<Integer>, IEquatable<Integer>, IValue
    {
        private Int32 m_Value;

        #region Properties
        public ValueType Type
        {
            get
            {
                return ValueType.Integer;
            }
        }

        public LSLValueType LSL_Type
        {
            get
            {
                return LSLValueType.Integer;
            }
        }
        #endregion Properties

        public Integer()
        {
            m_Value = 0;
        }

        public Integer(Int32 val)
        {
            m_Value = val;
        }

        public override string ToString()
        {
            return m_Value.ToString();
        }

        public int CompareTo(Integer v)
        {
            return m_Value.CompareTo(v.m_Value);
        }

        public bool Equals(Integer v)
        {
            return m_Value.Equals(v.m_Value);
        }

        public override bool Equals(object v)
        {
            if(typeof(IValue).IsAssignableFrom(v.GetType()))
            {
                return m_Value.Equals(((IValue)v).AsInteger);
            }
            return false;
        }

        #region Operators
        public static Integer operator-(Integer v)
        {
            return new Integer(-v.m_Value);
        }

        public static Integer operator+(Integer a, Integer b)
        {
            return new Integer(a.m_Value + b.m_Value);
        }

        public static Integer operator-(Integer a, Integer b)
        {
            return new Integer(a.m_Value - b.m_Value);
        }

        public static Integer operator *(Integer a, Integer b)
        {
            return new Integer(a.m_Value * b.m_Value);
        }

        public static Integer operator /(Integer a, Integer b)
        {
            return new Integer(a.m_Value / b.m_Value);
        }

        public static Integer operator %(Integer a, Integer b)
        {
            return new Integer(a.m_Value % b.m_Value);
        }

        public static implicit operator ABoolean(Integer v)
        {
            return new ABoolean(v.m_Value != 0);
        }

        public static implicit operator bool(Integer v)
        {
            return v.m_Value != 0;
        }
        public static implicit operator Int32(Integer v)
        {
            return v.m_Value;
        }
        public static bool operator==(Integer a, Integer b)
        {
            return a.m_Value == b.m_Value;
        }

        public static bool operator!=(Integer a, Integer b)
        {
            return a.m_Value != b.m_Value;
        }

        public static bool operator<(Integer a, Integer b)
        {
            return a.m_Value < b.m_Value;
        }

        public static bool operator>(Integer a, Integer b)
        {
            return a.m_Value > b.m_Value;
        }

        public static bool operator<=(Integer a, Integer b)
        {
            return a.m_Value <= b.m_Value;
        }

        public static bool operator >=(Integer a, Integer b)
        {
            return a.m_Value >= b.m_Value;
        }

        public override int GetHashCode()
        {
            return m_Value;
        }
        #endregion Operators

        public static Integer Parse(string v)
        {
            return new Integer(Int32.Parse(v));
        }

        #region Serialization
        public void FromBytes(byte[] byteArray, int pos, bool normalized)
        {
            if (!BitConverter.IsLittleEndian)
            {
                // Big endian architecture
                byte[] conversionBuffer = new byte[4];

                Buffer.BlockCopy(byteArray, pos, conversionBuffer, 0, 4);
                System.Array.Reverse(conversionBuffer, 0, 4);
                m_Value = BitConverter.ToInt32(conversionBuffer, 0);
            }
            else
            {
                // Little endian architecture
                m_Value = BitConverter.ToInt32(byteArray, pos);
            }
        }

        public void ToBytes(byte[] dest, int pos)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(m_Value), 0, dest, pos + 0, 4);
            if (!BitConverter.IsLittleEndian)
            {
                System.Array.Reverse(dest, pos + 0, 4);
            }
        }
        #endregion Serialization

        #region  Helpers
        public ABoolean AsBoolean { get { return new ABoolean(m_Value != 0); } }
        public Integer AsInteger { get { return new Integer(m_Value); } }
        public Quaternion AsQuaternion { get { return new Quaternion(0, 0, 0, m_Value); } }
        public Real AsReal { get { return new Real(m_Value); } }
        public AString AsString { get { return new AString(ToString()); } }
        public UUID AsUUID { get { return new UUID(); } }
        public Vector3 AsVector3 { get { return new Vector3(m_Value); } }
        public uint AsUInt { get { return (uint)m_Value; } }
        public int AsInt { get { return m_Value; } }
        public ulong AsULong { get { return (ulong)m_Value; } }
        #endregion
    }
}

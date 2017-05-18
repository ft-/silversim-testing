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

namespace SilverSim.Types
{
    [Serializable]
    public sealed class Integer : IComparable<Integer>, IEquatable<Integer>, IValue
    {
        private Int32 m_Value;

        #region Properties
        public ValueType Type => ValueType.Integer;

        public LSLValueType LSL_Type => LSLValueType.Integer;
        #endregion Properties

        public Integer()
        {
        }

        public Integer(Int32 val)
        {
            m_Value = val;
        }

        public override string ToString() => m_Value.ToString();

        public int CompareTo(Integer v) => m_Value.CompareTo(v.m_Value);

        public bool Equals(Integer v) => m_Value.Equals(v.m_Value);

        public override bool Equals(object v)
        {
            if(typeof(IValue).IsAssignableFrom(v.GetType()))
            {
                return m_Value.Equals(((IValue)v).AsInteger);
            }
            return false;
        }

        #region Operators
        public static Integer operator -(Integer v) => new Integer(-v.m_Value);

        public static Integer operator +(Integer a, Integer b) => new Integer(a.m_Value + b.m_Value);

        public static Integer operator -(Integer a, Integer b) => new Integer(a.m_Value - b.m_Value);

        public static Integer operator *(Integer a, Integer b) => new Integer(a.m_Value * b.m_Value);

        public static Integer operator /(Integer a, Integer b) => new Integer(a.m_Value / b.m_Value);

        public static Integer operator %(Integer a, Integer b) => new Integer(a.m_Value % b.m_Value);

        public static implicit operator ABoolean(Integer v) => new ABoolean(v.m_Value != 0);

        public static implicit operator bool(Integer v) => v.m_Value != 0;
        public static implicit operator Int32(Integer v) => v.m_Value;
        public static bool operator ==(Integer a, Integer b) => a.m_Value == b.m_Value;

        public static bool operator !=(Integer a, Integer b) => a.m_Value != b.m_Value;

        public static bool operator <(Integer a, Integer b) => a.m_Value < b.m_Value;

        public static bool operator >(Integer a, Integer b) => a.m_Value > b.m_Value;

        public static bool operator <=(Integer a, Integer b) => a.m_Value <= b.m_Value;

        public static bool operator >=(Integer a, Integer b) => a.m_Value >= b.m_Value;

        public override int GetHashCode() => m_Value;
        #endregion Operators

        public static Integer Parse(string v)
        {
            int i;
            if(!int.TryParse(v, out i))
            {
                throw new ArgumentException("Argument v is not an integer");
            }
            return new Integer(i);
        }

        public static bool TryParse(string v, out Integer res)
        {
            res = default(Integer);
            int i;
            if(!int.TryParse(v, out i))
            {
                return false;
            }
            res = new Integer(i);
            return true;
        }

        #region Serialization
        public void FromBytes(byte[] byteArray, int pos, bool normalized)
        {
            if (!BitConverter.IsLittleEndian)
            {
                // Big endian architecture
                var conversionBuffer = new byte[4];

                Buffer.BlockCopy(byteArray, pos, conversionBuffer, 0, 4);
                Array.Reverse(conversionBuffer, 0, 4);
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
                Array.Reverse(dest, pos + 0, 4);
            }
        }
        #endregion Serialization

        #region  Helpers
        public ABoolean AsBoolean => new ABoolean(m_Value != 0);
        public Integer AsInteger => new Integer(m_Value);
        public Quaternion AsQuaternion => new Quaternion(0, 0, 0, m_Value);
        public Real AsReal => new Real(m_Value);
        public AString AsString => new AString(ToString());
        public UUID AsUUID => new UUID();
        public Vector3 AsVector3 => new Vector3(m_Value);
        public uint AsUInt => (uint)m_Value;
        public int AsInt => m_Value;
        public ulong AsULong => (ulong)m_Value;
        public long AsLong => m_Value;
        #endregion
    }
}

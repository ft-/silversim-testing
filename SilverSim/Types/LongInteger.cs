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

namespace SilverSim.Types
{
    [Serializable]
    public sealed class LongInteger : IComparable<LongInteger>, IEquatable<LongInteger>, IValue
    {
        private long m_Value;

        #region Properties
        public ValueType Type => ValueType.LongInteger;

        public LSLValueType LSL_Type => LSLValueType.LongInteger;
        #endregion Properties

        public LongInteger()
        {
        }

        public LongInteger(long val)
        {
            m_Value = val;
        }

        public override string ToString() => m_Value.ToString();

        public int CompareTo(LongInteger v) => m_Value.CompareTo(v.m_Value);

        public bool Equals(LongInteger v) => m_Value.Equals(v.m_Value);

        public override bool Equals(object v)
        {
            if (typeof(IValue).IsAssignableFrom(v.GetType()))
            {
                return m_Value.Equals(((IValue)v).AsInteger);
            }
            return false;
        }

        #region Operators
        public static LongInteger operator -(LongInteger v) => new LongInteger(-v.m_Value);

        public static LongInteger operator +(LongInteger a, LongInteger b) => new LongInteger(a.m_Value + b.m_Value);

        public static LongInteger operator -(LongInteger a, LongInteger b) => new LongInteger(a.m_Value - b.m_Value);

        public static LongInteger operator *(LongInteger a, LongInteger b) => new LongInteger(a.m_Value * b.m_Value);

        public static LongInteger operator /(LongInteger a, LongInteger b) => new LongInteger(a.m_Value / b.m_Value);

        public static LongInteger operator %(LongInteger a, LongInteger b) => new LongInteger(a.m_Value % b.m_Value);

        public static implicit operator ABoolean(LongInteger v) => new ABoolean(v.m_Value != 0);

        public static implicit operator long(LongInteger v) => v.m_Value;
        public static bool operator ==(LongInteger a, LongInteger b) => a.m_Value == b.m_Value;

        public static bool operator !=(LongInteger a, LongInteger b) => a.m_Value != b.m_Value;

        public static bool operator <(LongInteger a, LongInteger b) => a.m_Value < b.m_Value;

        public static bool operator >(LongInteger a, LongInteger b) => a.m_Value > b.m_Value;

        public static bool operator <=(LongInteger a, LongInteger b) => a.m_Value <= b.m_Value;

        public static bool operator >=(LongInteger a, LongInteger b) => a.m_Value >= b.m_Value;

        public override int GetHashCode() => (int)(m_Value | (m_Value >> 32));
        #endregion Operators

        public static LongInteger Parse(string v)
        {
            long i;
            if (!long.TryParse(v, out i))
            {
                throw new ArgumentException("Argument v is not a long integer");
            }
            return new LongInteger(i);
        }

        public static bool TryParse(string v, out LongInteger res)
        {
            res = default(LongInteger);
            long i;
            if (!long.TryParse(v, out i))
            {
                return false;
            }
            res = new LongInteger(i);
            return true;
        }

        #region Serialization
        public void FromBytes(byte[] byteArray, int pos)
        {
            if (!BitConverter.IsLittleEndian)
            {
                // Big endian architecture
                byte[] conversionBuffer = new byte[8];

                Buffer.BlockCopy(byteArray, pos, conversionBuffer, 0, 8);
                System.Array.Reverse(conversionBuffer, 0, 8);
                m_Value = BitConverter.ToInt64(conversionBuffer, 0);
            }
            else
            {
                // Little endian architecture
                m_Value = BitConverter.ToInt64(byteArray, pos);
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

        #region  Helpers
        public ABoolean AsBoolean => new ABoolean(m_Value != 0);
        public Integer AsInteger => new Integer((int)m_Value);
        public Quaternion AsQuaternion => new Quaternion(0, 0, 0, m_Value);
        public Real AsReal => new Real(m_Value);
        public AString AsString => new AString(ToString());
        public UUID AsUUID => new UUID();
        public Vector3 AsVector3 => new Vector3(m_Value);
        public uint AsUInt => (uint)m_Value;
        public int AsInt => (int)m_Value;
        public ulong AsULong => (ulong)m_Value;
        public long AsLong => m_Value;
        #endregion
    }
}
